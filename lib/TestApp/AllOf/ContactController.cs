using System;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.AllOf;

public class ContactController : ContactControllerBase
{
	protected override Task<GetContactActionResult> GetContact()
	{
		return this.DelegateResponse<GetContactActionResult>();
	}
}
