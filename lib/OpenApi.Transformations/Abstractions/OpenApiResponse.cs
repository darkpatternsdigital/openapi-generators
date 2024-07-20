using System;
using System.Collections.Generic;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#responseObject
/// </summary>
public record OpenApiResponse(
	Uri Id,
	string Description,
	IReadOnlyList<OpenApiParameter> Headers,
	IReadOnlyDictionary<string, OpenApiMediaTypeObject>? Content

) : IReferenceableDocumentNode
{
	// links?
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		foreach (var h in Headers)
			yield return h;
		if (Content != null)
			foreach (var h in Content.Values)
				yield return h;
	}
}