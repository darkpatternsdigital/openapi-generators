
namespace DarkPatterns.TestApp.MinimalApis.OpenApi30Callbacks;

public class StreamsController : StreamsControllerBase
{
	protected override Task<PostOpenapi30CallbacksStreamsActionResult> PostOpenapi30CallbacksStreams(string callbackUrl)
	{
		this.DelegateRequest(callbackUrl);
		return this.DelegateResponse<PostOpenapi30CallbacksStreamsActionResult>();
	}
}
