using System.Linq;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// Checks that documentation in oas-server-documentation.yml reaches the generated code as XML documentation.
/// </summary>
public class DocumentationOutputShould
{
	private static string GetSource(string fileNameEnd) =>
		DynamicCompilation.GetGeneratedSources("oas-server-documentation.yml")
			.Single(e => e.Key.EndsWith("." + fileNameEnd, System.StringComparison.Ordinal))
			.Value;

	[Fact]
	public void Document_operations_with_their_summary_and_description()
	{
		var source = GetSource("GadgetsControllerBase.cs");
		Assert.Contains("/// Lists the gadgets", source);
		Assert.Contains("/// Returns every gadget, oldest first.", source);
	}

	[Fact]
	public void Document_parameters_with_their_description()
	{
		var source = GetSource("GadgetsControllerBase.cs");
		Assert.Contains("""<param name="color">Only gadgets of this color</param>""", source);
	}

	[Fact]
	public void Document_responses_with_their_description()
	{
		var source = GetSource("GadgetsControllerBase.cs");
		Assert.Contains("/// The gadgets", source);
	}

	[Theory]
	[InlineData(false, "DefaultControllerBase.cs")]
	[InlineData(true, "IDefaultApi.cs")]
	public void Document_the_default_response_with_its_description(bool minimalApi, string fileName)
	{
		var source = DynamicCompilation.GetGeneratedSources("oas-server-responses.yml", minimalApi: minimalApi)
			.Single(e => e.Key.EndsWith("." + fileName, System.StringComparison.Ordinal))
			.Value;
		Assert.Contains("/// Unexpected error", source);
	}

	[Fact]
	public void Document_models_with_their_description()
	{
		var source = GetSource("Gadget.cs");
		Assert.Contains("/// A gadget", source);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): property descriptions are not written to the generated model")]
	public void Document_properties_with_their_description()
	{
		var source = GetSource("Gadget.cs");
		Assert.Contains("The name of the gadget", source);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): externalDocs is not written to the generated controller")]
	public void Document_operations_with_their_external_docs()
	{
		var source = GetSource("GadgetsControllerBase.cs");
		Assert.Contains("https://example.com/docs/gadgets", source);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): operation tags are not written to the generated controller")]
	public void Tag_operations_with_their_tags()
	{
		var source = GetSource("GadgetsControllerBase.cs");
		Assert.Contains("Tags(\"gadgets\")", source);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): examples are not written to the generated code")]
	public void Document_properties_with_their_examples()
	{
		var source = GetSource("Gadget.cs");
		Assert.Contains("<example>sprocket</example>", source);
	}
}
