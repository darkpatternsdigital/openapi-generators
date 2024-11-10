using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Pointer;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using System.Diagnostics.CodeAnalysis;

namespace DarkPatterns.OpenApi.TypeScript;
using v3_0 = DarkPatterns.OpenApi.Specifications.v3_0;

public record TypeScriptImportReference(JsonSchema Schema, string Member, string File);

public record TypeScriptInlineDefinition(string Text, IReadOnlyList<TypeScriptImportReference> Imports, bool Nullable = false, bool IsEnumerable = false)
{
	public TypeScriptInlineDefinition MakeNullable() =>
		Nullable ? this : this with { Text = Text + " | null", Nullable = true };
}

public class TypeScriptInlineSchemas(TypeScriptSchemaOptions options, DocumentRegistry documentRegistry)
{
	public static readonly TypeScriptInlineDefinition AnyObject = new("any", [], Nullable: true);

	public bool ProduceSourceEntry(JsonSchema schema)
	{
		return ProduceSourceEntry(schema.ResolveSchemaInfo());
	}

	public bool ProduceSourceEntry(JsonSchemaInfo schema)
	{
		if (schema.Original.Id.OriginalString != schema.EffectiveSchema.Metadata.Id.OriginalString) return false;

		var nodes = documentRegistry.GetNodesTo(schema.EffectiveSchema.Metadata.Id);
		if (nodes.LastOrDefault() is (["allOf", _], JsonSchema))
			return false;
		if (nodes.Length == 2 && nodes[1] is (["components", "schemas", _], JsonSchema))
			return true;

		// We're going to inline much less than TS allows because it makes things easier for developers
		return TypeScriptTypeInfo.From(schema) switch
		{
			{ TypeAnnotation.AllowsObject: true, Properties.Count: 0, AdditionalProperties: JsonSchema } => true,
			{ AllOf.Count: > 0 } => true,
			{ AnyOf.Count: > 0 } => true,
			{ OneOf.Count: > 0 } => true,
			{ TypeAnnotation.AllowsString: true, Enum.Count: > 0 } => true,
			{ TypeAnnotation.AllowsArray: true } or { Items: JsonSchema _ } => false,
			{ TypeAnnotation.AllowsObject: true, Format: null } => true,
			{ Properties.Count: > 0 } => true,
			{ TypeAnnotation: { AllowsString: true } or { AllowsNumber: true } or { AllowsInteger: true } or { AllowsBoolean: true } } => false,
			{ } => false,
			_ => throw new NotSupportedException($"Unknown schema: {schema.Original.Id}"),
		};
	}

	public TypeScriptInlineDefinition ToInlineDataType(JsonSchema? schema)
	{
		if (schema == null) return new TypeScriptInlineDefinition("unknown", [], true, false);
		var schemaInfo = schema.ResolveSchemaInfo();
		return ToInlineDataType(schemaInfo);
	}

	/// <summary>
	/// Gets the inline data type for the schema, skipping the self-reference. May be unable to handle complex data types.
	/// </summary>
	public TypeScriptInlineDefinition GetInlineDataType(JsonSchema? schema)
	{
		if (schema == null) return new TypeScriptInlineDefinition("unknown", [], true, false);
		var schemaInfo = schema.ResolveSchemaInfo();
		return ForceConvertIntoInlineDataType(schemaInfo);
	}


	[return: NotNullIfNotNull(nameof(schemaInfo))]
	private TypeScriptInlineDefinition? ToInlineDataType(JsonSchemaInfo? schemaInfo)
	{
		if (schemaInfo == null) return null;
		schemaInfo = schemaInfo with { Original = schemaInfo.EffectiveSchema.Metadata };

		if (ProduceSourceEntry(schemaInfo))
			return new(UseReferenceName(schemaInfo.EffectiveSchema), [ToImportReference(schemaInfo.EffectiveSchema)]);

		var result = ForceConvertIntoInlineDataType(schemaInfo);
		return schemaInfo.TryGetAnnotation<v3_0.NullableKeyword>() is { IsNullable: true }
			? result.MakeNullable()
			: result;
	}

