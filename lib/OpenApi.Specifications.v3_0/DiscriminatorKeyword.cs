
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords;

namespace DarkPatterns.OpenApi.Specifications.v3_0;

/// <see href="https://spec.openapis.org/oas/v3.0.3#composition-and-inheritance-polymorphism">OpenAPI 3.0 discriminator</see>
public class DiscriminatorKeyword(string keyword, string propertyName, IReadOnlyDictionary<string, Uri>? mapping) : IJsonSchemaAnnotation
{
	private const string propertyNameField = "propertyName";
	private const string mappingField = "mapping";
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);
	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is not JsonObject obj || !obj.TryGetPropertyValue(propertyNameField, out var propertyNameNode))
			return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));

		if (propertyNameNode is not JsonValue val || !val.TryGetValue<string>(out var propertyName))
			return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo.Navigate(propertyNameField))));

		Dictionary<string, Uri>? mapping = null;
		if (obj.TryGetPropertyValue(mappingField, out var mappingNode))
		{
			if (mappingNode is not JsonObject mappingObj)
				return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo.Navigate(mappingField))));
			if (mappingObj.Any(kvp => kvp.Value is not JsonValue v || !v.TryGetValue<string>(out var _)))
				return DiagnosableResult<IJsonSchemaAnnotation>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo.Navigate(mappingField))));
			mapping = mappingObj.ToDictionary(kvp => kvp.Key, kvp => new Uri(kvp.Value!.AsValue().GetValue<string>(), UriKind.RelativeOrAbsolute));
		}
		return DiagnosableResult<IJsonSchemaAnnotation>.Pass(new DiscriminatorKeyword(keyword, propertyName, mapping));
	}

	public string Keyword => keyword;
	public string PropertyName => propertyName;
	public IReadOnlyDictionary<string, Uri>? Mapping => mapping;

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [];
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		// TODO
		yield break;
	}
}
