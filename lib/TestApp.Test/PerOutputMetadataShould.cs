using System;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;

/// <summary>
/// A file listed as both Client and MvcServer generates each output with its own metadata (planning #3).
/// </summary>
public class PerOutputMetadataShould
{
	[Fact]
	public Task Call_the_server_output_with_the_client_output_of_the_same_file() =>
		TestSingleRequest<PerOutput.Server.DataControllerBase.GetDataActionResult>(new(
			PerOutput.Server.DataControllerBase.GetDataActionResult.Ok(new(String: Optional.Create("text"), UnknownString: null, Integer: Optional.Create(5), UnknownInteger: null, Record: null)),
			async client =>
			{
				// The server output has its own PathPrefix; the client output has none
				client.BaseAddress = new Uri(client.BaseAddress!, "per-output/");
				return await client.SendAsync(PerOutput.Client.Operations.GetData());
			}
		)
		{
			AssertResponseMessage = VerifyResponse(200, new { String = "text", Integer = 5 }),
		});

	[Fact]
	public void Generate_an_item_listed_twice_with_the_same_metadata()
	{
		using var request = Compat.DuplicateItem.Operations.TestPath(limit: null);

		Assert.StartsWith("path", request.RequestUri!.OriginalString);
	}
}