	/// <summary>
	/// Attempt to convert schema into an inline type even if it would normally be
	/// a reference. Allows simpler-than-usual reference types to be converted
	/// without custom Handlebars files.
	/// </summary>
	private TypeScriptInlineDefinition ForceConvertIntoInlineDataType(JsonSchemaInfo schemaInfo)
	{
		schemaInfo = schemaInfo with { Original = schemaInfo.EffectiveSchema.Metadata };

		var typeInfo = TypeScriptTypeInfo.From(schemaInfo);
		TypeScriptInlineDefinition result = typeInfo switch
		{
			{ TypeAnnotation.AllowsArray: true, Items: null } => ArrayToInline(null),
			{ Items: JsonSchema items } => ArrayToInline(items),
			{ Info.EffectiveSchema.BoolValue: false } => new TypeScriptInlineDefinition("never", [], false, false),
			{ Info.EffectiveSchema.BoolValue: true } => new TypeScriptInlineDefinition("unknown", [], true, false),
			{ TypeAnnotation: v3_0.TypeKeyword { OpenApiType: var primitiveType }, Format: var format } =>
				new(options.Find(TypeAnnotation.ToPrimitiveTypeString(primitiveType), format), []),
			{ TypeAnnotation.AllowsNumber: true, Format: var format } =>
				new(options.Find(TypeAnnotation.Common.Number, format), []),
			{ TypeAnnotation.AllowsInteger: true, Format: var format } =>
				new(options.Find(TypeAnnotation.Common.Integer, format), []),
			{ TypeAnnotation.AllowsString: true, Format: var format } =>
				new(options.Find(TypeAnnotation.Common.String, format), []),
			{ TypeAnnotation.AllowsBoolean: true, Format: var format } =>
				new(options.Find(TypeAnnotation.Common.Boolean, format), []),
			{ TypeAnnotation.AllowsObject: true, Format: var format } =>
				new(options.Find(TypeAnnotation.Common.Object, format), []),
			_ => throw new NotSupportedException($"Unknown schema: {schemaInfo.Original.Id}"),
		};
		return schemaInfo.TryGetAnnotation<v3_0.NullableKeyword>() is { IsNullable: true }
			? result.MakeNullable()
			: result;

		TypeScriptInlineDefinition ArrayToInline(JsonSchema? items)
		{
			var inline = ToInlineDataType(items);
			return new(options.ToArrayType(inline.Text), inline.Imports, IsEnumerable: true);
		}
	}

	public string ToSourceEntryKey(JsonSchema schema)
	{
		var className = UseReferenceName(schema);
		return $"models/{className}.ts";
	}

	public TypeScriptImportReference ToImportReference(JsonSchema schema)
	{
		return new TypeScriptImportReference(schema, UseReferenceName(schema), ToSourceEntryKey(schema));
	}

	private string UseReferenceName(JsonSchema schema)
	{
		return TypeScriptNaming.ToClassName(UriToClassIdentifier(schema.Metadata.Id), options.ReservedIdentifiers());
	}

	private static readonly Regex HttpSuccessRegex = new Regex("2[0-9]{2}");
	private static bool Is2xx(int statusCode) => statusCode is >= 200 and < 300;

	internal string UriToClassIdentifier(Uri uri)
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
				case (_, OpenApiDocument):
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
				case (["requestBody"], OpenApiRequestBody requestBody) when context.Count >= 3:
					{
						if (context[1] is not (["content", var mimeType], _)) throw new NotImplementedException();
						if (context[2] is not (["schema"], _)) throw new NotImplementedException();

						return ([requestBody.Content!.Count == 1 ? "" : mimeType, "request"], context.Skip(3).ToArray());
					}
				case (["components", "parameters", var paramName], OpenApiParameter) when context.Count >= 1:
					if (context[1] is not (["schema"], _)) throw new NotImplementedException();
					return ([paramName], context.Skip(2).ToArray());
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
					throw new NotImplementedException($"Could not name section '{string.Join(", ", parts)} {t.GetType().FullName}' when naming {uri.OriginalString}");
				default:
					throw new NotImplementedException();
			};
		}
	}
}
