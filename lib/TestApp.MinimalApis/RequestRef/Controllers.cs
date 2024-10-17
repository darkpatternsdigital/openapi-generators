
namespace DarkPatterns.TestApp.MinimalApis.RequestRef;

public class AddressController : AddressControllerBase
{
	protected override Task<LookupRecordActionResult> LookupRecord(LookupRecordRequest lookupRecordBody)
	{
		this.DelegateRequest(lookupRecordBody);
		return this.DelegateResponse<LookupRecordActionResult>();
	}
}
