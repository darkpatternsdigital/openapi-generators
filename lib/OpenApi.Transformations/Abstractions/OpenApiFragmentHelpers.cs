using System;
using System.Linq;
using Json.Pointer;
using PrincipleStudios.OpenApi.Transformations.Abstractions;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public static class OpenApiFragmentHelpers
{

	public static string GetLastContextPart(this IJsonDocumentNode operation)
	{
		return PointerSegment.Parse(Uri.UnescapeDataString(operation.Metadata.Id.Fragment.Split('/').Last())).Value;
	}

}