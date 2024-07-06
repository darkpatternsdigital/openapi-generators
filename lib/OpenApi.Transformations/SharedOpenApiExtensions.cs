using Microsoft.OpenApi.Models;
using PrincipleStudios.OpenApi.Transformations.Specifications;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords;
using System;
using System.Text.Json.Nodes;

namespace PrincipleStudios.OpenApi.Transformations;

public static class SharedOpenApiExtensions
{
	public static bool UseOptionalAsNullable(this JsonSchema objectSchema, bool useLegacyByDefault = false)
	{
		if (objectSchema.TryGetAnnotation<UnknownKeyword>("x-ps-optional-as-nullable") is UnknownKeyword k
			&& k.Value is JsonValue n && n.TryGetValue<bool>(out var result))
			return result;

		return useLegacyByDefault;
	}

	[Obsolete("Avoid Microsoft.OpenApi")]
	public static bool UseOptionalAsNullable(this OpenApiSchema objectSchema, bool useLegacyByDefault = false)
	{
		if (!objectSchema.Extensions.TryGetValue("x-ps-optional-as-nullable", out var optionalAsNullable) || optionalAsNullable is not Microsoft.OpenApi.Any.OpenApiBoolean { Value: var result })
			return useLegacyByDefault;

		return result;
	}
}
