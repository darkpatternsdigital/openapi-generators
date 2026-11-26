using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using HttpTrace;

public class HttpTraceYamlShould
{
	[Fact]
	public Task Route_trace_requests() =>
		TestSingleRequest<ItemsControllerBase.TraceItemsActionResult>(new(
			ItemsControllerBase.TraceItemsActionResult.Unsafe(new Microsoft.AspNetCore.Mvc.NoContentResult()),
			client => client.SendAsync(new HttpRequestMessage(new HttpMethod("TRACE"), "/http-trace/items"))
		)
		{
			AssertResponseMessage = VerifyResponse(204),
		});
}
