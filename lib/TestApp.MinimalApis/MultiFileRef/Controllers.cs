

namespace DarkPatterns.TestApp.MinimalApis.MultiFileRef;

public class RandomController : RandomControllerBase
{
	protected override Task<GetRandomActionResult> GetRandom()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetRandomActionResult>();
	}
}

public class EndpointController : EndpointControllerBase
{
	protected override Task<DoSomethingActionResult> DoSomething()
	{
		this.DelegateRequest();
		return this.DelegateResponse<DoSomethingActionResult>();
	}
}