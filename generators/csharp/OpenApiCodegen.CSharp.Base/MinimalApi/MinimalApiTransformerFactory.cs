using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.OpenApi.CSharp;

namespace DarkPatterns.OpenApiCodegen.CSharp.MinimalApi;

public class MinimalApiTransformerFactory(TransformSettings settings)
{
	public ISourceProvider Build(ParseResult<OpenApiDocument> parseResult, CSharpSchemaOptions options)
	{
		if (parseResult.Document is not { } document) return new DiagnosticOnlySourceProvider(parseResult.Diagnostics);
		var handlebarsFactory = new HandlebarsFactory(MinimalApiHandlebarsTemplateProcess.CreateHandlebars);

		return new CompositeOpenApiSourceProvider([
			new DiagnosticOnlySourceProvider(parseResult.Diagnostics),
			new SafeSourceProvider(
				new MinimalApiTransformer(settings, document, options, handlebarsFactory),
				SafeSourceProvider.DefaultExceptionHandler(settings.SchemaRegistry.DocumentRegistry, document.Metadata)
			)
		]);
	}
}
