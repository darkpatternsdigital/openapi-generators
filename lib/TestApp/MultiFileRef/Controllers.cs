

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.MultiFileRef;

public class RandomController : RandomControllerBase
{
	protected override Task<GetRandomActionResult> GetRandom()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetRandomActionResult>();
	}
}
