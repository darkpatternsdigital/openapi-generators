using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Specifications.OpenApi3_1;

internal class OpenApi3_1Parser : SchemaValidatingParser<OpenApiDocument>
{
	// TODO
	/// <see cref="Json.Schema.OpenApi.MetaSchemas.DocumentSchema"/>
	public static readonly JsonSchema DocumentSchema = new JsonSchema(new NodeMetadata(new Uri("https://spec.openapis.org/oas/3.1/schema/2022-02-27")), []);

	public OpenApi3_1Parser() : base((_) => DocumentSchema)
	{
	}

	public override bool CanParse(IDocumentReference documentReference)
	{
		if (documentReference.RootNode is not JsonObject jObject) return false;
		if (!jObject.TryGetPropertyValue("openapi", out var versionNode)) return false;
		if (versionNode is not JsonValue jValue) return false;
		if (!jValue.TryGetValue<string>(out var version)) return false;
		if (!version.StartsWith("3.1.")) return false;
		return true;
	}

	protected override ParseResult<OpenApiDocument> Construct(IDocumentReference documentReference, IEnumerable<DiagnosticBase> diagnostics, SchemaRegistry schemaRegistry)
	{
		var factory = new OpenApi3_1DocumentFactory(schemaRegistry, diagnostics);
		var result = factory.ConstructDocument(documentReference.BaseUri, documentReference.RootNode ?? throw new InvalidOperationException(Errors.InvalidOpenApiRootNode));
		return new ParseResult<OpenApiDocument>(
			result,
			factory.Diagnostics.ToArray()
		);
	}
}
