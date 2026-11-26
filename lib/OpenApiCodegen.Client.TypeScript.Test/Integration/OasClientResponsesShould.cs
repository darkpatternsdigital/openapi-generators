using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using static DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils.GenerationUtilities;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Integration;

/// <summary>Responses parsed from the OpenAPI 3.0.3 coverage fixtures (schemas/oas-client-*.yaml)</summary>
[Collection(CommonDirectoryFixture.CollectionName)]
public class OasClientResponsesShould
{
	private readonly CommonDirectoryFixture commonDirectory;

	public OasClientResponsesShould(CommonDirectoryFixture commonDirectory)
	{
		this.commonDirectory = commonDirectory;
	}

	[Fact]
	public async Task Prefer_an_exact_status_code_over_a_range()
	{
		var body = new { id = "i-1" };

		var result = await commonDirectory.ConvertResponse("oas-client-responses.yaml", "getRanges", 200, body);

		AssertResponseSuccess(result, 200, body);
	}

	[Theory]
	[InlineData(201, "{\"id\":\"i-1\"}")]
	[InlineData(404, "{\"message\":\"missing\"}")]
	public async Task Parse_status_code_range_responses(int statusCode, string json)
	{
		var body = JsonNode.Parse(json);

		var result = await commonDirectory.ConvertResponse("oas-client-responses.yaml", "getRanges", statusCode, body);

		AssertResponseSuccess(result, statusCode, body);
	}

	[Fact]
	public async Task Parse_default_responses_as_other()
	{
		var body = new { message = "unavailable" };

		var result = await commonDirectory.ConvertResponse("oas-client-responses.yaml", "getWithDefault", 503, body);

		AssertResponseOtherStatusCode(result, body);
	}

	// The headers property is illustrative: response headers are not parsed yet
	[Fact(Skip = "OpenAPI 3.0 gap (Response headers): declared response headers are not parsed into the response")]
	public async Task Parse_typed_response_headers()
	{
		var result = await commonDirectory.TsNode(@"
			import { conversion } from './oas-client-responses.yaml/operations/getWithHeaders';
			const headers: Record<string, string> = { 'Content-Type': 'application/json', 'X-Rate-Limit': '10', 'X-Request-Id': 'r-1' };
			const result = conversion.response({ status: 200, response: { id: 'i-1' }, getResponseHeader: (name) => headers[name] ?? null });
			if (result.statusCode !== 200) throw new Error('Unexpected status');
			const rateLimit: number = result.headers['X-Rate-Limit'];
			console.log(JSON.stringify({ rateLimit, requestId: result.headers['X-Request-Id'] }));
		");

		Assert.Equal(0, result.ExitCode);
		CompareJson(JsonNode.Parse(result.Output), new { rateLimit = 10, requestId = "r-1" });
	}

	[Fact]
	public async Task Parse_responses_with_links()
	{
		var body = new { id = "i-1" };

		var result = await commonDirectory.ConvertResponse("oas-client-responses.yaml", "createItem", 201, body);

		AssertResponseSuccess(result, 201, body);
	}

	[Fact]
	public async Task Parse_head_responses()
	{
		var result = await commonDirectory.ConvertResponse("oas-client-methods.yaml", "checkItem", 404);

		AssertResponseSuccess(result, 404);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): writeOnly properties are required in responses")]
	public async Task Not_require_write_only_properties_in_responses()
	{
		var body = new { id = "a-1", name = "n" };

		var result = await commonDirectory.ConvertResponse("oas-client-models.yaml", "createAccount", 201, body);

		AssertResponseSuccess(result, 201, body);
	}
}
