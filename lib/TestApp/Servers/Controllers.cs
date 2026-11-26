namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Servers;

public class ThingsController : ThingsControllerBase
{
	protected override Task<ListThingsActionResult> ListThings()
	{
		this.DelegateRequest();
		return this.DelegateResponse<ListThingsActionResult>();
	}
}

public class OtherThingsController : OtherThingsControllerBase
{
	protected override Task<ListOtherThingsActionResult> ListOtherThings()
	{
		this.DelegateRequest();
		return this.DelegateResponse<ListOtherThingsActionResult>();
	}
}
