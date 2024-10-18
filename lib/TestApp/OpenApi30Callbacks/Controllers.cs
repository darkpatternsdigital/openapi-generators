
namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.OpenApi30Callbacks;

public class StreamsController : StreamsControllerBase
{
	protected override Task<PostStreamsActionResult> PostStreams(string callbackUrl)
	{
		this.DelegateRequest(callbackUrl);
		return this.DelegateResponse<PostStreamsActionResult>();
	}
}
