using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApiCodegen.TestUtils;
using System;
using System.IO;
using System.Text.Json.Nodes;
using Xunit;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp;
using static OptionsHelpers;

public class CSharpSchemaTransformerShould
{
	[Theory]
	[InlineData(false, "petstore.yaml", "/paths/~1pets/get/parameters/0/schema")]
	[InlineData(false, "petstore.yaml", "/paths/~1pets/get/parameters/1/schema")]
	[InlineData(false, "petstore.yaml", "/paths/~1pets/get/responses/200/content/application~1json/schema")]
	[InlineData(true, "petstore.yaml", "/paths/~1pets/get/responses/default/content/application~1json/schema")]
	[InlineData(true, "petstore.yaml", "/paths/~1pets/post/requestBody/content/application~1json/schema")]
	[InlineData(true, "petstore.yaml", "/paths/~1pets/post/responses/200/content/application~1json/schema")]
	[InlineData(false, "petstore.yaml", "/paths/~1pets~1{id}/get/parameters/0/schema")]
	[InlineData(false, "petstore.yaml", "/paths/~1pets~1{id}/delete/parameters/0/schema")]
	[InlineData(true, "petstore.yaml", "/components/schemas/Pet")]
	[InlineData(true, "petstore.yaml", "/components/schemas/NewPet")]
	[InlineData(true, "petstore.yaml", "/components/schemas/Error")]
	[InlineData(true, "no-refs.yaml", "/paths/~1address/post/requestBody/content/application~1json/schema")]
	[InlineData(true, "no-refs.yaml", "/paths/~1address/post/requestBody/content/application~1json/schema/properties/location")]
	public void DetermineWhenToGenerateSource(bool expectedInline, string documentName, string path)
	{
		var docRef = GetDocumentReference(documentName);

		var (registry, document, schema) = GetSchema(docRef, path);
		Assert.NotNull(document);
		Assert.NotNull(schema);

		var target = ConstructTarget(LoadOptions(), registry.DocumentRegistry);

		var actual = target.ProduceSourceEntry(schema!);

		Assert.Equal(expectedInline, actual);
	}

	private static (SchemaRegistry registry, OpenApiDocument? document, JsonSchema? schema) GetSchema(IDocumentReference docRef, string path)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = CommonParsers.DefaultParsers.Parse(docRef, registry);
		Assert.NotNull(docResult.Result);
		var document = docResult.Result;

		var metadata = new ResolvableNode(new NodeMetadata(new Uri(document.Id, "#" + path)), registry.DocumentRegistry);
		while (metadata.Node is JsonObject obj && obj.TryGetPropertyValue("$ref", out var refNode) && refNode?.GetValue<string>() is string refValue)
		{
			metadata = new ResolvableNode(new NodeMetadata(new Uri(metadata.Id, refValue), metadata.Metadata), registry.DocumentRegistry);
		}
		var schemaResult = JsonSchemaParser.Deserialize(metadata, new JsonSchemaParserOptions(registry, OpenApi.Specifications.v3_0.OpenApi3_0DocumentFactory.OpenApiDialect));
		return (registry, document, schemaResult.Fold<JsonSchema?>(v => v, _ => null));
	}

	private CSharpInlineSchemas ConstructTarget(CSharpSchemaOptions options, DocumentRegistry documentRegistry) => new(options, documentRegistry);

}