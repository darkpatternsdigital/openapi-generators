using Microsoft.AspNetCore.Mvc.Filters;

namespace DarkPatterns.Security;

public class SecurityActionFilterAttribute(string securityRequirements) : ActionFilterAttribute, IAuthorizationFilter
{
	public void OnAuthorization(AuthorizationFilterContext context)
	{

	}
}
