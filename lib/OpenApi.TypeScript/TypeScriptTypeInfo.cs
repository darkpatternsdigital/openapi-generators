using System;
using System.Collections.Generic;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords.Draft04;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Metadata;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;

namespace DarkPatterns.OpenApi.TypeScript;

public record TypeScriptTypeInfo(
	JsonSchemaInfo Info,
	string? Description,
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
	public static TypeScriptTypeInfo From(JsonSchema schema)
	{
		return From(schema.ResolveSchemaInfo());
	}

	public static TypeScriptTypeInfo From(JsonSchemaInfo info)
	{
		return new TypeScriptTypeInfo(
			info,
			info.TryGetAnnotation<DescriptionKeyword>()?.Description,
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
