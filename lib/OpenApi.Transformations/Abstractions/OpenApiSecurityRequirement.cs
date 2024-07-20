using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

public record OpenApiSecurityRequirement(
	Uri Id,
	IReadOnlyList<OpenApiSecuritySchemeRequirement> SchemeRequirements
) : IReferenceableDocumentNode
{

	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() => Enumerable.Empty<IJsonDocumentNode>();
}

public record OpenApiSecuritySchemeRequirement(string SchemeName, IReadOnlyList<string> ScopeNames)
{

}