using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using OpenApi30Callbacks;

public class OpenApi30CallbacksYamlShould
{
	[Fact]
	public Task Accept_the_callback_url_of_a_subscription() =>
		TestSingleRequest<StreamsControllerBase.PostOpenapi30CallbacksStreamsActionResult, string>(new(
			StreamsControllerBase.PostOpenapi30CallbacksStreamsActionResult.Created(new PostStreamsResponse("sub-1")),
			client => client.PostAsync("/openapi-3.0-callbacks/streams?callbackUrl=" + Uri.EscapeDataString("https://example.com/cb"), null)
		)
		{
			AssertRequest = (controller, callbackUrl) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("https://example.com/cb", callbackUrl);
			},
			AssertResponseMessage = VerifyResponse(201, new { subscriptionId = "sub-1" }),
		});

	[Fact]
	public async Task Send_callback_requests_with_the_webhook_client()
	{
		HttpRequestMessage? sent = null;
		string? sentBody = null;
		using var client = new HttpMessageInvoker(new CallbackHandler(async request =>
		{
			sent = request;
			sentBody = await request.Content!.ReadAsStringAsync();
			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}));

		var result = await client.PostOnData(
			new Uri("https://example.com/cb/data"),
			new PostStreamsPostOnDataRequest(null, Optional.Create("hello")));

		Assert.IsType<WebhookOperations.PostOnDataReturnType.Accepted>(result);
		Assert.Equal(HttpMethod.Post, sent?.Method);
		Assert.Equal("https://example.com/cb/data", sent?.RequestUri?.OriginalString);
		Assert.Equal("application/json", sent?.Content?.Headers.ContentType?.MediaType);
		Assert.True(JsonCompare.CompareJsonStrings(sentBody!, """{"userData":"hello"}"""), sentBody);
	}

	private sealed class CallbackHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			respond(request);
	}
}
