using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.TestUtils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp;

public class HttpRequestMessageFactoriesShould : IClassFixture<TempDirectory>
{
	private readonly string workingDirectory;

	public HttpRequestMessageFactoriesShould(TempDirectory directory)
	{
		workingDirectory = directory.DirectoryPath;
	}

	[Fact]
	public async Task PassBase64EncodedQueryData()
	{
		var actualMessage = await GetRequestMessage("controller-extension.yaml", "GetInfo(data: Optional.Create(new byte[] { 1, 2, 3 }))");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal("api/info?data=AQID", actualMessage.RequestUri?.OriginalString);
		Assert.Null(actualMessage.Content);
	}

	[Fact]
	public async Task FormatParameterValuesIndependentOfCulture()
	{
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		var actualMessage = await GetRequestMessage("parameter-formats.yaml", @"GetFormats(
			name: ""a b/c"",
			when: new System.DateTimeOffset(2026, 11, 22, 16, 45, 0, System.TimeSpan.FromHours(-6)),
			amount: 1.5,
			flag: true,
			size: Size.Small,
			data: new byte[] { 1, 2, 3 },
			xSize: Size.Large)");

		var (path, query) = SplitUri(actualMessage);
		Assert.Equal("formats/a%20b%2Fc", path);
		Assert.Equal("2026-11-22T16:45:00.0000000-06:00", query["when"]);
		Assert.Equal("1.5", query["amount"]);
		Assert.Equal("true", query["flag"]);
		Assert.Equal("small", query["size"]);
		Assert.Equal("AQID", query["data"]);
		Assert.Equal(["large"], actualMessage.Headers.GetValues("X-Size"));
	}

	[Fact]
	public async Task FormatFormFieldValues()
	{
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		var actualMessage = await GetRequestMessage("parameter-formats.yaml", @"PostFormats(
			name: ""n"",
			size: Size.Large,
			when: Optional.Create(new System.DateTimeOffset(2026, 11, 22, 16, 45, 0, System.TimeSpan.FromHours(-6))),
			flag: Optional.Create(false),
			tags: Optional.Create<System.Collections.Generic.IEnumerable<string>>(new[] { ""a"", ""b"" }),
			note: Optional.Create<string?>(null))");

		var form = HttpUtility.ParseQueryString(await actualMessage.Content!.ReadAsStringAsync());
		Assert.Equal("large", form["size"]);
		Assert.Equal("2026-11-22T16:45:00.0000000-06:00", form["when"]);
		Assert.Equal("false", form["flag"]);
		var tags = form.GetValues("tags");
		Assert.NotNull(tags);
		Assert.Equal(["a", "b"], tags);
		// A nullable field that is present with a null value is sent empty
		Assert.Equal("", form["note"]);
	}

	[Fact]
	public async Task OmitMissingOptionalFormFields()
	{
		var actualMessage = await GetRequestMessage("parameter-formats.yaml", @"PostFormats(name: ""n"", size: Size.Small, when: null, flag: null, tags: null, note: null)");

		Assert.Equal("size=small", await actualMessage.Content!.ReadAsStringAsync());
	}

