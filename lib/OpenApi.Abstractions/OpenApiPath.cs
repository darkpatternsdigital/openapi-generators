using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

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
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() =>
		Operations.Values;
}