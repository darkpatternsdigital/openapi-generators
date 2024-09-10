
namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.RequestRef;

public class AddressController : AddressControllerBase
{
	protected override Task<LookupRecordActionResult> LookupRecord(LookupRecordRequest lookupRecordBody)
	{
		this.DelegateRequest(lookupRecordBody);
		return this.DelegateResponse<LookupRecordActionResult>();
	}
}
