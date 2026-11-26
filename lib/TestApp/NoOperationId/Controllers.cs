namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.NoOperationId;

public class WidgetsController : WidgetsControllerBase
{
	// Without an operationId, an operation with a request body is generated with the name `_`
	protected override Task<PostNoOperationIdWidgetsActionResult> _(string body)
	{
		this.DelegateRequest(body);
		return this.DelegateResponse<PostNoOperationIdWidgetsActionResult>();
	}
}
