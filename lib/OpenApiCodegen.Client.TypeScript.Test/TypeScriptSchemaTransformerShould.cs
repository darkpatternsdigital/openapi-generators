using Xunit;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.TypeScript;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using System;
using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using DarkPatterns.OpenApiCodegen.TestUtils;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApi.Transformations.Specifications;
using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;
using static OptionsHelpers;

public class TypeScriptSchemaTransformerShould
{
	[Theory]
	[InlineData(false, "proj://embedded/petstore.yaml#/paths/~1pets/get/parameters/0/schema")]
	[InlineData(false, "proj://embedded/petstore.yaml#/paths/~1pets/get/parameters/1/schema")]
	[InlineData(false, "proj://embedded/petstore.yaml#/paths/~1pets/get/responses/200/content/application~1json/schema")]
	[InlineData(true, "proj://embedded/petstore.yaml#/paths/~1pets/get/responses/200/content/application~1json/schema/items")]
	[InlineData(true, "proj://embedded/petstore.yaml#/paths/~1pets/get/responses/default/content/application~1json/schema")]
	[InlineData(true, "proj://embedded/petstore.yaml#/paths/~1pets/post/requestBody/content/application~1json/schema")]
	[InlineData(true, "proj://embedded/petstore.yaml#/paths/~1pets/post/responses/200/content/application~1json/schema")]
	[InlineData(false, "proj://embedded/petstore.yaml#/paths/~1pets~1%7Bid%7D/get/parameters/0/schema")]
	[InlineData(false, "proj://embedded/petstore.yaml#/paths/~1pets~1%7Bid%7D/delete/parameters/0/schema")]
	[InlineData(true, "proj://embedded/petstore.yaml#/components/schemas/Pet")]
	[InlineData(true, "proj://embedded/petstore.yaml#/components/schemas/NewPet")]
	[InlineData(true, "proj://embedded/petstore.yaml#/components/schemas/Error")]
	[InlineData(true, "proj://embedded/no-refs.yaml#/paths/~1address/post/requestBody/content/application~1json/schema")]
	[InlineData(true, "proj://embedded/no-refs.yaml#/paths/~1address/post/requestBody/content/application~1json/schema/properties/location")]
	[InlineData(true, "proj://embedded/enum.yaml#/paths/~1rock-paper-scissors/post/responses/200/content/application~1json/schema")]
	[InlineData(true, "proj://embedded/array.yaml#/components/schemas/Colors")]
	public void Know_when_to_generate_source(bool expectedInline, string schemaUriString)
	{
		var schemaUri = new Uri(schemaUriString);
		var docRef = GetDocumentByUri(schemaUri);

		var (registry, document, schema) = GetSchema(docRef, schemaUri);
		Assert.NotNull(document);
		Assert.NotNull(schema);

		var target = ConstructTarget(LoadOptions(), registry);
		var actual = target.ProduceSourceEntry(schema!);

		Assert.Equal(expectedInline, actual);
	}

	private static (DocumentRegistry registry, OpenApiDocument? document, JsonSchema? schema) GetSchema(IDocumentReference docRef, Uri uri)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = CommonParsers.DefaultParsers.Parse(docRef, registry);
		Assert.NotNull(docResult.Document);
		var document = docResult.Document;

		var metadata = new ResolvableNode(new NodeMetadata(uri), registry);
		while (metadata.Node is JsonObject obj && obj.TryGetPropertyValue("$ref", out var refNode) && refNode?.GetValue<string>() is string refValue)
		{
			metadata = new ResolvableNode(new NodeMetadata(new Uri(metadata.Id, refValue), metadata.Metadata), registry);
		}
		var schemaResult = registry.ResolveSchema(metadata.Metadata, document.Dialect);
		return (registry, document, schemaResult);
	}

	[Theory]
	[InlineData("Array<string>", "proj://embedded/petstore.yaml#/paths/~1pets/get/parameters/0/schema")]
	[InlineData("number", "proj://embedded/petstore.yaml#/paths/~1pets/get/parameters/1/schema")]
	[InlineData("Array<Pet>", "proj://embedded/petstore.yaml#/paths/~1pets/get/responses/200/content/application~1json/schema")]
	[InlineData("Pet", "proj://embedded/petstore.yaml#/paths/~1pets/get/responses/200/content/application~1json/schema/items")]
	[InlineData("_Error", "proj://embedded/petstore.yaml#/paths/~1pets/get/responses/default/content/application~1json/schema")]
	[InlineData("NewPet", "proj://embedded/petstore.yaml#/paths/~1pets/post/requestBody/content/application~1json/schema")]
	[InlineData("Pet", "proj://embedded/petstore.yaml#/paths/~1pets/post/responses/200/content/application~1json/schema")]
	[InlineData("number", "proj://embedded/petstore.yaml#/paths/~1pets~1%7Bid%7D/get/parameters/0/schema")]
	[InlineData("number", "proj://embedded/petstore.yaml#/paths/~1pets~1%7Bid%7D/delete/parameters/0/schema")]
	[InlineData("Pet", "proj://embedded/petstore.yaml#/components/schemas/Pet")]
	[InlineData("NewPet", "proj://embedded/petstore.yaml#/components/schemas/NewPet")]
	[InlineData("_Error", "proj://embedded/petstore.yaml#/components/schemas/Error")]
	[InlineData("LookupRecordRequest", "proj://embedded/no-refs.yaml#/paths/~1address/post/requestBody/content/application~1json/schema")]
	[InlineData("LookupRecordRequestLocation", "proj://embedded/no-refs.yaml#/paths/~1address/post/requestBody/content/application~1json/schema/properties/location")]
	[InlineData("PlayRockPaperScissorsResponse", "proj://embedded/enum.yaml#/paths/~1rock-paper-scissors/post/responses/200/content/application~1json/schema")]
	[InlineData("Colors", "proj://embedded/array.yaml#/components/schemas/Colors")]
	public void ConvertToInlineTypes(string expectedInline, string schemaUriString)
	{
		var schemaUri = new Uri(schemaUriString);
		var docRef = GetDocumentByUri(schemaUri);

		var (registry, document, schema) = GetSchema(docRef, schemaUri);
		Assert.NotNull(document);
		Assert.NotNull(schema);

		var target = ConstructTarget(LoadOptions(), registry);
		var inline = target.ToInlineDataType(schema);

		Assert.Equal(expectedInline, inline.Text);
	}

	private TypeScriptInlineSchemas ConstructTarget(TypeScriptSchemaOptions options, DocumentRegistry registry)
	{
		return new(options, registry);
	}
}
