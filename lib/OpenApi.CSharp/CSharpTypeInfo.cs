using System;
using System.Collections.Generic;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords.Draft04;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;

namespace DarkPatterns.OpenApi.CSharp;

public record CSharpTypeInfo(
	JsonSchemaInfo Info,
	TypeAnnotation? TypeAnnotation,
	string? Format,
	IReadOnlyDictionary<string, JsonSchema> Properties,
	JsonSchema? AdditionalProperties,
	IReadOnlyList<JsonSchema>? AllOf,
	IReadOnlyList<JsonSchema>? AnyOf,
	IReadOnlyList<JsonSchema>? OneOf,
	System.Text.Json.Nodes.JsonArray? Enum,
	JsonSchema? Items
	)
{
	public static CSharpTypeInfo From(JsonSchema schema)
	{
		var info = schema.ResolveSchemaInfo();
		return From(info);
	}

	public static CSharpTypeInfo From(JsonSchemaInfo info)
	{
		return new CSharpTypeInfo(
			info,
			info.TryGetAnnotation<TypeAnnotation>(),
			info.TryGetAnnotation<FormatKeyword>()?.Format,
			info.TryGetAnnotation<PropertiesKeyword>()?.Properties ?? new Dictionary<string, JsonSchema>(),
			info.TryGetAnnotation<AdditionalPropertiesKeyword>()?.Schema,
			info.TryGetAnnotation<AllOfKeyword>()?.Schemas,
			info.TryGetAnnotation<AnyOfKeyword>()?.Schemas,
			info.TryGetAnnotation<OneOfKeyword>()?.Schemas,
			info.TryGetAnnotation<EnumKeyword>()?.Values,
			info.TryGetAnnotation<ItemsKeyword>()?.Schema
		);
	}
}
