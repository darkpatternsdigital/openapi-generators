using static DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.AllOf.ContactControllerBase;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.ControllerExtensions
{
	public class InfoController : ControllerExtensions.InformationControllerBase
	{
		protected override Task<GetInfoActionResult> GetInfo(byte[]? data)
		{
			this.DelegateRequest(data);
			return this.DelegateResponse<GetInfoActionResult>();
		}
	}
}
