using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using static DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils.GenerationUtilities;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Integration;

/// <summary>Models and documentation generated from the OpenAPI 3.0.3 coverage fixtures (schemas/oas-client-*.yaml)</summary>
[Collection(CommonDirectoryFixture.CollectionName)]
public class OasClientModelsShould
{
	private readonly CommonDirectoryFixture commonDirectory;

	public OasClientModelsShould(CommonDirectoryFixture commonDirectory)
	{
		this.commonDirectory = commonDirectory;
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): additionalProperties generates an empty type ({}) without an index signature")]
	public async Task Type_additional_properties()
	{
		var result = await commonDirectory.TsNode(@"
			import { Labels } from './oas-client-models.yaml/models/Labels';
			const labels: Labels = { a: 'x' };
			const value: string | undefined = labels['a'];
			console.log(value);
		");

		Assert.Equal(0, result.ExitCode);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): additionalProperties alongside named properties are not allowed by the type")]
	public async Task Allow_additional_properties_alongside_named_properties()
	{
		var result = await commonDirectory.CheckModel("oas-client-models.yaml", "Tagged", new { name = "n", color = "red" });

		Assert.Equal(0, result.ExitCode);
	}

	[Fact]
	public async Task Reject_additional_properties_when_false()
	{
		var result = await commonDirectory.CheckModel("oas-client-models.yaml", "Closed", new { name = "n", extra = "x" });

		Assert.NotEqual(0, result.ExitCode);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): readOnly properties are required in request bodies")]
	public async Task Not_require_read_only_properties_in_requests()
	{
		var body = new { password = "p", name = "n" };

		var result = await commonDirectory.ConvertRequest("oas-client-models.yaml", "createAccount", new { }, body);

		AssertRequestSuccess(result, "POST", "/accounts", body);
	}

	[Fact]
	public async Task Allow_recursive_models()
	{
		var result = await commonDirectory.CheckModel("oas-client-models.yaml", "Tree", new { value = "root", children = new[] { new { value = "leaf", children = new object[0] } } });

		Assert.Equal(0, result.ExitCode);
	}

	[Fact]
	public async Task Map_string_and_number_formats()
	{
		var result = await commonDirectory.CheckModel("oas-client-models.yaml", "Formats", new
		{
			id = "0f8fad5b-d9cb-469f-a165-70867728950e",
			day = "2026-11-25",
			when = "2026-11-25T16:45:00-06:00",
			big = 1,
			ratio = 1.5,
		});

		Assert.Equal(0, result.ExitCode);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Non-string enums): integer enums fail tsc (the barrel exports a 'priority' value that isn't generated)")]
	public async Task Restrict_integer_enums()
	{
		var valid = await commonDirectory.CheckModel("oas-client-enums.yml", "Priority", 2);
		var invalid = await commonDirectory.CheckModel("oas-client-enums.yml", "Priority", 4);

		Assert.Equal(0, valid.ExitCode);
		Assert.NotEqual(0, invalid.ExitCode);
	}

	[Fact]
	public async Task Allow_null_in_nullable_enums()
	{
		var nullValue = await commonDirectory.CheckModel("oas-client-nullable-enum.yaml", "Status", null!);
		var listed = await commonDirectory.CheckModel("oas-client-nullable-enum.yaml", "Status", "active");
		var unlisted = await commonDirectory.CheckModel("oas-client-nullable-enum.yaml", "Status", "other");

		Assert.Equal(0, nullValue.ExitCode);
		Assert.Equal(0, listed.ExitCode);
		Assert.NotEqual(0, unlisted.ExitCode);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): anyOf schemas are not generated, so the output fails tsc")]
	public async Task Allow_any_of_the_listed_schemas()
	{
		var result = await commonDirectory.CheckModel("oas-client-any-of.yml", "Shape", new { radius = 1.5 });

		Assert.Equal(0, result.ExitCode);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Models): schemas using not are not generated, so the output fails tsc")]
	public async Task Generate_models_for_not()
	{
		var result = await commonDirectory.CheckModel("oas-client-not.yml", "NotString", 5);

		Assert.Equal(0, result.ExitCode);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): deprecated operations are not marked @deprecated")]
	public void Mark_deprecated_operations()
	{
		Assert.Contains("@deprecated", ReadGenerated("oas-client-methods.yaml/operations/getLegacy.ts"));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): operation summaries and descriptions are not emitted")]
	public void Document_operation_summaries_and_descriptions()
	{
		var operation = ReadGenerated("oas-client-methods.yaml/operations/getLegacy.ts");

		Assert.Contains("Gets the legacy value", operation);
		Assert.Contains("Use replaceItem instead.", operation);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): deprecated properties are not marked @deprecated")]
	public void Mark_deprecated_properties()
	{
		Assert.Contains("@deprecated", ReadGenerated("oas-client-models.yaml/models/Account.ts"));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): property descriptions are not emitted as JSDoc")]
	public void Document_property_descriptions()
	{
		Assert.Contains("The display name of the account", ReadGenerated("oas-client-models.yaml/models/Account.ts"));
	}

	private string ReadGenerated(string relativePath) =>
		File.ReadAllText(Path.Combine(commonDirectory.DirectoryPath, relativePath));
}
