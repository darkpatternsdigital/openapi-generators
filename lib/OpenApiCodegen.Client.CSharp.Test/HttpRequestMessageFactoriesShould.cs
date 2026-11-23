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
		Assert.Equal(["a", "b"], form.GetValues("tags"));
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
		var actualMessage = await GetRequestMessage("form.yaml", "PostBasicForm(name: \"Fido\", tag: \"dog\", hasIdTag: true)");

		Assert.Equal("POST", actualMessage.Method.Method);
		Assert.Equal("basic", actualMessage.RequestUri?.OriginalString);
		var content = Assert.IsType<FormUrlEncodedContent>(actualMessage.Content);
		using var memoryStream = new MemoryStream();
		await content.CopyToAsync(memoryStream);
		var formString = Encoding.ASCII.GetString(memoryStream.ToArray());
		Assert.Equal("name=Fido&tag=dog&hasIdTag=true", formString);
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
