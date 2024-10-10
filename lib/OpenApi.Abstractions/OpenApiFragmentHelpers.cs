using System;
using System.Linq;
using Json.Pointer;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

public static class OpenApiFragmentHelpers
{

	public static string GetLastContextPart(this IJsonDocumentNode operation)
	{
		return PointerSegment.Parse(Uri.UnescapeDataString(operation.Metadata.Id.Fragment.Split('/').Last())).Value;
	}

}