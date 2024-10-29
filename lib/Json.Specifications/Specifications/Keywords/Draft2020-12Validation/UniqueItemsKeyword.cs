using Json.More;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-validation#name-uniqueitems">Draft 2020-12 uniqueItems keyword</see>
public class UniqueItemsKeyword(string keyword, bool mustBeUnique) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is JsonValue val && val.TryGetValue<bool>(out var value))
			return DiagnosableResult<IJsonSchemaAnnotation>.Pass(new UniqueItemsKeyword(keyword, value));
		return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));
	}

	public string Keyword => keyword;
	public bool MustBeUnique => mustBeUnique;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [];
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		if (nodeMetadata.Node is not JsonArray array) yield break;

		var set = new HashSet<JsonNode?>();
		foreach (var (node, index) in array.Select((node, i) => (node, i)))
		{
			// TODO: .NET 9 has JsonNode.DeepEquals
			if (set.Any(prev => prev.IsEquivalentTo(node)))
			{
				yield return new UniqueItemsKeywordNotUnique(evaluationContext.DocumentRegistry.ResolveLocation(nodeMetadata.Navigate(index)));
				continue;
			}
			set.Add(node);
		}
	}
}

public record UniqueItemsKeywordNotUnique(Location Location) : DiagnosticBase(Location)
{
	public static DiagnosticException.ToDiagnostic Builder() => (Location) => new UniqueItemsKeywordNotUnique(Location);
}
