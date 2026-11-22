using System;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// Source comments in generated code identify schema nodes relative to a base (the project
/// directory for C#, the working directory for the TypeScript CLI), keeping machine paths out.
/// </summary>
public class SourceReferenceShould
{
	private static TransformSettings CreateTarget(string? baseUri) =>
		new(DocumentLoader.CreateRegistry(), "", baseUri == null ? null : new Uri(baseUri));

	[Theory]
	[InlineData("file:///C:/repo/src/App/", "file:///C:/repo/schemas/pets.yaml#/components/schemas/Pet", "../../schemas/pets.yaml#/components/schemas/Pet")]
	[InlineData("file:///C:/repo/", "file:///C:/repo/schemas/pets.yaml#/components/schemas/Pet", "schemas/pets.yaml#/components/schemas/Pet")]
	[InlineData("file:///home/user/app/", "file:///home/user/app/api.yaml#/paths/~1pets~1{id}/get", "api.yaml#/paths/~1pets~1{id}/get")]
	[InlineData("file:///C:/repo/", "file:///C:/other%20dir/api.yaml#/info", "../other dir/api.yaml#/info")]
	// Inline schemas currently have a two-slash form of the same file URI
	[InlineData("file:///C:/repo/", "file://C:/repo/schemas/pets.yaml#/paths/~1pets/get/parameters/0/schema", "schemas/pets.yaml#/paths/~1pets/get/parameters/0/schema")]
	public void Make_file_references_relative_to_the_base(string baseUri, string id, string expected)
	{
		Assert.Equal(expected, CreateTarget(baseUri).ToSourceReference(new Uri(id)));
	}

	[Theory]
	[InlineData("file:///C:/repo/", "proj://darkpatterns-openapi/api.yaml#/components/schemas/Pet")]
	[InlineData(null, "file:///C:/repo/schemas/pets.yaml#/components/schemas/Pet")]
	public void Keep_other_references_as_written(string? baseUri, string id)
	{
		Assert.Equal(id, CreateTarget(baseUri).ToSourceReference(new Uri(id)));
	}
}
