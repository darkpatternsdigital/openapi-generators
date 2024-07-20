using DarkPatterns.OpenApiCodegen.Json.Extensions;
using System.Threading.Tasks;

namespace DarkPatterns.ServerInterfacesExample.Oauth.Controllers
{
	public class InfoController : InfoControllerBase
	{
		protected override Task<GetInfoActionResult> GetInfo()
		{
			return Task.FromResult(GetInfoActionResult.Ok(User.Identity!.IsAuthenticated ? $"success as {User.Identity.Name}" : "success"));
		}
	}
}