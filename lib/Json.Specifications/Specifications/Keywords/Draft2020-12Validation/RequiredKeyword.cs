using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-validation#name-required">Draft 2020-12 required keyword</see>
public class RequiredKeyword(string keyword, IReadOnlyList<string> requiredProperties) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	public string Keyword => keyword;
	public IReadOnlyList<string> RequiredProperties => requiredProperties;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [];
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is not JsonArray array) return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));

		var requiredProperties = array.Select(entry => entry is JsonValue v && v.TryGetValue<string>(out var s) ? s : null).ToArray();

		var nullProps = (from e in requiredProperties.Select((p, i) => (prop: p, i))
						 where e.prop == null
						 select new UnableToParseKeyword("required", options.Registry.ResolveLocation(nodeInfo.Navigate(e.i)))).ToArray();
		if (nullProps.Length > 0)
			return DiagnosableResult<IJsonSchemaAnnotation>.Fail(nullProps);

		return DiagnosableResult<IJsonSchemaAnnotation>.Pass(new RequiredKeyword(
			keyword,
			requiredProperties!
		));
	}

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		if (nodeMetadata.Node is not JsonObject obj) yield break;

		var missing = RequiredProperties.Except(obj.Select(x => x.Key)).ToArray();
		if (missing.Length > 0)
			yield return new MissingRequiredProperties(missing, evaluationContext.DocumentRegistry.ResolveLocation(nodeMetadata));
	}
}

public record MissingRequiredProperties(IReadOnlyList<string> Missing, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [string.Join(", ", Missing)];
	public static DiagnosticException.ToDiagnostic Builder(IReadOnlyList<string> Missing) => (Location) => new MissingRequiredProperties(Missing, Location);
}
