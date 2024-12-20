
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;

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
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		if (nodeMetadata.Node is not JsonObject obj)
			yield break;

		// TODO - leverage patternProperties (but do not apply to OpenAPI 3.0, which means forking this keyword)
		// compare given schema against all properties that do not satisfy `properties`
		if (context.TryGetAnnotation<PropertiesKeyword>() is { Properties: var properties })
		{
			foreach (var diagnostic in from kvp in obj.AsEnumerable()
									   where !properties.ContainsKey(kvp.Key)
									   from diag in Schema.Evaluate(nodeMetadata.Navigate(kvp.Key), evaluationContext.WithSchema(context.EffectiveSchema))
									   select diag)
			{
				yield return diagnostic;
			}
		}
	}
}
