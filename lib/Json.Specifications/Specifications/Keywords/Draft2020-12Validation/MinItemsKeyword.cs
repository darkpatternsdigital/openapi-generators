
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-validation#name-minitems">Draft 2020-12 minItems keyword</see>
public class MinItemsKeyword(string keyword, int value) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is JsonValue val && val.TryGetValue<decimal>(out var value) && value % 1 == 0 && value >= 0)
			return DiagnosableResult<IJsonSchemaAnnotation>.Pass(new MinItemsKeyword(keyword, (int)value));
		return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));
	}

	public string Keyword => keyword;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [];
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		// TODO
		throw new System.NotImplementedException($"Not implemented: limit min items to {value}");
	}
}
