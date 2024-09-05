using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;

public class MultiFileRefYamlShould
{

	[Fact]
	public Task HandleAllOfResponses() =>
		TestSingleRequest<MultiFileRef.RandomControllerBase.GetRandomActionResult>(new(
			MultiFileRef.RandomControllerBase.GetRandomActionResult.BadRequest(new MultiFileRef.Types.BadRequest(Code: "ERR-123", Message: "Mocked error")),
			client => client.GetAsync("/multi-file-ref/random")
		)
		{
			AssertResponseMessage = VerifyResponse(400, new { code = "ERR-123", message = "Mocked error" }),
		});
}
