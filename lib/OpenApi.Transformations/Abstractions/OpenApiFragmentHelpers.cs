using System.Linq;
using PrincipleStudios.OpenApi.Transformations.Abstractions;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public static class OpenApiFragmentHelpers
{

	public static string GetLastContextPart(this IReferenceableDocumentNode operation)
	{
		return operation.Id.Fragment.Split('/').Last();
	}

}