	[Fact]
	public async Task SendWebhookQueryParametersAndOptionalHeaders()
	{
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		var withoutHeader = await GetRequestMessage("parameter-formats.yaml", @"FormatsReceived(
			new System.Uri(""https://example.com/hook?token=x""),
			when: new System.DateTimeOffset(2026, 11, 22, 16, 45, 0, System.TimeSpan.FromHours(-6)),
			xSize: null)", webhooks: true);
		var withHeader = await GetRequestMessage("parameter-formats.yaml", @"FormatsReceived(
			new System.Uri(""https://example.com/hook""),
			when: new System.DateTimeOffset(2026, 11, 22, 16, 45, 0, System.TimeSpan.FromHours(-6)),
			xSize: Optional.Create(Size.Large))", webhooks: true);

		var (path, query) = SplitUri(withoutHeader);
		Assert.Equal("https://example.com/hook", path);
		Assert.Equal("x", query["token"]);
		Assert.Equal("2026-11-22T16:45:00.0000000-06:00", query["when"]);
		Assert.False(withoutHeader.Headers.Contains("X-Size"));
		Assert.Equal(["large"], withHeader.Headers.GetValues("X-Size"));
	}

	private static (string Path, NameValueCollection Query) SplitUri(HttpRequestMessage message)
	{
		var parts = message.RequestUri!.OriginalString.Split('?', 2);
		return (parts[0], HttpUtility.ParseQueryString(parts.Length > 1 ? parts[1] : ""));
	}

	[Fact]
	public async Task SerializeDictionaries()
	{
		var actualMessage = await GetRequestMessage("dictionary-ref.yaml", @"LookupRecord(lookupRecordBody: new() { [""key1""] = ""value1"", [""key\"":2""] = ""value\n2"" })");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("address", actualMessage.RequestUri?.OriginalString);
		Assert.NotNull(actualMessage.Content);
		var jsonContent = await actualMessage.Content!.ReadAsStringAsync();

		CompareJson(jsonContent, new Dictionary<string, string> { ["key1"] = "value1", ["key\":2"] = "value\n2" });
	}

	[Fact]
	public async Task SerializeEnumerations()
	{
		var actualMessage = await GetRequestMessage("enum.yaml", @"PlayRockPaperScissors(playRockPaperScissorsBody: new(Player1: Option.Rock, Player2: Option.Paper))");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("rock-paper-scissors", actualMessage.RequestUri?.OriginalString);
		Assert.NotNull(actualMessage.Content);
		var jsonContent = await actualMessage.Content!.ReadAsStringAsync();

		CompareJson(jsonContent, new { player1 = "rock", player2 = "paper" });
	}

	[Fact]
	public async Task SerializeEnumerationsInQuery()
	{
		var actualMessage = await GetRequestMessage("enum.yaml", @"PlayRockPaperScissorsQuery(player1: Option.Rock, player2: Option.Paper)");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal("rock-paper-scissors-query?player1=rock&player2=paper", actualMessage.RequestUri?.OriginalString);
		Assert.Null(actualMessage.Content);
	}

	[Theory]
	[InlineData("PlayerOne", "player+one")]
	[InlineData("_2", "2")]
	[InlineData("Three", "three!")]
	[InlineData("F0u2", "f0u%7c2")]
	[InlineData("FiE", "fi%25e")]
	public async Task SerializeEnumerationsWithDifficultQueryStringEnums(string enumName, string queryValue)
	{
		var actualMessage = await GetRequestMessage("enum.yaml", $@"DifficultQueryStringEnum(_enum: DifficultQueryStringEnumEnum.{enumName})");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal($"difficult-enum?enum={queryValue}", actualMessage.RequestUri?.OriginalString);
		Assert.Null(actualMessage.Content);
	}

	[Fact]
	public async Task PassBase64EncodedHeaderData()
	{
		var actualMessage = await GetRequestMessage("headers.yaml", "GetInfo(xData: new byte[] { 1, 2, 3 })");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal("info", actualMessage.RequestUri?.OriginalString);
		Assert.Collection(actualMessage.Headers.GetValues("X-Data"),
			value => Assert.Equal("AQID", value)
		);
		Assert.Null(actualMessage.Content);
	}

	[Fact]
	public async Task SendHeaderParametersByName()
	{
		var actualMessage = await GetRequestMessage("headers.yaml", "GetInfo(xData: new byte[] { 1, 2, 3 })");

		Assert.True(actualMessage.Headers.Contains("X-Data"));
	}

	[Fact]
	public async Task IncludePathLevelParameters()
	{
		var actualMessage = await GetRequestMessage("path-level-parameters.yaml", @"DeleteWidget(widgetId: ""w-1"", verbose: true, reason: Optional.Create(""old""))");

		Assert.Equal("DELETE", actualMessage.Method.Method);
		Assert.Equal("widgets/w-1?verbose=true&reason=old", actualMessage.RequestUri?.OriginalString);
	}

