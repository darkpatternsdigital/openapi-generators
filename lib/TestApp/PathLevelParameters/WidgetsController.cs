namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.PathLevelParameters;

public class WidgetsController : WidgetsWidgetIdControllerBase
{
	protected override Task<GetWidgetActionResult> GetWidget(string widgetId, bool? verbose)
	{
		this.DelegateRequest((widgetId, verbose));
		return this.DelegateResponse<GetWidgetActionResult>();
	}

	protected override Task<DeleteWidgetActionResult> DeleteWidget(string widgetId, bool verbose, string? reason)
	{
		this.DelegateRequest((widgetId, verbose, reason));
		return this.DelegateResponse<DeleteWidgetActionResult>();
	}
}
