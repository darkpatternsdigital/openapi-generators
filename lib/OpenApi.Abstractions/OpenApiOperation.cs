using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApi.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#operationObject
/// </summary>
public record OpenApiOperation(
	Uri Id,

	IReadOnlyList<string>? Tags,
	string? Summary,
	string? Description,
	string? OperationId,

	IReadOnlyList<OpenApiParameter> Parameters,
	OpenApiRequestBody? RequestBody,
	OpenApiResponses? Responses,
	IReadOnlyList<OpenApiSecurityRequirement> SecurityRequirements,

	bool Deprecated,
	IReadOnlyDictionary<string, JsonNode?> Extensions
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		foreach (var p in Parameters)
			yield return p;
		if (RequestBody != null) yield return RequestBody;
		if (Responses != null) yield return Responses;
	}

	// externalDocs?
	// callbacks?
	// servers?
}