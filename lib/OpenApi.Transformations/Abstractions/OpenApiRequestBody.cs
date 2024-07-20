using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#requestBodyObject
/// </summary>
public record OpenApiRequestBody(
	Uri Id,
	string? Description,
	IReadOnlyDictionary<string, OpenApiMediaTypeObject>? Content,
	bool Required
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() =>
		Content?.Values
		?? Enumerable.Empty<IReferenceableDocumentNode>();
}
