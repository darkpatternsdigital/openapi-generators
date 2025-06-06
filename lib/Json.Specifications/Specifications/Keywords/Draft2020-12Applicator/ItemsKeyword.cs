
using System.Collections.Generic;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-core#section-10.3.1.2">Draft 2020-12 items keyword</see>
public class ItemsKeyword(string keyword, JsonSchema schema) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		var schemaResult = JsonSchemaParser.Deserialize(nodeInfo, options);
		return schemaResult.Select<IJsonSchemaAnnotation>(schema => new ItemsKeyword(keyword, schema));
	}

	public string Keyword => keyword;

	public JsonSchema Schema => schema;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [Schema];
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		// TODO - leverage prefixItems and contains
		if (nodeMetadata.Node is not JsonArray array)
			yield break;

		for (var i = 0; i < array.Count; i++)
		{
			foreach (var entry in Schema.Evaluate(nodeMetadata.Navigate(i), evaluationContext))
				yield return entry;
		}
	}
}