	[Fact]
	public async Task HandleInlineObjects()
	{
		var actualMessage = await GetRequestMessage("no-refs.yaml", @"LookupRecord(lookupRecordBody: new(FormattedAddress: ""123 Main St, Richardson, TX 75081"", Location: new (Latitude: 32.9494998, Longitude: -96.7331253)))");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("address", actualMessage.RequestUri?.OriginalString);
		Assert.NotNull(actualMessage.Content);
		var jsonContent = await actualMessage.Content!.ReadAsStringAsync();

		CompareJson(jsonContent, new { formattedAddress = "123 Main St, Richardson, TX 75081", location = new { latitude = 32.9494998, longitude = -96.7331253 } });
	}

	[Fact]
	public async Task HandleOptionalRequestBodyParameters()
	{
		var actualMessage = await GetRequestMessage("nullable-vs-optional.yaml", @"Search(searchBody: new(Name: ""John Smith"", Department: null))");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("search", actualMessage.RequestUri?.OriginalString);
		Assert.NotNull(actualMessage.Content);
		var jsonContent = await actualMessage.Content!.ReadAsStringAsync();

		CompareJson(jsonContent, new { name = "John Smith" });
	}

	[Fact]
	public async Task HandleOptionalRequestBodyParametersThatAreProvided()
	{
		var actualMessage = await GetRequestMessage("nullable-vs-optional.yaml", @"Search(searchBody: new(Name: ""John Smith"", Department: Optional.Create(""Engineering"")))");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("search", actualMessage.RequestUri?.OriginalString);
		Assert.NotNull(actualMessage.Content);
		var jsonContent = await actualMessage.Content!.ReadAsStringAsync();

		CompareJson(jsonContent, new { name = "John Smith", department = "Engineering" });
	}

	[Fact]
	public async Task HandleWhenAnOptionalParameterIsNullableAndNull()
	{
		var actualMessage = await GetRequestMessage("nullable-vs-optional.yaml", @"Contrived(contrivedBody: new(NullableOnly: null, OptionalOnly: Optional.Create(15), OptionalOrNullable: Optional.Create<int?>(null)))");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("contrived", actualMessage.RequestUri?.OriginalString);
		Assert.NotNull(actualMessage.Content);
		var jsonContent = await actualMessage.Content!.ReadAsStringAsync();

		CompareJson(jsonContent, new { nullableOnly = (int?)null, optionalOnly = 15, optionalOrNullable = (int?)null });
	}

	[Fact]
	public async Task HandleWhenANullableParameterIsOptionalAndNotProvided()
	{
		var actualMessage = await GetRequestMessage("nullable-vs-optional.yaml", @"Contrived(contrivedBody: new(NullableOnly: null, OptionalOnly: null, OptionalOrNullable: null))");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("contrived", actualMessage.RequestUri?.OriginalString);
		Assert.NotNull(actualMessage.Content);
		var jsonContent = await actualMessage.Content!.ReadAsStringAsync();

		CompareJson(jsonContent, new { nullableOnly = (int?)null });
	}

	[Fact]
	public async Task NotAlterOperationsWhenSecurityIsRequired()
	{
		var actualMessage = await GetRequestMessage("oauth.yaml", @"GetInfo()");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal("info", actualMessage.RequestUri?.OriginalString);
	}

	[Fact]
	public async Task AllowQueryInPathWithoutOptionalParameters()
	{
		var actualMessage = await GetRequestMessage("query-in-path.yaml", "TestPath(limit: null)");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal("path?param1=test&param2=test&", actualMessage.RequestUri?.OriginalString);
		Assert.Null(actualMessage.Content);
	}

	[Fact]
	public async Task AllowQueryInPathWithOptionalParameters()
	{
		var actualMessage = await GetRequestMessage("query-in-path.yaml", "TestPath(limit: Optional.Create(15))");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal("path?param1=test&param2=test&limit=15", actualMessage.RequestUri?.OriginalString);
		Assert.Null(actualMessage.Content);
	}

	[Fact]
	public async Task AllowFormEncodedBodies()
	{
		var actualMessage = await GetRequestMessage("form.yaml", "PostBasicForm(name: \"Fido\", tag: Optional.Create(\"dog\"), hasIdTag: true)");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("basic", actualMessage.RequestUri?.OriginalString);
		var content = Assert.IsType<FormUrlEncodedContent>(actualMessage.Content);
		using var memoryStream = new MemoryStream();
		await content.CopyToAsync(memoryStream);
		var formString = Encoding.ASCII.GetString(memoryStream.ToArray());
		Assert.Equal("name=Fido&tag=dog&hasIdTag=true", formString);
	}

