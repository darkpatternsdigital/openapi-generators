using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Json.Pointer;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.Specifications.v3_0;
using DarkPatterns.Json.Documents;
using System.IO;

namespace DarkPatterns.OpenApi.CSharp;


public record CSharpInlineDefinition(string Text, bool Nullable = false, bool IsEnumerable = false)
{
	// Assumes C#8, since it's standard in VS2019+, which is when nullable reference types were introduced
	public CSharpInlineDefinition MakeNullable() =>
		Nullable ? this : new(Text + "?", Nullable: true, IsEnumerable: IsEnumerable);
}

public class CSharpInlineSchemas(CSharpSchemaOptions options, DocumentRegistry documentRegistry)
{
	public static readonly CSharpInlineDefinition AnyObject = new("object", Nullable: true);

	[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(schema))]
	public CSharpInlineDefinition? ToInlineDataType(JsonSchema? schema)
	{
		if (schema == null) return null;

		CSharpInlineDefinition result = CSharpTypeInfo.From(schema) switch
		{
			// Dictionary
			{ Type: "object", Properties: { Count: 0 }, AdditionalProperties: JsonSchema dictionaryValueSchema } =>
				new(options.ToMapType(ToInlineDataType(dictionaryValueSchema).Text), IsEnumerable: true),
			// Array
			{ Type: "array", Items: null } =>
				new(options.ToArrayType(options.FallbackType), IsEnumerable: true),
			{ Items: JsonSchema items } =>
				new(options.ToArrayType(ToInlineDataType(items).Text), IsEnumerable: true),
			// Generates a source file, so therefore it must have a class name
			_ when ProduceSourceEntry(schema) =>
				new($"global::{options.GetNamespace(schema)}.{GetClassName(schema)}"),
			// Specifically-mapped type
			{ Type: string type, Format: var format } =>
				new(options.Find(type, format)),
			_ => throw new NotSupportedException($"Unable to create inline reference for schema {schema.Metadata.Id}"),
		};
		return schema?.TryGetAnnotation<NullableKeyword>() is { IsNullable: true }
			? result.MakeNullable()
			: result;
	}

	public bool ProduceSourceEntry(JsonSchema schema)
	{
		var nodes = documentRegistry.GetNodesTo(schema.Metadata.Id);
		if (nodes.LastOrDefault() is (["allOf", _], JsonSchema))
			return false;
		// C# can't inline things that must be referenced, and vice versa.
		// (Except with tuples, but those don't serialize/deserialize reliably yet.)
		return CSharpTypeInfo.From(schema) switch
		{
			{ Type: "object", Properties.Count: 0, AdditionalProperties: JsonSchema } => false,
			{ AllOf.Count: > 0 } => true,
			{ AnyOf.Count: > 0 } => true,
			{ OneOf.Count: > 0 } => true,
			{ Type: "string", Enum.Count: > 0 } => true,
			{ Type: "array" } or { Items: JsonSchema } => false,
			// TODO: why is this testing for "object"?
			{ Type: string type, Format: var format, Properties.Count: 0, Enum.Count: 0 } => options.Find(type, format) == "object",
			{ Type: "object", Format: null } => true,
			{ Properties.Count: > 0 } => true,
			{ Type: "string" or "number" or "integer" or "boolean" } => false,
			{ } => false,
			_ => throw new NotSupportedException("Unknown schema"),
		};
	}

	private string GetClassName(JsonSchema schema)
	{
		return options.ToClassName(schema, UriToClassIdentifier(schema.Metadata.Id));
	}

	private static readonly Regex HttpSuccessRegex = new Regex("2[0-9]{2}");
	private static bool Is2xx(int statusCode) => statusCode is >= 200 and < 300;
	public string UriToClassIdentifier(Uri uri)
	{
		IReadOnlyList<JsonDocumentNodeContext> remaining = documentRegistry.GetNodesTo(uri);
		if (remaining.Count == 0)
			return string.Join(" ", JsonPointer.Parse(uri.Fragment).Segments.Select(s => s.Value));

		IEnumerable<string> parts = Enumerable.Empty<string>();
		while (remaining.Count > 0)
		{
			(var newParts, remaining) = Simplify(remaining);
			parts = parts.Concat(newParts).ToArray();
		}

		return string.Join(" ", parts);

		(IEnumerable<string> parts, IReadOnlyList<JsonDocumentNodeContext> remaining) Simplify(IReadOnlyList<JsonDocumentNodeContext> context)
		{
			switch (context[0])
			{
				case (["paths", var path], OpenApiPath) when context.Count >= 2:
					switch (context[1])
					{
						case ([var method], OpenApiOperation { OperationId: null }):
							return ([$"{method} ${path}"], context.Skip(2).ToArray());
						case (_, OpenApiOperation { OperationId: string opId }):
							return ([opId], context.Skip(2).ToArray());
						default:
							throw new NotImplementedException();
					}
				case ([], OpenApiDocument) when context.Count >= 2 && context[1] is (["components", _, string componentName], JsonSchema):
					return ([componentName], context.Skip(2).ToArray());
				case ([], OpenApiDocument):
					return (Enumerable.Empty<string>(), context.Skip(1).ToArray());
				case (["callbacks", var callbackName, _], OpenApiPath):
					switch (context[1])
					{
						case ([var method], OpenApiOperation { OperationId: null }):
							return ([$"{method} {callbackName}"], context.Skip(2).ToArray());
						case (_, OpenApiOperation { OperationId: string opId }):
							return ([opId], context.Skip(2).ToArray());
						default:
							throw new NotImplementedException();
					}
				case (_, OpenApiPath):
					return (Enumerable.Empty<string>(), context.Skip(1).ToArray());
				case (["responses"], OpenApiResponses responses) when context.Count >= 4:
					{
						if (context[1] is not ([var statusCode], OpenApiResponse response)) throw new NotImplementedException();
						if (context[3] is not (["schema"], _)) throw new NotImplementedException();

						var responseName = statusCode switch
						{
							"default" when responses.StatusCodeResponses.Count == 0 => "",
							"default" => "other",
							_ when responses.StatusCodeResponses.Count == 1 && responses.Default == null
								=> "",
							_ when HttpSuccessRegex.IsMatch(statusCode) && responses.StatusCodeResponses.Keys.Count(Is2xx) == 1
								=> "",
							_ when int.TryParse(statusCode, out var numeric) && HttpStatusCodes.StatusCodeNames.TryGetValue(numeric, out var statusCodeName)
								=> statusCodeName,
							_ => statusCode,
						};
						var (qualifierName, typeName) = context[2] switch
						{
							(["content", _], _) when response.Content!.Count == 1 => ("", "response"),
							(["content", var mimeType], _) => (mimeType, "response"),
							(["headers", var headerNam], _) => (headerNam, "header"),
							_ => throw new NotImplementedException()
						};
						return ([responseName, qualifierName, typeName], context.Skip(4).ToArray());
					}
				case (["components", "responses", var responseName], OpenApiResponse response) when context.Count >= 3:
					{
						if (context[2] is not (["schema"], _)) throw new NotImplementedException();

						var (qualifierName, typeName) = context[1] switch
						{
							(["content", _], _) when response.Content!.Count == 1 => ("", "response"),
							(["content", var mimeType], _) => (mimeType, "response"),
							(["headers", var headerNam], _) => (headerNam, "header"),
							_ => throw new NotImplementedException()
						};
						return ([responseName, qualifierName, typeName], context.Skip(3).ToArray());
					}
				case (["components", "requestBodies", var requestName], OpenApiRequestBody requestBody) when context.Count >= 3:
					{
						if (context[1] is not (["content", var mimeType], _)) throw new NotImplementedException();
						if (context[2] is not (["schema"], _)) throw new NotImplementedException();

						return ([requestName, requestBody.Content!.Count == 1 ? "" : mimeType, "request"], context.Skip(3).ToArray());
					}
				case (["components", "parameters", var paramName], OpenApiParameter) when context.Count >= 1:
					if (context[1] is not (["schema"], _)) throw new NotImplementedException();
					return ([paramName], context.Skip(2).ToArray());
				case (["requestBody"], OpenApiRequestBody requestBody) when context.Count >= 3:
					{
						if (context[1] is not (["content", var mimeType], _)) throw new NotImplementedException();
						if (context[2] is not (["schema"], _)) throw new NotImplementedException();

						return ([requestBody.Content!.Count == 1 ? "" : mimeType, "request"], context.Skip(3).ToArray());
					}
				case (["parameters", _], OpenApiParameter { Name: string paramName }) when context.Count >= 1:
					if (context[1] is not (["schema"], _)) throw new NotImplementedException();
					return ([paramName], context.Skip(2).ToArray());
				case (["items"], JsonSchema):
					return (["Item"], context.Skip(1).ToArray());
				case (["properties", var propName], JsonSchema):
					return ([propName], context.Skip(1).ToArray());
				case (["additionalProperties"], JsonSchema):
					return (["AdditionalProperty"], context.Skip(1).ToArray());
				case (var parts, JsonSchema):
					return (parts, context.Skip(1).ToArray());
				case (var parts, var t):
					throw new NotImplementedException($"{string.Join(", ", parts)} {t.GetType().FullName}");
				default:
					throw new NotImplementedException();
			};
		}
	}
}