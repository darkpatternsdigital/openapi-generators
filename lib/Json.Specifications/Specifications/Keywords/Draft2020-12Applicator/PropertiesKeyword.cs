
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-core#name-properties">Draft 2020-12 properties keyword</see>
public class PropertiesKeyword(string keyword, IReadOnlyDictionary<string, JsonSchema> properties) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is not JsonObject obj)
			return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));

		return DiagnosableResult<IJsonSchemaAnnotation>.Pass(new UnresolvedPropertiesAnnotation(
			keyword,
			obj.ToDictionary(kvp => kvp.Key, kvp => nodeInfo.Navigate(kvp.Key))
		));
	}

	public string Keyword => keyword;

	public IReadOnlyDictionary<string, JsonSchema> Properties => properties;
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<JsonSchema> GetReferencedSchemas() => Properties.Values;

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		if (nodeMetadata.Node is not JsonObject obj) yield break;

		foreach (var kvp in obj)
		{
			if (!properties.TryGetValue(kvp.Key, out var valueSchema))
				// Ignore properties not defined
				continue;
			foreach (var entry in valueSchema.Evaluate(nodeMetadata.Navigate(kvp.Key), evaluationContext.WithSchema(context.EffectiveSchema)))
				yield return entry;
		}
	}

	class UnresolvedPropertiesAnnotation(string keyword, IReadOnlyDictionary<string, ResolvableNode> properties) : IJsonSchemaFixupAnnotation
	{
		public string Keyword => keyword;
		public IEnumerable<JsonSchema> GetReferencedSchemas() => [];

		public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext) => [];
		public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
			=> [];

		public void FixupInPlace(JsonSchema schema, IJsonSchemaModifier modifier, JsonSchemaParserOptions options)
		{
			var results = properties.ToDictionary(
					(kvp) => kvp.Key,
					(kvp) => JsonSchemaParser.Deserialize(kvp.Value, options)
				);
			var failures = results.Values.OfType<DiagnosableResult<JsonSchema>.Failure>().ToArray();
			if (failures.Length > 0)
				modifier.AddDiagnostics(failures.SelectMany(r => r.Diagnostics));

			modifier.ReplaceAnnotations(
				schema.Annotations.Except([this]).Concat(
					[new PropertiesKeyword(
						keyword,
						results.Where((kvp) => kvp.Value is DiagnosableResult<JsonSchema>.Success).ToDictionary(
							(kvp) => kvp.Key,
							(kvp) => ((DiagnosableResult<JsonSchema>.Success)kvp.Value).Value
						)
					)]
				),
				needsFixup: true);
		}

	}
}
