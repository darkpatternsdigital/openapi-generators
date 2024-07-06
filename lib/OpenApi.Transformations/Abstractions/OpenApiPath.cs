using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#pathItemObject
/// </summary>
public record OpenApiPath(
	Uri Id,
	string? Summary,
	string? Description,

	IReadOnlyDictionary<string, OpenApiOperation> Operations,
	IReadOnlyDictionary<string, JsonNode?> Extensions
) : IReferenceableDocumentNode
{
	// TODO: parameters
	// servers?
}