using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using static DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils.GenerationUtilities;
using System;
using System.Collections.Specialized;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Integration;

/// <summary>Requests built from the OpenAPI 3.0.3 coverage fixtures (schemas/oas-client-*.yaml)</summary>
[Collection(CommonDirectoryFixture.CollectionName)]
public class OasClientRequestsShould
{
	private readonly CommonDirectoryFixture commonDirectory;

	public OasClientRequestsShould(CommonDirectoryFixture commonDirectory)
	{
		this.commonDirectory = commonDirectory;
	}

	[Fact]
	public async Task Send_put_requests_with_bodies()
	{
		var body = new { name = "n" };

		var result = await commonDirectory.ConvertRequest("oas-client-methods.yaml", "replaceItem", new { id = "i-1" }, body);

		AssertRequestSuccess(result, "PUT", "/items/i-1", body);
	}

	[Fact]
	public async Task Send_patch_requests_with_an_optional_body()
	{
		var body = new { name = "n" };

		var result = await commonDirectory.ConvertRequest("oas-client-methods.yaml", "updateItem", new { id = "i-1" }, body);

		AssertRequestSuccess(result, "PATCH", "/items/i-1", body);
	}

	[Fact]
	public async Task Omit_an_optional_request_body()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-methods.yaml", "updateItem", new { id = "i-1" });

