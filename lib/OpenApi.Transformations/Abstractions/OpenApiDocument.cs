using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#openapi-object
/// </summary>
public record OpenApiDocument(
	Uri Id,
	OpenApiSpecVersion OpenApiSpecVersion,
	OpenApiInfo Info,
	IJsonSchemaDialect Dialect,
	IReadOnlyDictionary<string, OpenApiPath> Paths,
	IReadOnlyList<OpenApiSecurityRequirement> SecurityRequirements
) : IReferenceableDocument
{
	// TODO:
	// servers?
	// webhooks?

	// We don't use the following internally (yet?) directly, so we probably won't map them... at least for now
	// components
	// tags
	// externalDocs

	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		yield return Info;
		foreach (var path in Paths.Values)
			yield return path;
		foreach (var securityRequirement in SecurityRequirements)
			yield return securityRequirement;
	}

}
