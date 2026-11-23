using System;
using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>How generated C# clients match and read response bodies by media type.</summary>
public static class CSharpMediaTypes
{
	/// <summary>The body type of a response of this media type: the schema's type for JSON, else by kind.</summary>
	public static string? ToBodyType(MediaTypeKind kind, string? schemaDataType) =>
		kind switch
		{
			MediaTypeKind.Json => schemaDataType,
			MediaTypeKind.Text => "string",
			MediaTypeKind.Raw => "global::System.Net.Http.HttpContent",
			_ => null,
		};

	/// <summary>
	/// A C# condition matching the declared media type (or range) against <paramref name="variable"/>,
	/// the response's media type without parameters, ignoring case.
	/// </summary>
	public static string ToCondition(string declaredMediaType, string variable)
	{
		var essence = declaredMediaType.Split(';')[0].Trim();
		if (essence == "*/*")
			return "true";
		if (essence.EndsWith("/*", StringComparison.Ordinal))
			return $"{variable}?.StartsWith(\"{essence.Substring(0, essence.Length - 1)}\", global::System.StringComparison.OrdinalIgnoreCase) == true";
		return $"string.Equals({variable}, \"{essence}\", global::System.StringComparison.OrdinalIgnoreCase)";
	}
}
