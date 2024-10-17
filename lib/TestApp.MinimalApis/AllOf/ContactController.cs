namespace DarkPatterns.TestApp.MinimalApis.AllOf;

public class ContactController : ContactControllerBase
{
	protected override Task<GetContactActionResult> GetContact()
	{
		return this.DelegateResponse<GetContactActionResult>();
	}
}
