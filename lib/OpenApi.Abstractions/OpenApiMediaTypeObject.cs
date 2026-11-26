using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#mediaTypeObject
/// </summary>
public record OpenApiMediaTypeObject(
	Uri Id,
	JsonSchema? Schema,
	IReadOnlyDictionary<string, OpenApiEncoding>? Encoding
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		if (Schema != null)
			yield return Schema;
		if (Encoding != null)
			foreach (var encoding in Encoding.Values)
				yield return encoding;
	}
}

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#encodingObject
/// </summary>
public record OpenApiEncoding(
	Uri Id,
	string? ContentType,
	IReadOnlyList<OpenApiParameter> Headers,
	string Style,
	bool Explode,
	bool AllowReserved
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() => Headers;
}

