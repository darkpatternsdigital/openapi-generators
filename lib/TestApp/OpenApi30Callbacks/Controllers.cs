
namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.OpenApi30Callbacks;

public class StreamsController : StreamsControllerBase
{
	protected override Task<PostOpenapi30CallbacksStreamsActionResult> PostOpenapi30CallbacksStreams(string callbackUrl)
	{
		this.DelegateRequest(callbackUrl);
		return this.DelegateResponse<PostOpenapi30CallbacksStreamsActionResult>();
	}
}
