namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.NoOperationId;

public class WidgetsController : WidgetsControllerBase
{
	protected override Task<PostNoOperationIdWidgetsActionResult> PostNoOperationIdWidgets(string body)
	{
		this.DelegateRequest(body);
		return this.DelegateResponse<PostNoOperationIdWidgetsActionResult>();
	}
}
