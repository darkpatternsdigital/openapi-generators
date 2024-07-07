using System.Linq;
using Json.Pointer;
using PrincipleStudios.OpenApi.Transformations.Abstractions;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public static class OpenApiFragmentHelpers
{

	public static string GetLastContextPart(this IReferenceableDocumentNode operation)
	{
		return PointerSegment.Parse(operation.Id.Fragment.Split('/').Last()).Value;
	}

}