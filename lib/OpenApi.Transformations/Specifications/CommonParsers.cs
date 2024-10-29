using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Specifications;

public static class CommonParsers
{
	public static readonly IParser<OpenApiDocument> OpenApi3_0Parser = new OpenApi.Specifications.v3_0.OpenApi3_0Parser();
	public static readonly IParser<OpenApiDocument> OpenApi3_1Parser = new OpenApi3_1.OpenApi3_1Parser();

	public static readonly IReadOnlyList<IParser<OpenApiDocument>> DefaultParsers =
	[
		OpenApi3_0Parser,
		// OpenApi3_1Parser,
	];

	public static ParseResult<TResult> Parse<TResult>(this IEnumerable<IParser<TResult>> parsers, IDocumentReference document, SchemaRegistry schemaRegistry)
		where TResult : class, IReferenceableDocument
	{
		var result = parsers
			.Where(parser => parser.CanParse(document))
			.Select(parser => parser.Parse(document, schemaRegistry))
			.FirstOrDefault()
			?? new ParseResult<TResult>(null, [new UnableToParseDiagnostic(schemaRegistry.DocumentRegistry.ResolveLocation(NodeMetadata.FromRoot(document)))]);
		if (result.Result != null)
		{
			schemaRegistry.DocumentRegistry.Register(result.Result);
			var options = new JsonSchemaParserOptions(schemaRegistry, result.Result.Dialect);
			var newDiagnostics = schemaRegistry.RecursivelyFixupAll();
			return result with { Diagnostics = [.. newDiagnostics, .. result.Diagnostics] };
		}
		return result;
	}
}

public record UnableToParseDiagnostic(Location Location) : DiagnosticBase(Location);
