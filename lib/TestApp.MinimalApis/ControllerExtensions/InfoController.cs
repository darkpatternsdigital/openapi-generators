namespace DarkPatterns.TestApp.MinimalApis.ControllerExtensions
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
