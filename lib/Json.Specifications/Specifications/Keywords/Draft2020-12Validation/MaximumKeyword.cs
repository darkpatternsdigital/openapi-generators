
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;

/// <see href="https://json-schema.org/draft/2020-12/json-schema-validation#name-maximum">Draft 2020-12 maximum keyword</see>
public class MaximumKeyword(string keyword, decimal value) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is JsonValue val && val.TryGetValue<decimal>(out var value))
			return DiagnosableResult<IJsonSchemaAnnotation>.Pass(new MaximumKeyword(keyword, value));
		return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));
	}

	public string Keyword => keyword;
	public decimal Value => value;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [];
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		// TODO
		throw new System.NotImplementedException();
	}
}
