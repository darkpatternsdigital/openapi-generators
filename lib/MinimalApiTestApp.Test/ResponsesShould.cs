using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

using static Utilities;

/// <summary>The generated result types write their responses</summary>
public class ResponsesShould
{
	[Fact]
	public async Task Write_a_oneOf_body_as_its_case()
	{
		var api = new Mock<OneOf.IPetApi>();
		api.Setup(a => a.GetRandomPet()).ReturnsAsync(OneOf.IPetApi.GetRandomPetResult.Ok(new(Dog: new(true, "Shiba Inu"))));

		await SendAsync(api, client => client.GetAsync("/one-of/pet"),
			VerifyResponse(200, "application/json", """{ "bark": true, "breed": "Shiba Inu" }"""));
	}

	[Fact]
	public async Task Add_response_headers()
	{
		var api = new Mock<Headers.IInfoApi>();
		api.Setup(a => a.GetInfo(It.IsAny<byte[]>())).ReturnsAsync(Headers.IInfoApi.GetInfoResult.Ok(new() { ["key"] = "value" }, "abc"));

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/headers/info") { Headers = { { "X-Data", "AQID" } } }), async response =>
		{
			await VerifyResponse(200, "application/json", """{ "key": "value" }""")(response);
			Assert.Equal(["abc"], response.Headers.GetValues("X-Data"));
		});
	}

	[Fact]
	public async Task Write_status_codes_in_a_range()
	{
		var api = new Mock<Responses.IRangesApi>();
		api.Setup(a => a.GetRanges()).ReturnsAsync(Responses.IRangesApi.GetRangesResult.Successful(202, "accepted"));

		await SendAsync(api, client => client.GetAsync("/responses/ranges"),
			VerifyResponse(202, "application/json", "\"accepted\""));
	}

	[Fact]
	public void Reject_status_codes_outside_the_range()
	{
		Assert.Throws<System.ArgumentOutOfRangeException>(() => Responses.IRangesApi.GetRangesResult.Successful(404, "missing"));
	}

	[Fact]
	public async Task Write_the_default_response_with_any_status_code()
	{
		var api = new Mock<Responses.IDefaultApi>();
		api.Setup(a => a.GetDefault()).ReturnsAsync(Responses.IDefaultApi.GetDefaultResult.OtherStatusCode(503, new("unavailable")));

		await SendAsync(api, client => client.GetAsync("/responses/default"),
			VerifyResponse(503, "application/json", """{ "code": "unavailable" }"""));
	}

	[Fact]
	public async Task Write_text_bodies_as_text()
	{
		var api = new Mock<Responses.IMediaTypesApi>();
		api.Setup(a => a.GetMediaTypes()).ReturnsAsync(Responses.IMediaTypesApi.GetMediaTypesResult.TextPlainOk("plain text"));

		await SendAsync(api, client => client.GetAsync("/responses/media-types"), async response =>
		{
			Assert.Equal(200, (int)response.StatusCode);
			Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
			Assert.Equal("plain text", await response.Content.ReadAsStringAsync());
		});
	}

	[Fact]
	public async Task Write_status_codes_without_a_body()
	{
		var api = new Mock<Headers.IRedirectApi>();
		api.Setup(a => a.Redirect()).ReturnsAsync(Headers.IRedirectApi.RedirectResult.Redirect("/elsewhere"));

		await SendAsync(api, client => client.GetAsync("/headers/redirect"), response =>
		{
			Assert.Equal(302, (int)response.StatusCode);
			Assert.Equal("/elsewhere", response.Headers.Location?.OriginalString);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task Write_unsafe_results_with_their_headers()
	{
		var api = new Mock<Responses.IRefApi>();
		api.Setup(a => a.GetRef()).ReturnsAsync(Responses.IRefApi.GetRefResult.Unsafe(Results.StatusCode(418), new KeyValuePair<string, string>("X-Teapot", "yes")));

		await SendAsync(api, client => client.GetAsync("/responses/ref"), response =>
		{
			Assert.Equal(418, (int)response.StatusCode);
			Assert.Equal(["yes"], response.Headers.GetValues("X-Teapot"));
			return Task.CompletedTask;
		});
	}

	[Fact]
	public void Expose_the_underlying_result_and_headers()
	{
		var result = Headers.IInfoApi.GetInfoResult.NoContent("abc");

		Assert.IsAssignableFrom<IStatusCodeHttpResult>(result.UnderlyingResult);
		Assert.Equal(204, ((IStatusCodeHttpResult)result.UnderlyingResult).StatusCode);
		Assert.Equal([new("X-Data", "abc")], result.Headers.ToArray());
	}
}
