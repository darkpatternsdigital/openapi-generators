using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

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

	bool Deprecated,
	IReadOnlyDictionary<string, JsonNode?> Extensions
) : IReferenceableDocumentNode
{

	// externalDocs?
	// callbacks?
	// servers?
}