namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Documentation;

public class GadgetsController : GadgetsControllerBase
{
	protected override Task<ListGadgetsActionResult> ListGadgets(string? color, string? legacyFilter)
	{
		this.DelegateRequest((color, legacyFilter));
		return this.DelegateResponse<ListGadgetsActionResult>();
	}
}

public class OldGadgetsController : OldGadgetsControllerBase
{
	protected override Task<ListOldGadgetsActionResult> ListOldGadgets()
	{
		this.DelegateRequest();
		return this.DelegateResponse<ListOldGadgetsActionResult>();
	}
}
