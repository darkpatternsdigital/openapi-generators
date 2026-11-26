using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using HttpMethods;

public class HttpMethodsYamlShould
{
	[Fact]
	public Task Route_get_requests() =>
		TestSingleRequest<ItemsIdControllerBase.GetItemActionResult, string>(new(
			ItemsIdControllerBase.GetItemActionResult.Ok("item"),
			client => client.GetAsync("/http-methods/items/a1")
		)
		{
			AssertRequest = (controller, id) => Assert.Equal("a1", id),
			AssertResponseMessage = VerifyResponse(200, "item"),
		});

	[Fact]
	public Task Route_put_requests() =>
		TestSingleRequest<ItemsIdControllerBase.PutItemActionResult, (string Id, string Body)>(new(
			ItemsIdControllerBase.PutItemActionResult.NoContent(),
			client => client.PutAsync("/http-methods/items/a1", JsonContent.Create("new"))
		)
		{
			AssertRequest = (controller, request) => Assert.Equal(("a1", "new"), request),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Route_post_requests() =>
		TestSingleRequest<ItemsIdControllerBase.PostItemActionResult, (string Id, string Body)>(new(
			ItemsIdControllerBase.PostItemActionResult.NoContent(),
			client => client.PostAsync("/http-methods/items/a1", JsonContent.Create("new"))
		)
		{
			AssertRequest = (controller, request) => Assert.Equal(("a1", "new"), request),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Route_delete_requests() =>
		TestSingleRequest<ItemsIdControllerBase.DeleteItemActionResult, string>(new(
			ItemsIdControllerBase.DeleteItemActionResult.NoContent(),
			client => client.DeleteAsync("/http-methods/items/a1")
		)
		{
			AssertRequest = (controller, id) => Assert.Equal("a1", id),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Route_patch_requests() =>
		TestSingleRequest<ItemsIdControllerBase.PatchItemActionResult, (string Id, string Body)>(new(
			ItemsIdControllerBase.PatchItemActionResult.Ok("patched"),
			client => client.PatchAsync("/http-methods/items/a1", JsonContent.Create("change"))
		)
		{
			AssertRequest = (controller, request) => Assert.Equal(("a1", "change"), request),
			AssertResponseMessage = VerifyResponse(200, "patched"),
		});

	[Fact]
	public Task Route_head_requests() =>
		TestSingleRequest<ItemsIdControllerBase.HeadItemActionResult, string>(new(
			ItemsIdControllerBase.HeadItemActionResult.Ok("42"),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/http-methods/items/a1"))
		)
		{
			AssertRequest = (controller, id) => Assert.Equal("a1", id),
			AssertResponseMessage = async (response) =>
			{
				Assert.Equal(200, (int)response.StatusCode);
				Assert.Equal("42", response.Headers.GetValues("X-Item-Size").Single());
				Assert.Empty(await response.Content.ReadAsByteArrayAsync());
			},
		});

	[Fact]
	public Task Route_options_requests() =>
		TestSingleRequest<ItemsIdControllerBase.OptionsItemActionResult, string>(new(
			ItemsIdControllerBase.OptionsItemActionResult.NoContent("GET, PUT, POST, DELETE, PATCH, HEAD, OPTIONS"),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Options, "/http-methods/items/a1"))
		)
		{
			AssertRequest = (controller, id) => Assert.Equal("a1", id),
			AssertResponseMessage = (response) =>
			{
				Assert.Equal(204, (int)response.StatusCode);
				Assert.Equal("GET, PUT, POST, DELETE, PATCH, HEAD, OPTIONS", string.Join(", ", response.Content.Headers.Allow));
				return Task.CompletedTask;
			},
		});
}
