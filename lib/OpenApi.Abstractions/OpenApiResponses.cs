using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#responsesObject
/// </summary>
/// <remarks>
/// <see cref="StatusCodeRangeResponses"/> are keyed by the first digit of the range, such as 4 for `4XX`.
/// </remarks>
public record OpenApiResponses(
	Uri Id,
	OpenApiResponse? Default,
	IReadOnlyDictionary<int, OpenApiResponse> StatusCodeResponses,
	IReadOnlyDictionary<int, OpenApiResponse> StatusCodeRangeResponses
) : IReferenceableDocumentNode
{

	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		if (Default != null)
			yield return Default;
		foreach (var n in StatusCodeResponses.Values)
			yield return n;
		foreach (var n in StatusCodeRangeResponses.Values)
			yield return n;
	}
}
