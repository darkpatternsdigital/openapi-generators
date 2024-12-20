﻿using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc;
using static OptionsHelpers;

public class CSharpSchemaTransformerShould
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
	[InlineData("global::DPD.Controller.FindPetsByStatusStatusItem", "petstore3.json", "/paths/~1pet~1findByStatus/get/parameters/0/schema/items")]
	[InlineData("global::DPD.Controller.DifficultQueryStringEnumEnum", "enum.yaml", "/paths/~1difficult-enum/get/parameters/0/schema")]
	[InlineData("global::DPD.Controller.TreeNode", "csharp-name-override.yaml", "/components/schemas/Node")]
	[InlineData("global::DPD.Controller.LookupRecordRequest", "request-ref.yaml", "/components/requestBodies/LookupRecord/content/application~1json/schema")]
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

	[Fact]
	public void Determine_a_name_for_schema_with_override()
	{
		string expectedName = "global::My.TestEnum";
		string documentName = "enum.yaml";
		string path = "/paths/~1difficult-enum/get/parameters/0/schema";

		var docResult = GetDocumentReference(documentName);
		Assert.NotNull(docResult);
		var (registry, document, schema) = GetSchema(docResult, path);
		var opt = LoadOptions(configurationBuilder =>
		{
			configurationBuilder.AddYamlStream(new MemoryStream(Encoding.UTF8.GetBytes($@"
overrideNames:
  {schema!.Metadata.Id.OriginalString}: My.TestEnum
")));
		});
		var target = CreateTarget(opt, registry);

		Assert.NotNull(schema);

		var actual = target.ToInlineDataType(schema!);

		Assert.Equal(expectedName, actual?.Text);
	}

	private static (DocumentRegistry registry, OpenApiDocument? document, JsonSchema? schema) GetSchema(IDocumentReference docRef, string path)
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
		return (registry.DocumentRegistry, document, schemaResult.Fold<JsonSchema?>(v => v, _ => null));
	}
}
