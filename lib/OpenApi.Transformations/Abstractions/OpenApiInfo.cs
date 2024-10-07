using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using System;
using System.Collections.Generic;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#info-object
/// </summary>
public record OpenApiInfo(
	Uri Id,
	string Title,
	string? Summary,
	string? Description,
	Uri? TermsOfService,
	OpenApiContact? Contact,
	OpenApiLicense? License,
	string Version
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		if (Contact != null)
			yield return Contact;
		if (License != null)
			yield return License;
	}
}
