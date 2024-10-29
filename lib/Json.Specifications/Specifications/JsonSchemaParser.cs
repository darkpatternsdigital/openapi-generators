using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications;

public static class JsonSchemaParser
{
	public static DiagnosableResult<JsonSchema> Deserialize(ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (options.SchemaRegistry.FindSchema(nodeInfo.Id) is JsonSchema schema)
			return DiagnosableResult<JsonSchema>.Pass(schema);

		switch (nodeInfo.Node)
		{
			case JsonObject obj:
				return DeserializeKeywords(obj);
			case JsonValue v when v.TryGetValue<bool>(out var boolValue):
				var boolResult = new JsonSchema(nodeInfo.Metadata, boolValue);
				options.SchemaRegistry.EnsureSchemaRegistered(boolResult);
				return DiagnosableResult<JsonSchema>.Pass(boolResult);
			default:
				return DiagnosableResult<JsonSchema>.Fail(nodeInfo.Metadata, options.Registry, UnableToParseSchema.Builder());
		}

		DiagnosableResult<JsonSchema> DeserializeKeywords(JsonObject obj)
		{
			var baseResult = new JsonSchema(nodeInfo.Metadata, []);

			var keywords =
				from kvp in obj
				select DeserializeKeyword(kvp.Key, nodeInfo.Navigate(kvp.Key));
			var diagnostics = keywords.OfType<DiagnosableResult<IJsonSchemaAnnotation>.Failure>().SelectMany(k => k.Diagnostics).ToArray();
			if (diagnostics.Length > 0) return DiagnosableResult<JsonSchema>.Fail(diagnostics);

			return DiagnosableResult<JsonSchema>.Pass(new JsonSchema(
				nodeInfo.Metadata,
				keywords.OfType<DiagnosableResult<IJsonSchemaAnnotation>.Success>().Select(k => k.Value)
			));
		}

		DiagnosableResult<IJsonSchemaAnnotation> DeserializeKeyword(string keyword, ResolvableNode nodeInfo)
		{
			foreach (var vocabulary in options.Dialect.Vocabularies)
			{
				if (vocabulary.Keywords.TryGetValue(keyword, out var def))
					return def.ParseAnnotation(keyword, nodeInfo, options);
			}
			return options.Dialect.UnknownKeyword.ParseAnnotation(keyword, nodeInfo, options);
		}
	}
}

public record JsonSchemaParserOptions(
	ISchemaRegistry SchemaRegistry,
	IJsonSchemaDialect Dialect
)
{
	public DocumentRegistry Registry => SchemaRegistry.DocumentRegistry;
}
