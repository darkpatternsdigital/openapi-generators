namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.PerOutput.Server;

public class DataController : DataControllerBase
{
	protected override Task<GetDataActionResult> GetData()
	{
		return this.DelegateResponse<GetDataActionResult>();
	}
}
