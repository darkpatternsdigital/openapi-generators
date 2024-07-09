using Bogus;
using Json.Pointer;
using PrincipleStudios.OpenApi.CSharp;
using PrincipleStudios.OpenApi.Transformations;
using PrincipleStudios.OpenApi.Transformations.Abstractions;
using PrincipleStudios.OpenApi.Transformations.DocumentTypes;
using PrincipleStudios.OpenApi.Transformations.Specifications;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Applicator;
using PrincipleStudios.OpenApiCodegen.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;
using static PrincipleStudios.OpenApiCodegen.TestUtils.DocumentHelpers;

namespace PrincipleStudios.OpenApiCodegen.Server.Mvc;
using static OptionsHelpers;

public class CSharpInlineSchemasShould
{
	private CSharpInlineSchemas CreateTarget(CSharpServerSchemaOptions options, DocumentRegistry registry) => new(options, registry);

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
		var options = LoadOptions();
		var (registry, document, schema) = GetSchema(docRef, path);
		var target = CreateTarget(options, registry);
		Assert.NotNull(document);
		Assert.NotNull(schema);

		var actual = target.ProduceSourceEntry(schema!);

		Assert.Equal(expectedInline, actual);
	}

	[Theory]
	[InlineData("FindPetsByStatusStatusItem", "petstore3.json", "/paths/~1pet~1findByStatus/get/parameters/0/schema/items")]
	[InlineData("DifficultQueryStringEnumEnum", "enum.yaml", "/paths/~1difficult-enum/get/parameters/0/schema")]
	public void Determine_a_name_for_schema_by_path(string expectedName, string documentName, string path)
	{
		var docResult = GetDocumentReference(documentName);
		Assert.NotNull(docResult);
		var (registry, document, schema) = GetSchema(docResult, path);
		var target = CreateTarget(LoadOptions(), registry);

		Assert.NotNull(schema);

		var actual = target.ToInlineDataType(schema!);

		Assert.Equal(expectedName, actual?.Text);
	}

	private static (DocumentRegistry registry, OpenApiDocument? document, JsonSchema? schema) GetSchema(IDocumentReference docRef, string path)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = CommonParsers.DefaultParsers.Parse(docRef, registry);
		Assert.NotNull(docResult.Document);
		var document = docResult.Document;

		var metadata = new ResolvableNode(new NodeMetadata(new Uri(document.Id, "#" + path)), registry);
		while (metadata.Node is JsonObject obj && obj.TryGetPropertyValue("$ref", out var refNode) && refNode?.GetValue<string>() is string refValue)
		{
			metadata = new ResolvableNode(new NodeMetadata(new Uri(metadata.Id, refValue), metadata.Metadata), registry);
		}
		var schemaResult = JsonSchemaParser.Deserialize(metadata, new JsonSchemaParserOptions(registry, OpenApi.Transformations.Specifications.OpenApi3_0.OpenApi3_0DocumentFactory.OpenApiDialect));
		return (registry, document, schemaResult.Fold<JsonSchema?>(v => v, _ => null));
	}
}
