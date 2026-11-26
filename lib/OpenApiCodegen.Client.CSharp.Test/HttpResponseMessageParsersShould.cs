using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp;

public class HttpResponseMessageParsersShould : IClassFixture<TempDirectory>
{
	private readonly string workingDirectory;

	public HttpResponseMessageParsersShould(TempDirectory directory)
	{
		workingDirectory = directory.DirectoryPath;
	}

	[Fact]
	public async Task DeserializeAllOf()
	{
		var result = await DeserializeResponseMessage("all-of.yaml", @"ParseGetContact", 200, "GetContactReturnType.Ok", new { firstName = "John", lastName = "Doe", id = "john-doe-123" });

		Assert.NotNull(result);

		Assert.Equal("John", result.Body.FirstName);
		Assert.Equal("Doe", result.Body.LastName);
		Assert.Equal("john-doe-123", result.Body.Id);
	}

	[Fact]
	public async Task DeserializeAnyWithObject()
	{
		var result = await DeserializeResponseMessage("any.yaml", @"ParseGetData", 200, "GetDataReturnType.Ok", new { foo = "bar" });

		Assert.NotNull(result);

		var body = Assert.IsAssignableFrom<System.Text.Json.Nodes.JsonObject>((object)result.Body);
		Assert.True(body.TryGetPropertyValue("foo", out var fooNode));
		Assert.Equal("bar", fooNode?.GetValue<string>());
	}

	[Fact]
	public async Task DeserializeAnyWithString()
	{
		var result = await DeserializeResponseMessage("any.yaml", @"ParseGetData", 200, "GetDataReturnType.Ok", "foo");

		Assert.NotNull(result);

		var body = Assert.IsAssignableFrom<System.Text.Json.Nodes.JsonValue>((object)result.Body);
		Assert.Equal("foo", body?.GetValue<string>());
	}

	[Fact]
	public async Task DeserializeStringArray()
	{
		var result = await DeserializeResponseMessage("array.yaml", @"ParseGetColors", 200, "GetColorsReturnType.Ok", new[] { "red", "green", "blue" });

		Assert.NotNull(result);

		var body = Assert.IsAssignableFrom<IEnumerable<string>>((object)result.Body);
		Assert.Collection(body,
			(elem) => Assert.Equal("red", elem),
			(elem) => Assert.Equal("green", elem),
			(elem) => Assert.Equal("blue", elem));
	}

	[Fact]
	public async Task DeserializeArrayOfStringArray()
	{
		var result = await DeserializeResponseMessage("array.yaml", @"ParseGetPalettes", 200, "GetPalettesReturnType.Ok", new[] { new[] { "red", "green", "blue" }, new[] { "cyan", "magenta", "yellow" } });

		Assert.NotNull(result);

		var body = Assert.IsAssignableFrom<IEnumerable<IEnumerable<string>>>((object)result.Body);
		Assert.Collection(body,
			(palette) => Assert.Collection(palette,
				(elem) => Assert.Equal("red", elem),
				(elem) => Assert.Equal("green", elem),
				(elem) => Assert.Equal("blue", elem)),
			(palette) => Assert.Collection(palette,
				(elem) => Assert.Equal("cyan", elem),
				(elem) => Assert.Equal("magenta", elem),
				(elem) => Assert.Equal("yellow", elem)));
	}

	[Fact]
	public async Task DeserializeDictionary()
	{
		var result = await DeserializeResponseMessage("dictionary-ref.yaml", @"ParseLookupRecord", 200, "LookupRecordReturnType.Ok", new { foo = "bar", boo = "baz" });

		Assert.NotNull(result);

		var body = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>((object)result.Body);
		Assert.Equal(2, body.Count);
		Assert.True(body.ContainsKey("foo"));
		Assert.Equal("bar", body["foo"]);
		Assert.True(body.ContainsKey("boo"));
		Assert.Equal("baz", body["boo"]);
	}

	[Fact]
	public async Task DeserializeEnum()
	{
		var result = await DeserializeResponseMessage("enum.yaml", @"ParsePlayRockPaperScissors", 200, "PlayRockPaperScissorsReturnType.Ok", "player1");

		Assert.NotNull(result);

		Assert.Equal("Player1", (string)result.Body.ToString("g"));
	}

