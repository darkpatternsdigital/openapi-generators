using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using Servers;

public class ServersYamlShould
{
	[Fact(Skip = "OpenAPI 3.0 gap (Servers): the server URL's base path is ignored, so the request gets 404")]
	public Task Route_paths_relative_to_the_server_url() =>
		TestSingleRequest<ThingsControllerBase.ListThingsActionResult>(new(
			ThingsControllerBase.ListThingsActionResult.Ok(["a"]),
			client => client.GetAsync("/servers/api/v1/things")
		)
		{
			AssertResponseMessage = VerifyResponse(200, new[] { "a" }),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Servers): Path Item servers are ignored, so the request gets 404")]
	public Task Route_paths_relative_to_the_path_item_server_url() =>
		TestSingleRequest<OtherThingsControllerBase.ListOtherThingsActionResult>(new(
			OtherThingsControllerBase.ListOtherThingsActionResult.Ok(["b"]),
			client => client.GetAsync("/servers/api/v2/other-things")
		)
		{
			AssertResponseMessage = VerifyResponse(200, new[] { "b" }),
		});
}
