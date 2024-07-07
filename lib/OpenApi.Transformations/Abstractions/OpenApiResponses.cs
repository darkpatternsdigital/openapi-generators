using System;
using System.Collections.Generic;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#responsesObject
/// </summary>
public record OpenApiResponses(
	Uri Id,
	OpenApiResponse? Default,
	IReadOnlyDictionary<int, OpenApiResponse> StatusCodeResponses
) : IReferenceableDocumentNode
{

	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		if (Default != null)
			yield return Default;
		foreach (var n in StatusCodeResponses.Values)
			yield return n;
	}
}
