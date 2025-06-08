using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications;

public static class JsonSchemaExtensions
{
	public static JsonSchemaInfo ResolveSchemaInfo(this JsonSchema schema, NodeMetadata? originalMetadata = null)
	{
		originalMetadata ??= schema.Metadata;
		var annotations = schema.GetAllAnnotations();
		if (annotations.IsRefOnly(out var refSchema))
			return ResolveSchemaInfo(refSchema, originalMetadata);
		// TODO: if the dialect allows it, combine refSchema andexisting annotations
		return new JsonSchemaInfo(originalMetadata, schema, annotations);
	}

	private static bool IsRefOnly(this IReadOnlyList<IJsonSchemaAnnotation> annotations, [NotNullWhen(true)] out JsonSchema? refSchema)
	{
		if (annotations is [IJsonSchemaRefAnnotation { ReferencedSchema: { } result }])
		// TODO: should allow non-evaluating annotations like defs here
		{
			refSchema = result;
			return true;
		}

		refSchema = annotations.OfType<IJsonSchemaRefAnnotation>().FirstOrDefault()?.ReferencedSchema;
		return false;
	}
}

public record JsonSchemaInfo(NodeMetadata Original, JsonSchema EffectiveSchema, IReadOnlyList<IJsonSchemaAnnotation> Annotations)
{
	public T? TryGetAnnotation<T>() where T : class, IJsonSchemaAnnotation
	{
		return Annotations.OfType<T>().FirstOrDefault();
	}

	public T? TryGetAnnotation<T>(string keyword) where T : class, IJsonSchemaAnnotation
	{
		return Annotations.OfType<T>().Where(k => k.Keyword == keyword).FirstOrDefault();
	}
}