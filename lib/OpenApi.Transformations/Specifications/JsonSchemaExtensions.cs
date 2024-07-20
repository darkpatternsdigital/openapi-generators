using System.Collections.Generic;
using System.Linq;

namespace DarkPatterns.OpenApi.Transformations.Specifications;

public static class JsonSchemaExtensions
{
	public static T? TryGetAnnotation<T>(this JsonSchema schema) where T : class, IJsonSchemaAnnotation
	{
		return schema.Annotations.OfType<T>().FirstOrDefault();
	}

	public static T? TryGetAnnotation<T>(this JsonSchema schema, string keyword) where T : class, IJsonSchemaAnnotation
	{
		return schema.Annotations.OfType<T>().Where(k => k.Keyword == keyword).FirstOrDefault();
	}
}