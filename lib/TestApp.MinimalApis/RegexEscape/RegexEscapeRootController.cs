namespace DarkPatterns.TestApp.MinimalApis.RegexEscape;

public class RegexEscapeRootController : RegexEscape.ControllerBase
{
	protected override Task<TestForRegexActionResult> TestForRegex(string testForRegexBody)
	{
		this.DelegateRequest(testForRegexBody);
		return this.DelegateResponse<TestForRegexActionResult>();
	}
}
