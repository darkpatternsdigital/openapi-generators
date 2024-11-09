using Microsoft.AspNetCore.Mvc.Filters;

namespace DarkPatterns.Security;

public class SecurityActionFilterAttribute(string securityRequirements) : ActionFilterAttribute, IAuthorizationFilter
{
	public void OnAuthorization(AuthorizationFilterContext context)
	{
		// TODO: it would be good to write a test for this.
		Console.Write(securityRequirements);
	}
}
