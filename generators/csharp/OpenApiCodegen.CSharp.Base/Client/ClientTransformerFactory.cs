using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApiCodegen.CSharp.Client;

public class ClientTransformerFactory(TransformSettings settings)
{
	public ISourceProvider Build(ParseResult<OpenApiDocument> parseResult, CSharpSchemaOptions options)
	{
		if (parseResult.Result is not { } document) return new DiagnosticOnlySourceProvider(parseResult.Diagnostics);
		var handlebarsFactory = new HandlebarsFactory(ClientHandlebarsTemplateProcess.CreateHandlebars);

		var result = new CompositeOpenApiSourceProvider([
			new DiagnosticOnlySourceProvider(parseResult.Diagnostics),
			new CSharpClientTransformer(settings, document, options, handlebarsFactory),
		]);

		return new SafeSourceProvider(result, (ex) =>
		{
			if (parseResult.Diagnostics is not [])
			{
				// Assume that the parser errors caused the exception.
				return new(
					[],
					parseResult.Diagnostics
				);
			}
			return new(
				[],
				[.. ex.ToDiagnostics(settings.SchemaRegistry.DocumentRegistry, document.Metadata)]
			);
		});
	}
}
