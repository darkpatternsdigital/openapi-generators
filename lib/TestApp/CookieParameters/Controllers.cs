namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.CookieParameters;

public class SessionController : SessionControllerBase
{
	protected override Task<GetSessionActionResult> GetSession(string sessionId, string? theme)
	{
		this.DelegateRequest((sessionId, theme));
		return this.DelegateResponse<GetSessionActionResult>();
	}
}
