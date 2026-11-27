using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

internal static class Utilities
{
	static Utilities()
	{
		// Each test starts its own host. With config reloading on, every host holds a file watcher,
		// and parallel tests exceed Linux's inotify instance limit (128 by default).
		Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
	}

	/// <summary>Sends a request to the test app, with <paramref name="api"/> as the implementation of its handler interface</summary>
	public static async Task SendAsync<TApi>(Mock<TApi> api, Func<HttpClient, Task<HttpResponseMessage>> request, Func<HttpResponseMessage, Task> assertResponse)
		where TApi : class
	{
		using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
			builder.ConfigureTestServices(services => services.AddSingleton(api.Object)));
		// Redirects are responses to check, not to follow
		using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
		using var response = await request(client);
		await assertResponse(response);
	}

	public static Func<HttpResponseMessage, Task> VerifyResponse(int statusCode) => response =>
	{
		Assert.Equal(statusCode, (int)response.StatusCode);
		return Task.CompletedTask;
	};

	public static Func<HttpResponseMessage, Task> VerifyResponse(int statusCode, string mediaType, string json) => async response =>
	{
		Assert.Equal(statusCode, (int)response.StatusCode);
		Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
		Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(await response.Content.ReadAsStringAsync())));
	};
}
