using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#parameterObject
/// </summary>
public record OpenApiParameter(
	Uri Id,

	string Name,
	ParameterLocation In,
	string? Description,
	bool Required,
	bool Deprecated,
	bool AllowEmptyValue,

	// TODO - there's a bunch of ways to serialize parameters, this is only the simplest way
	string Style,
	bool Explode,
	bool AllowReserved,
	JsonSchema? Schema,
	// Used instead of Schema for more complex serialization
	IReadOnlyDictionary<string, OpenApiMediaTypeObject>? Content
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		if (Schema != null)
			yield return Schema;
		if (Content != null)
			foreach (var mediaType in Content.Values)
				yield return mediaType;
	}
}

public enum ParameterLocation
{
	Query,
	Header,
	Path,
	Cookie,
}