namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.ValidationMinMax;

public class ColorsController : ColorsControllerBase
{
	protected override Task<GetColorActionResult> GetColor(long id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<GetColorActionResult>();
	}
}
