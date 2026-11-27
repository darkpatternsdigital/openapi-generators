using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Loaders;
using DarkPatterns.Json.Specifications.Keywords;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;

namespace DarkPatterns.OpenApi.Specifications.v3_0;

public class OpenApi3_0Parser : SchemaValidatingParser<OpenApiDocument>
{
	private static readonly Uri schemaUri = new Uri("https://spec.openapis.org/oas/3.0/schema/2021-09-28");

	/// <summary>
	/// The OpenAPI 3.0 schema is written in JSON Schema draft-04; it uses the
	/// OpenAPI 3.0 Schema Object keywords plus `patternProperties`.
	/// </summary>
	private static readonly IJsonSchemaDialect schemaDialect = new JsonSchemaDialect(
		new Uri("http://json-schema.org/draft-04/schema#"),
		null,
		[
			OpenApi3_0DocumentFactory.Vocabulary,
			new JsonSchemaVocabulary(
				new Uri("http://json-schema.org/draft-04/schema#patternProperties"),
				[("patternProperties", PatternPropertiesKeyword.Instance)]
			),
		],
		UnknownKeyword.Instance
	);

	private static readonly Lazy<JsonSchema> openApi3_0Schema = new(LoadOpenApi3_0Schema);

	public OpenApi3_0Parser() : base(_ => openApi3_0Schema.Value)
	{
	}

	private static JsonSchema LoadOpenApi3_0Schema()
	{
		// The schema gets its own registry so its nodes are not treated as part of the documents being parsed
		var schemaRegistry = new SchemaRegistry(new DocumentRegistry(new DocumentRegistryOptions([], [])));
		using var schemaStream = typeof(OpenApi3_0Parser).Assembly.GetManifestResourceStream($"{typeof(OpenApi3_0Parser).Namespace}.Schemas.schema.yaml");
		using var sr = new StreamReader(schemaStream);
		var yamlDocument = new YamlDocumentLoader().LoadDocument(schemaUri, sr, schemaDialect);
		schemaRegistry.DocumentRegistry.AddDocument(yamlDocument);
		var metadata = ResolvableNode.FromRoot(schemaRegistry.DocumentRegistry, yamlDocument);

		var result = JsonSchemaParser.Deserialize(metadata, new JsonSchemaParserOptions(schemaRegistry, schemaDialect));
		if (result is not DiagnosableResult<JsonSchema>.Success { Value: var schema })
			throw new InvalidOperationException(Errors.FailedToParseEmbeddedSchema);
		// Resolve the schema's `$ref`s; otherwise, only the root of the document is validated
		schemaRegistry.DocumentRegistry.Register(schema);
		if (schemaRegistry.RecursivelyFixupAll().Any())
			throw new InvalidOperationException(Errors.FailedToParseEmbeddedSchema);
		return schema;
	}

	public override bool CanParse(IDocumentReference documentReference)
	{
		if (documentReference.RootNode is not JsonObject jObject) return false;
		if (!jObject.TryGetPropertyValue("openapi", out var versionNode)) return false;
		if (versionNode is not JsonValue jValue) return false;
		if (!jValue.TryGetValue<string>(out var version)) return false;
		if (!version.StartsWith("3.0.")) return false;
		return true;
	}

	protected override ParseResult<OpenApiDocument> Construct(IDocumentReference documentReference, IEnumerable<DiagnosticBase> diagnostics, SchemaRegistry schemaRegistry)
	{
		var factory = new OpenApi3_0DocumentFactory(schemaRegistry, diagnostics);
		var result = factory.ConstructDocument(documentReference);
		return new ParseResult<OpenApiDocument>(
			result,
			factory.Diagnostics.ToArray()
		);
	}
}