	[Fact]
	public async Task OmitAnOptionalFormFieldThatIsNotSet()
	{
		var actualMessage = await GetRequestMessage("form.yaml", "PostBasicForm(name: \"Fido\", tag: null, hasIdTag: false)");

		Assert.Equal("name=Fido&hasIdTag=false", await actualMessage.Content!.ReadAsStringAsync());
	}

	[Fact]
	public async Task SendPutRequestsWithBodies()
	{
		var actualMessage = await GetRequestMessage("oas-client-methods.yaml", @"ReplaceItem(id: ""i-1"", replaceItemBody: new(Name: ""n""))");

		Assert.Equal("PUT", actualMessage.Method.Method);
		Assert.Equal("items/i-1", actualMessage.RequestUri?.OriginalString);
		Assert.Equal("application/json", actualMessage.Content?.Headers.ContentType?.MediaType);
		CompareJson(await actualMessage.Content!.ReadAsStringAsync(), new { name = "n" });
	}

	[Fact]
	public async Task SendPatchRequestsWithAnOptionalBody()
	{
		var actualMessage = await GetRequestMessage("oas-client-methods.yaml", @"UpdateItem(id: ""i-1"", updateItemBody: Optional.Create(new Item(Name: ""n"")))");

		Assert.Equal("PATCH", actualMessage.Method.Method);
		Assert.Equal("items/i-1", actualMessage.RequestUri?.OriginalString);
		CompareJson(await actualMessage.Content!.ReadAsStringAsync(), new { name = "n" });
	}

	[Fact]
	public async Task OmitAnOptionalRequestBody()
	{
		var actualMessage = await GetRequestMessage("oas-client-methods.yaml", @"UpdateItem(id: ""i-1"")");

		Assert.Equal("PATCH", actualMessage.Method.Method);
		Assert.Equal("items/i-1", actualMessage.RequestUri?.OriginalString);
		Assert.Null(actualMessage.Content);
	}

	[Theory]
	[InlineData("oas-client-methods.yaml", @"CheckItem(id: ""i-1"")", "HEAD", "items/i-1")]
	[InlineData("oas-client-methods.yaml", @"ItemOptions(id: ""i-1"")", "OPTIONS", "items/i-1")]
	[InlineData("oas-client-trace.yaml", "TraceEcho()", "TRACE", "echo")]
	public async Task SendEachHttpMethod(string documentName, string operation, string method, string path)
	{
		var actualMessage = await GetRequestMessage(documentName, operation);

		Assert.Equal(method, actualMessage.Method.Method);
		Assert.Equal(path, actualMessage.RequestUri?.OriginalString);
		Assert.Null(actualMessage.Content);
	}

	[Fact]
	public async Task GenerateOperationsWithoutAnOperationId()
	{
		var actualMessage = await GetRequestMessage("oas-client-methods.yaml", "GetUntitled()");

		Assert.Equal("GET", actualMessage.Method.Method);
		Assert.Equal("untitled", actualMessage.RequestUri?.OriginalString);
	}

	[Fact]
	public async Task SubstituteSeveralParametersInOnePathSegment()
	{
		var actualMessage = await GetRequestMessage("oas-client-methods.yaml", @"GetFile(name: ""report"", ext: ""pdf"")");

		Assert.Equal("files/report.pdf", actualMessage.RequestUri?.OriginalString);
	}

	[Fact]
	public async Task SendCookieParameters()
	{
		var actualMessage = await GetRequestMessage("oas-client-cookies.yaml", @"GetSession(session: ""abc"", theme: Optional.Create(""dark""), visits: null)");

		Assert.Equal(["session=abc; theme=dark"], actualMessage.Headers.GetValues("Cookie"));
	}

