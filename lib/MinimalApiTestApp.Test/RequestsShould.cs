using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using Moq;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

using static Utilities;

/// <summary>Requests are bound to the handler interface's parameters</summary>
public class RequestsShould
{
	private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

	[Fact]
	public async Task Bind_path_and_query_parameters()
	{
		var api = new Mock<PathLevelParameters.IWidgetsWidgetIdApi>();
		api.Setup(a => a.GetWidget("w1", Optional.Create(true))).ReturnsAsync(PathLevelParameters.IWidgetsWidgetIdApi.GetWidgetResult.Ok("found"));

		await SendAsync(api, client => client.GetAsync("/path-level-parameters/widgets/w1?verbose=true"),
			VerifyResponse(200, "application/json", "\"found\""));
	}

	[Fact]
	public async Task Bind_missing_optional_query_parameters_as_null()
	{
		var api = new Mock<PathLevelParameters.IWidgetsWidgetIdApi>();
		api.Setup(a => a.DeleteWidget("w1", false, null!)).ReturnsAsync(PathLevelParameters.IWidgetsWidgetIdApi.DeleteWidgetResult.NoContent());

		await SendAsync(api, client => client.DeleteAsync("/path-level-parameters/widgets/w1?verbose=false"),
			VerifyResponse(204));
	}

	[Fact]
	public async Task Reject_requests_without_a_required_query_parameter()
	{
		var api = new Mock<PathLevelParameters.IWidgetsWidgetIdApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.DeleteAsync("/path-level-parameters/widgets/w1"),
			VerifyResponse(400));
	}

	[Fact]
	public async Task Bind_base64_header_parameters()
	{
		var api = new Mock<Headers.IInfoApi>();
		api.Setup(a => a.GetInfo(new byte[] { 1, 2, 3 })).ReturnsAsync(Headers.IInfoApi.GetInfoResult.NoContent("ok"));

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/headers/info") { Headers = { { "X-Data", Convert.ToBase64String([1, 2, 3]) } } }),
			VerifyResponse(204));
	}

	[Fact]
	public async Task Reject_invalid_base64_header_parameters()
	{
		var api = new Mock<Headers.IInfoApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/headers/info") { Headers = { { "X-Data", "not base64!" } } }),
			VerifyResponse(400));
	}

	[Fact]
	public async Task Bind_a_discriminated_oneOf_body()
	{
		var api = new Mock<OneOf.IPetApi>();
		api.Setup(a => a.AddPet(It.Is<OneOf.SpecifiedPet>(p => p.Dog != null && p.Dog.Breed == "Shiba Inu"))).ReturnsAsync(OneOf.IPetApi.AddPetResult.Ok());

		await SendAsync(api, client => client.PostAsync("/one-of/pet", Json("""{ "petType": "dog", "bark": true, "breed": "Shiba Inu" }""")),
			VerifyResponse(200));
	}

	[Fact]
	public async Task Reject_an_invalid_oneOf_body()
	{
		var api = new Mock<OneOf.IPetApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.PostAsync("/one-of/pet", Json("""{ "petType": "fish" }""")),
			VerifyResponse(400));
	}

	// MVC answers 415 Unsupported Media Type; Minimal API answers 400 for a missing JSON body
	[Fact]
	public async Task Reject_requests_without_a_required_body()
	{
		var api = new Mock<OneOf.IPetApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.PostAsync("/one-of/pet", null),
			VerifyResponse(400));
	}

	[Theory]
	[InlineData("GET")]
	[InlineData("PUT")]
	[InlineData("POST")]
	[InlineData("DELETE")]
	[InlineData("PATCH")]
	[InlineData("HEAD")]
	[InlineData("OPTIONS")]
	public async Task Route_each_HTTP_method_to_its_operation(string method)
	{
		var api = new Mock<HttpMethods.IItemsIdApi>();
		api.Setup(a => a.GetItem("i1")).ReturnsAsync(HttpMethods.IItemsIdApi.GetItemResult.Ok("got"));
		api.Setup(a => a.PutItem("i1", "body")).ReturnsAsync(HttpMethods.IItemsIdApi.PutItemResult.NoContent());
		api.Setup(a => a.PostItem("i1", "body")).ReturnsAsync(HttpMethods.IItemsIdApi.PostItemResult.NoContent());
		api.Setup(a => a.DeleteItem("i1")).ReturnsAsync(HttpMethods.IItemsIdApi.DeleteItemResult.NoContent());
		api.Setup(a => a.PatchItem("i1", "body")).ReturnsAsync(HttpMethods.IItemsIdApi.PatchItemResult.Ok("patched"));
		api.Setup(a => a.HeadItem("i1")).ReturnsAsync(HttpMethods.IItemsIdApi.HeadItemResult.Ok("3"));
		api.Setup(a => a.OptionsItem("i1")).ReturnsAsync(HttpMethods.IItemsIdApi.OptionsItemResult.NoContent("GET, PUT"));

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/http-methods/items/i1")
		{
			Content = method is "PUT" or "POST" or "PATCH" ? Json("\"body\"") : null,
		}), response =>
		{
			Assert.True(response.IsSuccessStatusCode, $"{method}: {(int)response.StatusCode}");
			return Task.CompletedTask;
		});
		Assert.Single(api.Invocations);
	}

	[Fact]
	public async Task Route_TRACE_requests()
	{
		var api = new Mock<HttpTrace.IItemsApi>();
		api.Setup(a => a.TraceItems()).ReturnsAsync(HttpTrace.IItemsApi.TraceItemsResult.Ok("traced"));

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(HttpMethod.Trace, "/http-trace/items")),
			VerifyResponse(200));
	}
}
