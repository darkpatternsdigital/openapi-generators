using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

public record OpenApiServer(Uri Id, Uri Url, string? Description, IReadOnlyDictionary<string, OpenApiServerVariable> Variables) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		foreach (var e in Variables.Values)
			yield return e;
	}
}
