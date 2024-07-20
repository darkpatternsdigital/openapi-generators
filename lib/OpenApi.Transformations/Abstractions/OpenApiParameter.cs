using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

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

public enum ParameterLocation
{
	Query,
	Header,
	Path,
	Cookie,
}