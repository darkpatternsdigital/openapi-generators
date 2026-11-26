namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.HttpTrace;

public class ItemsController : ItemsControllerBase
{
	protected override Task<TraceItemsActionResult> TraceItems()
	{
		this.DelegateRequest();
		return this.DelegateResponse<TraceItemsActionResult>();
	}
}
