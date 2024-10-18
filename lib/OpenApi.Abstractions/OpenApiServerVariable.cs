using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

public record OpenApiServerVariable(Uri Id, IReadOnlyList<string> AllowedValues, string DefaultValue, string? Description) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		return Enumerable.Empty<IJsonDocumentNode>();
	}
}
