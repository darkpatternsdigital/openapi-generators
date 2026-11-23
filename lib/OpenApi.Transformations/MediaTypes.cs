using System;

namespace DarkPatterns.OpenApi.Transformations;

/// <summary>How generated clients read a response body of a given media type.</summary>
public enum MediaTypeKind
{
	/// <summary>No content (e.g. a 204 response).</summary>
	None,
	/// <summary><c>application/json</c> or a <c>+json</c> type: deserialized.</summary>
	Json,
	/// <summary><c>text/*</c>: read as a string.</summary>
	Text,
	/// <summary>Anything else: the raw body (<c>HttpContent</c> in C#, a <c>Blob</c> in TypeScript).</summary>
	Raw,
}

public static class MediaTypes
{
	/// <summary>
	/// Classifies a declared media type (or range, such as <c>text/*</c>), ignoring parameters and case.
	/// </summary>
	public static MediaTypeKind Classify(string mediaType)
	{
		var essence = mediaType.Split(';')[0].Trim();
		if (essence.Length == 0) return MediaTypeKind.None;
		if (essence.Equals("application/json", StringComparison.OrdinalIgnoreCase)
			|| essence.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
			return MediaTypeKind.Json;
		if (essence.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
			return MediaTypeKind.Text;
		return MediaTypeKind.Raw;
	}
}