	[Fact]
	public async Task EncodeCookieValues()
	{
		var actualMessage = await GetRequestMessage("oas-client-cookies.yaml", @"GetSession(session: ""a;b=c"", theme: null, visits: Optional.Create(3))");

		Assert.Equal(["session=a%3Bb%3Dc; visits=3"], actualMessage.Headers.GetValues("Cookie"));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): path arrays are formatted with ToString instead of comma-separated (simple style)")]
	public async Task SendSimpleStylePathArrays()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", "GetSimple(ids: new[] { 1, 2, 3 })");

		Assert.Equal("simple/1,2,3", Uri.UnescapeDataString(actualMessage.RequestUri!.OriginalString));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): style: label is ignored")]
	public async Task SendLabelStylePathArrays()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", "GetLabel(ids: new[] { 1, 2, 3 })");

		Assert.Equal("label/.1,2,3", Uri.UnescapeDataString(actualMessage.RequestUri!.OriginalString));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): style: matrix is ignored")]
	public async Task SendMatrixStylePathArrays()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", "GetMatrix(ids: new[] { 1, 2, 3 })");

		Assert.Equal("matrix/;ids=1,2,3", Uri.UnescapeDataString(actualMessage.RequestUri!.OriginalString));
	}

	[Fact]
	public async Task RepeatExplodedQueryArrays()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", @"GetFormExplode(tags: new[] { ""a"", ""b"" })");

		Assert.Equal("form-explode?tags=a&tags=b", actualMessage.RequestUri?.OriginalString);
	}

	[Theory(Skip = "OpenAPI 3.0 gap (Parameter style): explode: false and delimited styles are ignored; arrays are always sent as repeated keys")]
	[InlineData("GetFormNoExplode", "a,b")]
	[InlineData("GetSpaceDelimited", "a b")]
	[InlineData("GetPipeDelimited", "a|b")]
	public async Task DelimitQueryArraysThatAreNotExploded(string operation, string expected)
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", $@"{operation}(tags: new[] {{ ""a"", ""b"" }})");

		var (_, query) = SplitUri(actualMessage);
		Assert.Equal([expected], query.GetValues("tags") ?? Array.Empty<string>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): style: deepObject is ignored; the object is sent with ToString")]
	public async Task SendDeepObjectQueryParameters()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", @"GetDeepObject(filter: new(Color: ""red"", Size: ""large""))");

		var (_, query) = SplitUri(actualMessage);
		Assert.Equal("red", query["filter[color]"]);
		Assert.Equal("large", query["filter[size]"]);
		Assert.Null(query["filter"]);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): exploded form objects are sent with ToString instead of one key per property")]
	public async Task SendExplodedFormObjectQueryParameters()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", @"GetFormObject(filter: new(Color: ""red"", Size: ""large""))");

		var (_, query) = SplitUri(actualMessage);
		Assert.Equal("red", query["color"]);
		Assert.Equal("large", query["size"]);
		Assert.Null(query["filter"]);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): allowReserved is ignored; reserved characters are percent-encoded")]
	public async Task SendReservedCharactersWhenAllowed()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", @"GetReserved(path: ""a/b:c"")");

		Assert.Equal("reserved?path=a/b:c", actualMessage.RequestUri?.OriginalString);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): content-based parameters are typed object and sent with ToString instead of as JSON")]
	public async Task SerializeContentParametersAsTheirMediaType()
	{
		var actualMessage = await GetRequestMessage("oas-client-param-styles.yaml", @"GetContentParam(filter: new Filter(Color: ""red"", Size: ""large""))");

		var (_, query) = SplitUri(actualMessage);
		CompareJson(query["filter"]!, new { color = "red", size = "large" });
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): array header parameters don't compile ('Select' without System.Linq)")]
	public async Task SendArrayHeadersCommaSeparated()
	{
		var actualMessage = await GetRequestMessage("oas-client-header-array.yml", @"GetTagged(xTags: new[] { ""a"", ""b"" })");

		var values = string.Join(",", actualMessage.Headers.GetValues("X-Tags")).Split(',').Select(v => v.Trim());
		Assert.Equal(["a", "b"], values);
	}

	[Fact]
	public async Task SendNonStringHeaders()
	{
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		var actualMessage = await GetRequestMessage("oas-client-header-scalars.yml", "GetCounted(xCount: 5, xRatio: 1.5, xEnabled: true)");

		Assert.Equal(["5"], actualMessage.Headers.GetValues("X-Count"));
		Assert.Equal(["1.5"], actualMessage.Headers.GetValues("X-Ratio"));
		Assert.Equal(["true"], actualMessage.Headers.GetValues("X-Enabled"));
	}

	[Fact]
	public async Task SendTextBodiesAsText()
	{
		var actualMessage = await GetRequestMessage("oas-client-request-bodies.yaml", @"SendText(sendTextBody: ""hello"")");

		Assert.Equal("text/plain", actualMessage.Content?.Headers.ContentType?.MediaType);
		Assert.Equal("hello", await actualMessage.Content!.ReadAsStringAsync());
	}

	[Fact]
	public async Task SendBinaryBodies()
	{
		var actualMessage = await GetRequestMessage("oas-client-request-bodies.yaml", "SendBinary(sendBinaryBody: new System.IO.MemoryStream(new byte[] { 1, 2, 3 }))");

		Assert.Equal("application/octet-stream", actualMessage.Content?.Headers.ContentType?.MediaType);
		Assert.Equal([1, 2, 3], await actualMessage.Content!.ReadAsByteArrayAsync());
	}

	[Fact]
	public async Task SendTheJsonMediaTypeOfARequestBodyWithSeveral()
	{
		var actualMessage = await GetRequestMessage("oas-client-request-bodies.yaml", @"SendEither(sendEitherBody: new Note(Text: ""hello""))");

		Assert.Equal("application/json", actualMessage.Content?.Headers.ContentType?.MediaType);
		CompareJson(await actualMessage.Content!.ReadAsStringAsync(), new { text = "hello" });
	}

	[Fact]
	public async Task SendTheTextMediaTypeOfARequestBodyWithSeveral()
	{
		var actualMessage = await GetRequestMessage("oas-client-request-bodies.yaml", @"SendEither(sendEitherBody: ""hello"")");

		Assert.Equal("text/plain", actualMessage.Content?.Headers.ContentType?.MediaType);
		Assert.Equal("hello", await actualMessage.Content!.ReadAsStringAsync());
	}

	[Fact]
	public async Task OmitAnOptionalJsonRequestBody()
	{
		var withoutBody = await GetRequestMessage("oas-client-request-bodies.yaml", "SaveNote()");
		var withBody = await GetRequestMessage("oas-client-request-bodies.yaml", @"SaveNote(saveNoteBody: Optional.Create(new Note(Text: ""hello"")))");

		Assert.Equal("PUT", withoutBody.Method.Method);
		Assert.Null(withoutBody.Content);
		CompareJson(await withBody.Content!.ReadAsStringAsync(), new { text = "hello" });
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Forms and multipart): multipart/form-data bodies are sent as JSON labelled multipart/form-data")]
	public async Task SendMultipartFormData()
	{
		var actualMessage = await GetRequestMessage("oas-client-multipart.yaml", @"Upload(uploadBody: new(Description: Optional.Create(""d""), File: new System.IO.MemoryStream(new byte[] { 1, 2, 3 })))");

		var content = Assert.IsType<MultipartFormDataContent>(actualMessage.Content);
		var parts = content.ToDictionary(p => p.Headers.ContentDisposition!.Name!.Trim('"'));
		Assert.Equal("d", await parts["description"].ReadAsStringAsync());
		Assert.Equal([1, 2, 3], await parts["file"].ReadAsByteArrayAsync());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (XML): an operation whose only request body is XML is not generated")]
	public async Task SendXmlBodies()
	{
		var actualMessage = await GetRequestMessage("oas-client-xml.yaml", @"PostXmlItem(postXmlItemBody: new(Name: ""n""))");

		Assert.Equal("application/xml", actualMessage.Content?.Headers.ContentType?.MediaType);
		Assert.Equal("<item><name>n</name></item>", await actualMessage.Content!.ReadAsStringAsync());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): readOnly properties are required in requests and sent")]
	public async Task NotSendReadOnlyProperties()
	{
		var actualMessage = await GetRequestMessage("oas-client-models.yaml", @"CreateAccount(createAccountBody: new(Password: ""p"", Name: ""n""))");

		CompareJson(await actualMessage.Content!.ReadAsStringAsync(), new { password = "p", name = "n" });
	}

	// The credential arguments are illustrative: the generated client has no way to supply credentials yet
	[Theory(Skip = "OpenAPI 3.0 gap (Client security): security requirements are ignored; there is no way to supply credentials")]
	[InlineData(@"GetWithHeaderKey(headerKey: ""secret"")", "X-API-Key", "secret")]
	[InlineData(@"GetWithCookieKey(cookieKey: ""secret"")", "Cookie", "session=secret")]
	[InlineData(@"GetWithBasic(basic: ""dXNlcjpwYXNz"")", "Authorization", "Basic dXNlcjpwYXNz")]
	[InlineData(@"GetWithBearer(bearer: ""token"")", "Authorization", "Bearer token")]
	[InlineData(@"GetWithOAuth2(oauth2: ""token"")", "Authorization", "Bearer token")]
	[InlineData(@"GetWithOpenIdConnect(openId: ""token"")", "Authorization", "Bearer token")]
	public async Task ApplyCredentialsInHeaders(string operation, string headerName, string expected)
	{
		var actualMessage = await GetRequestMessage("oas-client-security.yaml", operation);

		Assert.Equal([expected], actualMessage.Headers.GetValues(headerName));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Client security): security requirements are ignored; there is no way to supply credentials")]
	public async Task ApplyApiKeysInQuery()
	{
		var actualMessage = await GetRequestMessage("oas-client-security.yaml", @"GetWithQueryKey(queryKey: ""secret"")");

		Assert.Equal("query-key?api_key=secret", actualMessage.RequestUri?.OriginalString);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Servers): servers are ignored; requests use the path alone")]
	public async Task PrefixPathsWithTheDefaultServer()
	{
		var actualMessage = await GetRequestMessage("oas-client-servers.yaml", "ListItems()");

		Assert.Equal("https://us.example.com/v1/items", actualMessage.RequestUri?.OriginalString);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Callbacks): the callback URL expression is ignored; '/events' is not appended to the given URL")]
	public async Task SendCallbacksToTheExpressionUrl()
	{
		var actualMessage = await GetRequestMessage("oas-client-callbacks.yaml", @"NotifyEvent(new System.Uri(""https://example.com/hooks""), notifyEventBody: new(Id: ""e-1""))", webhooks: true);

		Assert.Equal("https://example.com/hooks/events", actualMessage.RequestUri?.OriginalString);
	}

	[Fact]
	public async Task SendCallbackBodies()
	{
		var actualMessage = await GetRequestMessage("oas-client-callbacks.yaml", @"NotifyEvent(new System.Uri(""https://example.com/hooks""), notifyEventBody: new(Id: ""e-1""))", webhooks: true);

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("application/json", actualMessage.Content?.Headers.ContentType?.MediaType);
		CompareJson(await actualMessage.Content!.ReadAsStringAsync(), new { id = "e-1" });
	}

	private async Task<HttpRequestMessage> GetRequestMessage(string documentName, string operationAndParameters, Action<CSharpSchemaOptions>? configureOptions = null, bool webhooks = false)
	{
		var libBytes = DynamicCompilation.GetGeneratedLibrary(documentName, configureOptions, webhooks);

		var fullPath = Path.Combine(workingDirectory, Path.GetRandomFileName());
		File.WriteAllBytes(fullPath, libBytes);

		var scriptOptions = ScriptOptions.Default
			.AddReferences(DynamicCompilation.SystemTextCompilationRefPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray())
			.AddReferences(MetadataReference.CreateFromFile(fullPath))
			.WithImports(
				"DPD.Controller",
				webhooks ? "DPD.Controller.WebhookOperations" : "DPD.Controller.Operations",
				"DarkPatterns.OpenApiCodegen.Json.Extensions"
			);

		return (HttpRequestMessage)await CSharpScript.EvaluateAsync($"{operationAndParameters}", scriptOptions);

	}

	private void CompareJson(string actualJson, object expected)
	{
		Assert.True(JsonCompare.CompareJson(actualJson, expected));
	}
}
