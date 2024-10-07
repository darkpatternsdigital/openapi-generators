using System;
using System.Linq;
using Json.Pointer;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

public static class OpenApiFragmentHelpers
{

	public static string GetLastContextPart(this IJsonDocumentNode operation)
	{
		return PointerSegment.Parse(Uri.UnescapeDataString(operation.Metadata.Id.Fragment.Split('/').Last())).Value;
	}

}