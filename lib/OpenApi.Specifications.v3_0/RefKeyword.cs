
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords;

namespace DarkPatterns.OpenApi.Specifications.v3_0;

using static DiagnosableResult<IJsonSchemaAnnotation>;

public class RefKeyword(string keyword, Uri reference, Uri absoluteReference) : IJsonSchemaFixupAnnotation, IJsonSchemaRefAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is JsonValue val && val.TryGetValue<string>(out var s) && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var reference))
		{
			var absoluteReference = new Uri(nodeInfo.Id, reference);
			var n = options.Registry.ResolveMetadataNode(absoluteReference, nodeInfo.Metadata);
			var referencedSchemaResult = JsonSchemaParser.Deserialize(n, options);
			return referencedSchemaResult.Select(
				referencedSchema => (IJsonSchemaAnnotation)new RefKeyword(keyword, reference, absoluteReference)
			);
		}
		return Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));
	}

	public string Keyword => keyword;

	public JsonSchema? ReferencedSchema { get; private set; }
	public Uri Reference => reference;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => ReferencedSchema == null ? [] : [ReferencedSchema];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		if (ReferencedSchema != null)
			foreach (var diag in ReferencedSchema.Evaluate(nodeMetadata, evaluationContext.WithSchema(context.EffectiveSchema)))
				yield return diag;
		yield break;
	}

	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations() =>
		// The OpenAPI 3.0 ref only allows for a full replacement... otherwise it merges things.
		[];

	public void FixupInPlace(JsonSchema schema, IJsonSchemaModifier modifier, JsonSchemaParserOptions options)
	{
		var result = options.SchemaRegistry.ResolveSchema(new(absoluteReference, schema.Metadata), options.Dialect);
		if (result is JsonSchema newResult)
		{
			ReferencedSchema = newResult;
		}
	}
}
