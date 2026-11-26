using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using CookieParameters;

public class CookieParametersYamlShould
{
	[Fact]
	public Task Bind_cookie_parameters() =>
		TestSingleRequest<SessionControllerBase.GetSessionActionResult, (string SessionId, string? Theme)>(new(
			SessionControllerBase.GetSessionActionResult.Ok("s-1"),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/cookie-parameters/session")
			{
				Headers = { { "Cookie", "sessionId=s-1; theme=dark%20mode" } },
			})
		)
		{
			AssertRequest = (controller, request) => Assert.Equal(("s-1", "dark mode"), request),
			AssertResponseMessage = VerifyResponse(200, "s-1"),
		});

	[Fact]
	public Task Leave_missing_optional_cookie_parameters_null() =>
		TestSingleRequest<SessionControllerBase.GetSessionActionResult, (string SessionId, string? Theme)>(new(
			SessionControllerBase.GetSessionActionResult.Ok("s-1"),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/cookie-parameters/session")
			{
				Headers = { { "Cookie", "sessionId=s-1" } },
			})
		)
		{
			AssertRequest = (controller, request) => Assert.Equal(("s-1", null), request),
			AssertResponseMessage = VerifyResponse(200, "s-1"),
		});

	[Fact]
	public Task Reject_a_missing_required_cookie_parameter() =>
		TestValidationError(
			SessionControllerBase.GetSessionActionResult.Unsafe,
			client => client.GetAsync("/cookie-parameters/session")
		);
}