		AssertRequestSuccess(result, "PATCH", "/items/i-1");
	}

	[Theory]
	[InlineData("checkItem", "HEAD")]
	[InlineData("itemOptions", "OPTIONS")]
	public async Task Send_each_http_method(string operation, string method)
	{
		var result = await commonDirectory.ConvertRequest("oas-client-methods.yaml", operation, new { id = "i-1" });

		AssertRequestSuccess(result, method, "/items/i-1");
	}

	[Fact]
	public async Task Send_trace_requests()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-trace.yaml", "traceEcho", new { });

		AssertRequestSuccess(result, "TRACE", "/echo");
	}

	[Fact]
	public async Task Generate_operations_without_an_operationId()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-methods.yaml", "getUntitled", new { });

		AssertRequestSuccess(result, "GET", "/untitled");
	}

	[Fact]
	public async Task Substitute_several_parameters_in_one_path_segment()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-methods.yaml", "getFile", new { name = "report", ext = "pdf" });

		AssertRequestSuccess(result, "GET", "/files/report.pdf");
	}

	[Fact]
	public async Task Send_cookie_parameters()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-cookies.yaml", "getSession", new { session = "abc", theme = "dark" });

		var token = AssertRequestSuccess(result, "GET", "/session");
		Assert.Equal("session=abc; theme=dark", token?["headers"]?["Cookie"]?.GetValue<string>());
	}

	[Fact]
	public async Task Encode_cookie_values()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-cookies.yaml", "getSession", new { session = "a;b=c", visits = 3 });

		var token = AssertRequestSuccess(result, "GET", "/session");
		Assert.Equal("session=a%3Bb%3Dc; visits=3", token?["headers"]?["Cookie"]?.GetValue<string>());
	}

	[Fact]
	public async Task Send_simple_style_path_arrays()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", "getSimple", new { ids = new[] { 1, 2, 3 } });

		Assert.Equal("/simple/1,2,3", Uri.UnescapeDataString(GetPath(result)));
	}

	[Theory(Skip = "OpenAPI 3.0 gap (Parameter style): label and matrix styles are ignored; arrays are sent comma-separated without a prefix")]
	[InlineData("getLabel", "/label/.1,2,3")]
	[InlineData("getMatrix", "/matrix/;ids=1,2,3")]
	public async Task Prefix_label_and_matrix_style_path_arrays(string operation, string expected)
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", operation, new { ids = new[] { 1, 2, 3 } });

		Assert.Equal(expected, Uri.UnescapeDataString(GetPath(result)));
	}

	[Fact]
	public async Task Repeat_exploded_query_arrays()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", "getFormExplode", new { tags = new[] { "a", "b" } });

		AssertRequestSuccess(result, "GET", "/form-explode?tags=a&tags=b");
	}

	[Theory(Skip = "OpenAPI 3.0 gap (Parameter style): explode: false and delimited styles are ignored; arrays are always sent as repeated keys")]
	[InlineData("getFormNoExplode", "a,b")]
	[InlineData("getSpaceDelimited", "a b")]
	[InlineData("getPipeDelimited", "a|b")]
	public async Task Delimit_query_arrays_that_are_not_exploded(string operation, string expected)
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", operation, new { tags = new[] { "a", "b" } });

		Assert.Equal([expected], GetQuery(result).GetValues("tags") ?? Array.Empty<string>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): style: deepObject is ignored; the object is sent as [object Object]")]
	public async Task Send_deep_object_query_parameters()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", "getDeepObject", new { filter = new { color = "red", size = "large" } });

		var query = GetQuery(result);
		Assert.Equal("red", query["filter[color]"]);
		Assert.Equal("large", query["filter[size]"]);
		Assert.Null(query["filter"]);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): exploded form objects are sent as [object Object] instead of one key per property")]
	public async Task Send_exploded_form_object_query_parameters()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", "getFormObject", new { filter = new { color = "red", size = "large" } });

		var query = GetQuery(result);
		Assert.Equal("red", query["color"]);
		Assert.Equal("large", query["size"]);
		Assert.Null(query["filter"]);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): allowReserved is ignored; reserved characters are percent-encoded")]
	public async Task Send_reserved_characters_when_allowed()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", "getReserved", new { path = "a/b:c" });

		AssertRequestSuccess(result, "GET", "/reserved?path=a/b:c");
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): content-based parameters are sent as [object Object] instead of as JSON")]
	public async Task Serialize_content_parameters_as_their_media_type()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-param-styles.yaml", "getContentParam", new { filter = new { color = "red", size = "large" } });

		CompareJson(JsonNode.Parse(GetQuery(result)["filter"]!), new { color = "red", size = "large" });
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): array header parameters fail tsc (string[] is not assignable to string)")]
	public async Task Send_array_headers_comma_separated()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-header-array.yml", "getTagged", new { xTags = new[] { "a", "b" } });

		var token = AssertRequestSuccess(result, "GET", "/tagged");
		Assert.Equal("a,b", token?["headers"]?["X-Tags"]?.GetValue<string>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): number and boolean header parameters fail tsc (not assignable to string)")]
	public async Task Send_non_string_headers()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-header-scalars.yml", "getCounted", new { xCount = 5, xRatio = 1.5, xEnabled = true });

		var token = AssertRequestSuccess(result, "GET", "/counted");
		Assert.Equal("5", token?["headers"]?["X-Count"]?.GetValue<string>());
		Assert.Equal("1.5", token?["headers"]?["X-Ratio"]?.GetValue<string>());
		Assert.Equal("true", token?["headers"]?["X-Enabled"]?.GetValue<string>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Request bodies): text/plain request bodies are dropped from the operation")]
	public async Task Send_text_bodies()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-request-bodies.yaml", "sendText", new { }, "hello", "text/plain");

		AssertRequestSuccess(result, "POST", "/text", "hello", "text/plain");
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Request bodies): application/octet-stream bodies throw 'Non-JSON requests not supported'")]
	public async Task Send_binary_bodies()
	{
		var result = await commonDirectory.TsNode(@"
			import { conversion } from './oas-client-request-bodies.yaml/operations/sendBinary';
			const request = conversion.request({}, new Blob([new Uint8Array([1, 2, 3])]), 'application/octet-stream');
			(async () => {
				const bytes = request.body instanceof Blob ? Array.from(new Uint8Array(await request.body.arrayBuffer())) : null;
				console.log(JSON.stringify({ contentType: request.headers?.['Content-Type'], bytes }));
			})();
		");

		Assert.Equal(0, result.ExitCode);
		CompareJson(JsonNode.Parse(result.Output), new { contentType = "application/octet-stream", bytes = new[] { 1, 2, 3 } });
	}

	[Fact]
	public async Task Send_the_json_media_type_of_a_request_body_with_several()
	{
		var body = new { text = "hello" };

		var result = await commonDirectory.ConvertRequest("oas-client-request-bodies.yaml", "sendEither", new { }, body);

		AssertRequestSuccess(result, "POST", "/either", body);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Request bodies): only the JSON media type of a request body is kept; text/plain is dropped")]
	public async Task Send_the_text_media_type_of_a_request_body_with_several()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-request-bodies.yaml", "sendEither", new { }, "hello", "text/plain");

		AssertRequestSuccess(result, "POST", "/either", "hello", "text/plain");
	}

	[Fact]
	public async Task Omit_an_optional_json_request_body()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-request-bodies.yaml", "saveNote", new { });

		AssertRequestSuccess(result, "PUT", "/optional");
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Forms and multipart): multipart/form-data request bodies are dropped from the operation")]
	public async Task Send_multipart_form_data()
	{
		var result = await commonDirectory.TsNode(@"
			import { conversion } from './oas-client-multipart.yaml/operations/upload';
			const request = conversion.request({}, { description: 'd', file: new Blob(['abc']) }, 'multipart/form-data');
			(async () => {
				const body = request.body as FormData;
				console.log(JSON.stringify({ description: body.get('description'), file: await (body.get('file') as Blob).text() }));
			})();
		");

		Assert.Equal(0, result.ExitCode);
		CompareJson(JsonNode.Parse(result.Output), new { description = "d", file = "abc" });
	}

	[Fact(Skip = "OpenAPI 3.0 gap (XML): XML request bodies are dropped from the operation")]
	public async Task Send_xml_bodies()
	{
		var result = await commonDirectory.TsNode(@"
			import { conversion } from './oas-client-xml.yaml/operations/postXmlItem';
			const request = conversion.request({}, { name: 'n' }, 'application/xml');
			console.log(JSON.stringify({ contentType: request.headers?.['Content-Type'], body: request.body }));
		");

		Assert.Equal(0, result.ExitCode);
		CompareJson(JsonNode.Parse(result.Output), new { contentType = "application/xml", body = "<item><name>n</name></item>" });
	}

	// The credential parameters are illustrative: the generated client has no way to supply credentials yet
	[Theory(Skip = "OpenAPI 3.0 gap (Client security): security requirements are ignored; there is no way to supply credentials")]
	[InlineData("getWithHeaderKey", "headerKey", "secret", "X-API-Key", "secret")]
	[InlineData("getWithCookieKey", "cookieKey", "secret", "Cookie", "session=secret")]
	[InlineData("getWithBasic", "basic", "dXNlcjpwYXNz", "Authorization", "Basic dXNlcjpwYXNz")]
	[InlineData("getWithBearer", "bearer", "token", "Authorization", "Bearer token")]
	[InlineData("getWithOAuth2", "oauth2", "token", "Authorization", "Bearer token")]
	[InlineData("getWithOpenIdConnect", "openId", "token", "Authorization", "Bearer token")]
	public async Task Apply_credentials_in_headers(string operation, string credentialName, string credential, string headerName, string expected)
	{
		var result = await commonDirectory.ConvertRequest("oas-client-security.yaml", operation, new JsonObject { [credentialName] = credential });

		Assert.Equal(0, result.ExitCode);
		Assert.Equal(expected, JsonNode.Parse(result.Output)?["headers"]?[headerName]?.GetValue<string>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Client security): security requirements are ignored; there is no way to supply credentials")]
	public async Task Apply_api_keys_in_query()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-security.yaml", "getWithQueryKey", new { queryKey = "secret" });

		AssertRequestSuccess(result, "GET", "/query-key?api_key=secret");
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Servers): servers are ignored; requests use the path alone")]
	public async Task Prefix_paths_with_the_default_server()
	{
		var result = await commonDirectory.ConvertRequest("oas-client-servers.yaml", "listItems", new { });

		AssertRequestSuccess(result, "GET", "https://us.example.com/v1/items");
	}

	// The callback URL parameter is illustrative: callbacks are not generated yet
	[Fact(Skip = "OpenAPI 3.0 gap (Callbacks): callback operations are not generated")]
	public async Task Send_callbacks_to_the_expression_url()
	{
		var body = new { id = "e-1" };

		var result = await commonDirectory.ConvertRequest("oas-client-callbacks.yaml", "notifyEvent", new { callbackUrl = "https://example.com/hooks" }, body);

		AssertRequestSuccess(result, "POST", "https://example.com/hooks/events", body);
	}

	private static string GetPath(NodeUtility.ProcessResult result)
	{
		Assert.Equal(0, result.ExitCode);
		return JsonNode.Parse(result.Output)?["path"]?.GetValue<string>() ?? "";
	}

	private static NameValueCollection GetQuery(NodeUtility.ProcessResult result)
	{
		var parts = GetPath(result).Split('?', 2);
		return HttpUtility.ParseQueryString(parts.Length > 1 ? parts[1] : "");
	}
}
