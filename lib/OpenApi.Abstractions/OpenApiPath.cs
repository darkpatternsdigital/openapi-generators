using System;
using System.Collections.Generic;
using System.Linq;
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

	IReadOnlyList<OpenApiParameter> Parameters,
	IReadOnlyDictionary<string, OpenApiOperation> Operations,
	IReadOnlyDictionary<string, JsonNode?> Extensions
) : IReferenceableDocumentNode
{
	// servers?
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() =>
		[.. Parameters, .. Operations.Values];

	/// <summary>
	/// Combines Path Item parameters with an operation's own parameters: the path-level ones first,
	/// except any the operation overrides with the same <c>name</c> and <c>in</c>, then the operation's.
	/// Parsers put the result in <see cref="OpenApiOperation.Parameters"/>.
	/// </summary>
	public static IReadOnlyList<OpenApiParameter> MergeParameters(IReadOnlyList<OpenApiParameter> pathParameters, IReadOnlyList<OpenApiParameter> operationParameters) =>
		[
			.. pathParameters.Where(p => !operationParameters.Any(o => o.Name == p.Name && o.In == p.In)),
			.. operationParameters,
		];
}