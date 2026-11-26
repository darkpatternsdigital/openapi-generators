namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.IntegerEnum;

public class PriorityController : PriorityControllerBase
{
	protected override Task<SetPriorityActionResult> SetPriority(int setPriorityBody)
	{
		this.DelegateRequest(setPriorityBody);
		return this.DelegateResponse<SetPriorityActionResult>();
	}
}
