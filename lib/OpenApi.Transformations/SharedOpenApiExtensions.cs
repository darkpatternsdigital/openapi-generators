using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords;
using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApi.Transformations;

public static class SharedOpenApiExtensions
{
	// TODO: use configuration options instead of hard-coded keyword
	public static bool UseOptionalAsNullable(this JsonSchemaInfo objectSchema, bool useLegacyByDefault = false)
	{
		if (objectSchema.TryGetAnnotation<UnknownKeyword>("x-ps-optional-as-nullable") is UnknownKeyword k
			&& k.Value is JsonValue n && n.TryGetValue<bool>(out var result))
			return result;

		return useLegacyByDefault;
	}
}
