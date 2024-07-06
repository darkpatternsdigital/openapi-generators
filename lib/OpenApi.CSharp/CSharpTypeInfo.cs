using System.Collections.Generic;
using PrincipleStudios.OpenApi.Transformations.Specifications;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft04;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Applicator;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Validation;

namespace PrincipleStudios.OpenApi.CSharp;
using OpenApi3_0 = Transformations.Specifications.OpenApi3_0;

public record CSharpTypeInfo(
	JsonSchema? Schema,
	string? Type,
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
	public static CSharpTypeInfo From(JsonSchema? schema)
	{
		return new CSharpTypeInfo(
			schema,
			schema?.TryGetAnnotation<OpenApi3_0.TypeKeyword>()?.Value,
			schema?.TryGetAnnotation<FormatKeyword>()?.Format,
			schema?.TryGetAnnotation<PropertiesKeyword>()?.Properties ?? new Dictionary<string, JsonSchema>(),
			schema?.TryGetAnnotation<AdditionalPropertiesKeyword>()?.Schema,
			schema?.TryGetAnnotation<AllOfKeyword>()?.Schemas,
			schema?.TryGetAnnotation<AnyOfKeyword>()?.Schemas,
			schema?.TryGetAnnotation<OneOfKeyword>()?.Schemas,
			schema?.TryGetAnnotation<EnumKeyword>()?.Values,
			schema?.TryGetAnnotation<ItemsKeyword>()?.Schema
		);
	}
}
