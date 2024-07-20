using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApi.Transformations.Specifications.Keywords;
using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApi.Transformations;

public static class SharedOpenApiExtensions
{
	public static bool UseOptionalAsNullable(this JsonSchema objectSchema, bool useLegacyByDefault = false)
	{
		if (objectSchema.TryGetAnnotation<UnknownKeyword>("x-ps-optional-as-nullable") is UnknownKeyword k
			&& k.Value is JsonValue n && n.TryGetValue<bool>(out var result))
			return result;

		return useLegacyByDefault;
	}
}
