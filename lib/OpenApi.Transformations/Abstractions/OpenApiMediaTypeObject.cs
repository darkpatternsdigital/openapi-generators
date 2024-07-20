using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#mediaTypeObject
/// </summary>
public record OpenApiMediaTypeObject(
	Uri Id,
	JsonSchema? Schema
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		if (Schema != null)
			yield return Schema;
	}
}

