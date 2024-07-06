using System.Linq;

namespace PrincipleStudios.OpenApi.Transformations.Specifications;

public static class JsonSchemaExtensions
{
	public static T? TryGetAnnotation<T>(this JsonSchema schema) where T : class, IJsonSchemaAnnotation
	{
		return schema is AnnotatedJsonSchema a
			? a.Annotations.OfType<T>().FirstOrDefault()
			: null;
	}

	public static T? TryGetAnnotation<T>(this JsonSchema schema, string keyword) where T : class, IJsonSchemaAnnotation
	{
		return schema is AnnotatedJsonSchema a
			? a.Annotations.OfType<T>().Where(k => k.Keyword == keyword).FirstOrDefault()
			: null;
	}
}