	[Fact]
	public async Task DeserializeConflictStatusCode()
	{
		var result = await DeserializeResponseMessage("enum.yaml", @"ParsePlayRockPaperScissors", 409, "PlayRockPaperScissorsReturnType.Conflict");

		Assert.NotNull(result);
	}

	[Fact]
	public async Task DeserializeResponseHeader()
	{
		var result = await DeserializeResponseMessage("headers.yaml", @"ParseGetInfo", 204, "GetInfoReturnType.NoContent", msg =>
		{
			msg.Headers.Add("X-Data", "custom-header-value");
		});

		Assert.NotNull(result);

		HttpResponseMessage rawMessage = Assert.IsAssignableFrom<HttpResponseMessage>(result.Response);
		Assert.Collection(rawMessage.Headers.GetValues("X-Data"), v => Assert.Equal("custom-header-value", v));

		// TODO - custom headers should probably be able to be accessed via strongly typed props
		// Assert.Equal("custom-header-value", (string)result.XData);
	}

	[Fact]
	public async Task DeserializeDeepObjectCreatedFromNoRefs()
	{
		var addr1 = new { formattedAddress = "123 Main St, Richardson, TX 75081", location = new { latitude = 32.9494998, longitude = -96.7331253 } };
		var addr2 = new { formattedAddress = "123 Main St, Dallas, TX 75252", location = new { latitude = 32.7829188, longitude = -96.7906893 } };
		var result = await DeserializeResponseMessage("no-refs.yaml", @"ParseLookupRecord", 409, "LookupRecordReturnType.Conflict", new
		{
			multiple = new[] {
				addr1,
				addr2,
			}
		});

		Assert.NotNull(result);

		// .Value is needed because Multiple is optional - normally this would use an extension method, but I can't via Optional.
		var resultAddresses = Assert.IsAssignableFrom<IEnumerable<object>>((object)result.Body.Multiple.Value);
		Assert.Collection(resultAddresses,
			(dynamic entry) => Assert.Equal(addr1.formattedAddress, entry.FormattedAddress),
			(dynamic entry) => Assert.Equal(addr2.formattedAddress, entry.FormattedAddress)
		);
	}

	[Fact]
	public async Task DeserializeOptionalNullValuesAsPresent()
	{
		var result = await DeserializeResponseMessage("nullable-vs-optional.yaml", @"ParseContrived", 200, "ContrivedReturnType.Ok", new
		{
			nullableOnly = (int?)null,
			optionalOnly = 15,
			optionalOrNullable = (int?)null,
		});

		Assert.NotNull(result);

		Assert.Null((object)result.Body.NullableOnly);
		var optionalOnly = Assert.IsAssignableFrom<Optional<int>>((object)result.Body.OptionalOnly);
		Assert.Equal(15, optionalOnly.GetValueOrDefault());
		var optionalOrNullable = Assert.IsAssignableFrom<Optional<int?>>((object)result.Body.OptionalOrNullable);
		Assert.NotNull(optionalOrNullable);
		Assert.True(optionalOrNullable.TryGet(out var nullable));
		Assert.Null(nullable);
	}

	[Fact]
	public async Task DeserializeOptionalNullValuesAsNotPresent()
	{
		var result = await DeserializeResponseMessage("nullable-vs-optional.yaml", @"ParseContrived", 200, "ContrivedReturnType.Ok", new
		{
			nullableOnly = (int?)null,
		});

		Assert.NotNull(result);

		Assert.Null((object)result.Body.NullableOnly);
		Assert.Null(result.Body.OptionalOnly);
		Assert.Null(result.Body.OptionalOrNullable);
	}

	[Fact]
	public async Task DeserializeSelfReferentialObjects()
	{
		var result = await DeserializeResponseMessage("self-ref.yaml", @"ParseGetData", 200, "GetDataReturnType.Ok", new
		{
			id = 1,
			target = new { id = 2 },
		});

		Assert.NotNull(result);

		Assert.NotNull(result.Body._Target);
		Assert.Equal(1, result.Body.Id);
		Assert.Null(result.Body._Target.Value._Target);
		Assert.Equal(2, result.Body._Target.Value.Id);
	}

