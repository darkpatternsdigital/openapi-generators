
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords;

namespace PrincipleStudios.OpenApi.Transformations.Specifications.OpenApi3_0;

public class RefKeyword(string keyword, Uri reference, Uri absoluteReference) : IJsonSchemaFixupAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is JsonValue val && val.TryGetValue<string>(out var s) && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var reference))
			return DiagnosableResult<IJsonSchemaAnnotation>.Pass(new RefKeyword(keyword, reference, new Uri(nodeInfo.Id, reference)));
		return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));
	}

	public string Keyword => keyword;

	public Uri AbsoluteReference => absoluteReference;
	public Uri Reference => reference;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchema context, EvaluationContext evaluationContext)
	{
		yield break;
	}

	public void FixupInPlace(JsonSchema schema, IJsonSchemaModifier modifier, JsonSchemaParserOptions options)
	{
		var result = options.Registry.ResolveSchema(new(absoluteReference, schema.Metadata), options.Dialect);
		if (result is JsonSchema newResult)
		{
			if (newResult.BoolValue is bool v)
				modifier.SetBooleanSchemaValue(v);
			else
				modifier.ReplaceAnnotations(newResult.Annotations, !newResult.IsFixupComplete);
			modifier.UpdateId(newResult.Metadata);
		}
	}
}
