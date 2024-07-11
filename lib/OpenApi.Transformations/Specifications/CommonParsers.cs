using PrincipleStudios.OpenApi.Transformations.Abstractions;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;
using PrincipleStudios.OpenApi.Transformations.DocumentTypes;
using System.Collections.Generic;
using System.Linq;

namespace PrincipleStudios.OpenApi.Transformations.Specifications;

public static class CommonParsers
{
	public static readonly IParser<OpenApiDocument> OpenApi3_0Parser = new OpenApi3_0.OpenApi3_0Parser();
	public static readonly IParser<OpenApiDocument> OpenApi3_1Parser = new OpenApi3_1.OpenApi3_1Parser();

	public static readonly IReadOnlyList<IParser<OpenApiDocument>> DefaultParsers =
	[
		OpenApi3_0Parser,
		// OpenApi3_1Parser,
	];

	public static ParseResult<TResult> Parse<TResult>(this IEnumerable<IParser<TResult>> parsers, IDocumentReference document, DocumentRegistry documentRegistry)
		where TResult : class, IReferenceableDocument
	{
		var result = parsers
			.Where(parser => parser.CanParse(document))
			.Select(parser => parser.Parse(document, documentRegistry))
			.FirstOrDefault()
			?? new ParseResult<TResult>(null, [new UnableToParseDiagnostic(documentRegistry.ResolveLocation(NodeMetadata.FromRoot(document)))]);
		if (result.Document != null)
		{
			documentRegistry.Register(result.Document);
			var options = new JsonSchemaParserOptions(documentRegistry, result.Document.Dialect);
			do
			{
				var schemas = result.Document.GetNestedNodes(recursive: true).OfType<JsonSchema>().Where(s => !s.IsFixupComplete).ToArray();
				foreach (var schema in schemas)
				{
					schema.FixupInPlace(options);
				}
			} while (result.Document.GetNestedNodes(recursive: true).OfType<JsonSchema>().Where(s => !s.IsFixupComplete).Any());
		}
		return result;
	}
}

public record UnableToParseDiagnostic(Location Location) : DiagnosticBase(Location);