	// Using dynamic for this because it is returned from an EvaluateAsync script of a type we don't have - it was generated from the yaml during runtime!
	private Task<dynamic> DeserializeResponseMessage(string documentName, string operation, int statusCode, string parsedType, object? jsonBody, Action<HttpResponseMessage>? additionalMessageSetup = null) =>
		DeserializeResponseMessage(documentName, operation, statusCode, parsedType, responseMessage =>
		{
			responseMessage.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");

			additionalMessageSetup?.Invoke(responseMessage);
		});

	[Fact]
	public async Task ReadTextResponsesAsStrings()
	{
		var result = await DeserializeResponseMessage("response-media-types.yaml", "ParseGetReport", 200, "GetReportReturnType.TextPlainOk", msg =>
		{
			msg.Content = new StringContent("hello", Encoding.UTF8, "text/plain");
		});

		Assert.NotNull(result);
		Assert.Equal("hello", (string)result.Body);
	}

	[Fact]
	public async Task ReturnOtherMediaTypesAsContent()
	{
		var result = await DeserializeResponseMessage("response-media-types.yaml", "ParseGetReport", 200, "GetReportReturnType.ApplicationOctetStreamOk", msg =>
		{
			msg.Content = new ByteArrayContent([1, 2, 3]);
			msg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
		});

		Assert.NotNull(result);
		HttpContent body = Assert.IsAssignableFrom<HttpContent>(result.Body);
		Assert.Equal([1, 2, 3], await body.ReadAsByteArrayAsync());
	}

	[Fact]
	public async Task MatchMediaTypesIgnoringParametersAndCase()
	{
		var result = await DeserializeResponseMessage("response-media-types.yaml", "ParseGetReport", 200, "GetReportReturnType.ApplicationJsonOk", msg =>
		{
			msg.Content = new StringContent(@"{""name"":""quarterly""}", Encoding.UTF8);
			msg.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("APPLICATION/JSON; charset=utf-8");
		});

		Assert.NotNull(result);
		Assert.Equal("quarterly", (string)result.Body.Name);
	}

	[Fact]
	public async Task HandleAResponseWithoutContentType()
	{
		var result = await DeserializeResponseMessage("response-media-types.yaml", "ParseGetReport", 200, "GetReportReturnType", msg =>
		{
			msg.Content = new ByteArrayContent([1, 2, 3]);
		});

		Assert.NotNull(result);
	}

	[Fact]
	public async Task PreferAnExactStatusCodeOverARange()
	{
		var result = await DeserializeResponseMessage("oas-client-responses.yaml", "ParseGetRanges", 200, "GetRangesReturnType.Ok", new { id = "i-1" });

		Assert.NotNull(result);
		Assert.Equal("i-1", (string)result.Body.Id);
	}

	[Theory(Skip = "OpenAPI 3.0 gap (Response ranges): 2XX and 4XX responses are dropped, so their bodies are not parsed")]
	[InlineData(201, "{\"id\":\"i-1\"}", "Id", "i-1")]
	[InlineData(404, "{\"message\":\"missing\"}", "Message", "missing")]
	public async Task ParseStatusCodeRangeResponses(int statusCode, string json, string property, string expected)
	{
		var result = await DeserializeResponseMessage("oas-client-responses.yaml", "ParseGetRanges", statusCode, "GetRangesReturnType", msg =>
		{
			msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
		});

		Assert.NotNull(result);
		object body = result.Body;
		Assert.Equal(expected, body.GetType().GetProperty(property)?.GetValue(body));
	}

	[Fact]
	public async Task ParseDefaultResponses()
	{
		var result = await DeserializeResponseMessage("oas-client-responses.yaml", "ParseGetWithDefault", 503, "GetWithDefaultReturnType.OtherStatusCode", new { message = "unavailable" });

		Assert.NotNull(result);
		Assert.Equal("unavailable", (string)result.Body.Message);
		Assert.Equal(503, (int)result.Response.StatusCode);
	}

	[Fact]
	public async Task ReturnUndocumentedStatusCodesWithTheResponse()
	{
		var result = await DeserializeResponseMessage("oas-client-responses.yaml", "ParseGetItem", 500, "GetItemReturnType", new { message = "oops" });

		Assert.NotNull(result);
		Assert.Equal("GetItemReturnType", ((object)result).GetType().Name);
		Assert.Equal(500, (int)result.Response.StatusCode);
	}

