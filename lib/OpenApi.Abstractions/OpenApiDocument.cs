using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#openapi-object
/// </summary>
public record OpenApiDocument(
	Uri Id,
	OpenApiSpecVersion OpenApiSpecVersion,
	OpenApiInfo Info,
	DocumentSettings Settings,
	IReadOnlyDictionary<string, OpenApiPath> Paths,
	IReadOnlyList<OpenApiSecurityRequirement> SecurityRequirements,
	IReadOnlyList<OpenApiServer> Servers,
	IReadOnlyDictionary<string, OpenApiPath> Webhooks
) : IReferenceableDocument
{
	// We don't use the following internally (yet?) directly, so we probably won't map them... at least for now
	// components
	// tags
	// externalDocs

	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		foreach (var server in Servers)
			yield return server;
		yield return Info;
		foreach (var path in Paths.Values)
			yield return path;
		foreach (var securityRequirement in SecurityRequirements)
			yield return securityRequirement;
		foreach (var webhook in Webhooks.Values)
			yield return webhook;
	}
}
