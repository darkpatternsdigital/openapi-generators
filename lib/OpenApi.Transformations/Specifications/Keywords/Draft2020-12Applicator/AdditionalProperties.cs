
using System.Collections.Generic;
using System.Text.Json.Nodes;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;

namespace PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Applicator;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-core#name-additionalproperties">Draft 2020-12 additionalProperties keyword</see>
public class AdditionalPropertiesKeyword(string keyword, JsonSchema schema) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		var schemaResult = JsonSchemaParser.Deserialize(nodeInfo, options);
		return schemaResult.Select<IJsonSchemaAnnotation>(schema => new AdditionalPropertiesKeyword(keyword, schema));
	}

	public string Keyword => keyword;

	public JsonSchema Schema => schema;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [schema];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchema context, EvaluationContext evaluationContext)
	{
		if (nodeMetadata.Node is not JsonObject obj)
			yield break;

		// TODO - leverage patternProperties (but do not apply to OpenAPI 3.0, which means forking this keyword)
		// TODO - compare given schema against all properties that do not satisfy `properties`
	}
}