	// The property name is illustrative: response headers have no typed accessors yet
	[Fact(Skip = "OpenAPI 3.0 gap (Response headers): declared response headers are not parsed into typed properties")]
	public async Task ParseTypedResponseHeaders()
	{
		var result = await DeserializeResponseMessage("oas-client-responses.yaml", "ParseGetWithHeaders", 200, "GetWithHeadersReturnType.Ok", new { id = "i-1" }, msg =>
		{
			msg.Headers.Add("X-Rate-Limit", "10");
			msg.Headers.Add("X-Request-Id", "r-1");
		});

		Assert.NotNull(result);
		Assert.Equal(10, (int)result.XRateLimit);
		Assert.Equal("r-1", (string)result.XRequestId.Value);
	}

	[Fact]
	public async Task ParseResponsesWithLinks()
	{
		var result = await DeserializeResponseMessage("oas-client-responses.yaml", "ParseCreateItem", 201, "CreateItemReturnType.Created", new { id = "i-1" });

		Assert.NotNull(result);
		Assert.Equal("i-1", (string)result.Body.Id);
	}

	[Fact]
	public async Task ParseHeadResponses()
	{
		var result = await DeserializeResponseMessage("oas-client-methods.yaml", "ParseCheckItem", 404, "CheckItemReturnType.NotFound");

		Assert.NotNull(result);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (XML): XML response bodies are returned as raw HttpContent instead of the schema's type")]
	public async Task ParseXmlResponses()
	{
		var result = await DeserializeResponseMessage("oas-client-xml.yaml", "ParseGetXmlItem", 200, "GetXmlItemReturnType.Ok", msg =>
		{
			msg.Content = new StringContent("<item><name>n</name></item>", Encoding.UTF8, "application/xml");
		});

		Assert.NotNull(result);
		Assert.Equal("n", (string)result.Body.Name);
	}

	[Fact]
	public async Task DeserializeAdditionalPropertiesAsADictionary()
	{
		var result = await DeserializeResponseMessage("oas-client-models.yaml", "ParseGetLabels", 200, "GetLabelsReturnType.Ok", new { a = "x", b = "y" });

		Assert.NotNull(result);
		var body = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>((object)result.Body);
		Assert.Equal("x", body["a"]);
		Assert.Equal("y", body["b"]);
	}

	// The property name is illustrative: additional properties are not kept yet
	[Fact(Skip = "OpenAPI 3.0 gap (Models): additional properties alongside named properties are dropped")]
	public async Task KeepAdditionalPropertiesAlongsideNamedProperties()
	{
		var result = await DeserializeResponseMessage("oas-client-models.yaml", "ParseGetTagged", 200, "GetTaggedReturnType.Ok", new { name = "n", color = "red" });

		Assert.NotNull(result);
		Assert.Equal("n", (string)result.Body.Name);
		Assert.Equal("red", (string)result.Body.AdditionalProperties["color"]);
	}

	[Fact]
	public async Task DeserializeObjectsWithoutAdditionalProperties()
	{
		var result = await DeserializeResponseMessage("oas-client-models.yaml", "ParseGetClosed", 200, "GetClosedReturnType.Ok", new { name = "n" });

		Assert.NotNull(result);
		Assert.Equal("n", (string)result.Body.Name);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): writeOnly properties are required in responses, so a response without them fails to parse")]
	public async Task NotRequireWriteOnlyPropertiesInResponses()
	{
		var result = await DeserializeResponseMessage("oas-client-models.yaml", "ParseCreateAccount", 201, "CreateAccountReturnType.Created", new { id = "a-1", name = "n" });

		Assert.NotNull(result);
		Assert.Equal("a-1", (string)result.Body.Id);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): default values are not applied to missing properties")]
	public async Task ApplyDefaultValuesToMissingProperties()
	{
		var result = await DeserializeResponseMessage("oas-client-models.yaml", "ParseGetSettings", 200, "GetSettingsReturnType.Ok", new { });

		Assert.NotNull(result);
		Assert.Equal(20, (int)result.Body.PageSize.Value);
	}

	[Fact]
	public async Task DeserializeRecursiveModels()
	{
		var result = await DeserializeResponseMessage("oas-client-models.yaml", "ParseGetTree", 200, "GetTreeReturnType.Ok", new
		{
			value = "root",
			children = new[] { new { value = "leaf" } },
		});

		Assert.NotNull(result);
		Assert.Equal("root", (string)result.Body.Value);
		var children = Assert.IsAssignableFrom<IEnumerable<object>>((object)result.Body.Children.Value);
		Assert.Collection(children, (dynamic child) => Assert.Equal("leaf", (string)child.Value));
	}

	[Fact]
	public async Task DeserializeStringAndNumberFormats()
	{
		var result = await DeserializeResponseMessage("oas-client-models.yaml", "ParseGetFormats", 200, "GetFormatsReturnType.Ok", new
		{
			id = "0f8fad5b-d9cb-469f-a165-70867728950e",
			day = "2026-11-25",
			when = "2026-11-25T16:45:00-06:00",
			big = 9007199254740993L,
			ratio = 1.5,
		});

		Assert.NotNull(result);
		Assert.Equal(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"), (Guid)result.Body.Id);
		Assert.Equal(new DateOnly(2026, 11, 25), (DateOnly)result.Body.Day);
		Assert.Equal(new DateTimeOffset(2026, 11, 25, 16, 45, 0, TimeSpan.FromHours(-6)), (DateTimeOffset)result.Body.When);
		Assert.Equal(9007199254740993L, (long)result.Body.Big);
		Assert.Equal(1.5f, (float)result.Body.Ratio);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Non-string enums): integer enums are generated as a plain int")]
	public async Task DeserializeIntegerEnumsAsEnums()
	{
		var result = await DeserializeResponseMessage("oas-client-enums.yml", "ParseGetPriority", 200, "GetPriorityReturnType.Ok", 2);

		Assert.NotNull(result);
		object body = result.Body;
		Assert.True(body.GetType().IsEnum);
		Assert.Equal(2, Convert.ToInt32(body, System.Globalization.CultureInfo.InvariantCulture));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Non-string enums): a nullable enum that lists null fails generation (NotSupportedException)")]
	public async Task DeserializeNullableEnums()
	{
		var result = await DeserializeResponseMessage("oas-client-nullable-enum.yaml", "ParseGetStatus", 200, "GetStatusReturnType.Ok", (object?)null);

		Assert.NotNull(result);
		Assert.Null((object?)result.Body);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): anyOf schemas are not generated, so the client doesn't compile")]
	public async Task DeserializeAnyOf()
	{
		var result = await DeserializeResponseMessage("oas-client-any-of.yml", "ParseGetShape", 200, "GetShapeReturnType.Ok", new { radius = 1.5 });

		Assert.NotNull(result);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): a schema using not reports UnableToCreateInlineSchemaDiagnostic")]
	public async Task DeserializeNot()
	{
		var result = await DeserializeResponseMessage("oas-client-not.yml", "ParseGetNotString", 200, "GetNotStringReturnType.Ok", 5);

		Assert.NotNull(result);
	}

	private async Task<dynamic> DeserializeResponseMessage(string documentName, string operation, int statusCode, string parsedType, Action<HttpResponseMessage>? additionalMessageSetup = null)
	{
		// Not disposed here: results such as HttpContent bodies are read after this returns
		var responseMessage = new HttpResponseMessage((System.Net.HttpStatusCode)statusCode);
		additionalMessageSetup?.Invoke(responseMessage);

		var libBytes = DynamicCompilation.GetGeneratedLibrary(documentName);

		var fullPath = Path.Combine(workingDirectory, Path.GetRandomFileName());
		File.WriteAllBytes(fullPath, libBytes);

		var scriptOptions = ScriptOptions.Default
			.AddReferences(DynamicCompilation.SystemTextCompilationRefPaths.Select(r => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(r)).ToArray())
			.AddReferences(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(fullPath))
			.WithImports(
				"DPD.Controller",
				"DPD.Controller.Operations",
				"DarkPatterns.OpenApiCodegen.Json.Extensions"
			);

		return await CSharpScript.EvaluateAsync($@"
            var parsed = await DPD.Controller.Operations.{operation}(Message);

            parsed is {parsedType} ? parsed : null
        ", scriptOptions, globals: new ResponseMessageGlobals(responseMessage));
	}

	public record ResponseMessageGlobals(HttpResponseMessage Message);
}
