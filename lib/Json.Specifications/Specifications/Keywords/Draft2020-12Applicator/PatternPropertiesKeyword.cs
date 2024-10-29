
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-core#name-patternproperties">Draft 2020-12 patternProperties keyword</see>
public class PatternPropertiesKeyword(string keyword, IReadOnlyDictionary<Regex, JsonSchema> patternSchemas) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	public string Keyword => keyword;

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is not JsonObject obj)
			return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));

		return (from kvp in obj
				let keyRegex = new Regex(kvp.Key)
				let parsed = JsonSchemaParser.Deserialize(nodeInfo.Navigate(kvp.Key), options)
				let entry = parsed.Select(schema => (keyRegex, schema))
				select entry).AggregateAll()
			.Select(tuples => (IJsonSchemaAnnotation)new PatternPropertiesKeyword(keyword, tuples.ToDictionary(t => t.keyRegex, t => t.schema)));
	}

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		if (nodeMetadata.Node is not JsonObject obj) yield break;

		foreach (var (propertyName, _) in obj)
			foreach (var (pattern, schema) in patternSchemas)
				foreach (var diag in pattern.Match(propertyName) switch
				{
					{ Success: true } => schema.Evaluate(nodeMetadata.Navigate(propertyName), evaluationContext.WithSchema(context.EffectiveSchema)),
					_ => []
				})
					yield return diag;
	}

	public IEnumerable<JsonSchema> GetReferencedSchemas()
	{
		return patternSchemas.Values;
	}
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];
}
