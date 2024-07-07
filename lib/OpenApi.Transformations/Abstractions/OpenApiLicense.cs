using System;
using System.Collections.Generic;
using System.Linq;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#license-object
/// </summary>
public record OpenApiLicense(
	Uri Id,
	string Name,
	Uri? Url,
	string? Identifier
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() =>
		Enumerable.Empty<IJsonDocumentNode>();
}

