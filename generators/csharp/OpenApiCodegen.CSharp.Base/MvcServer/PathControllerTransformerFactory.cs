using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

public class PathControllerTransformerFactory(TransformSettings settings)
{
	public ISourceProvider Build(ParseResult<OpenApiDocument> parseResult, CSharpServerSchemaOptions options)
	{
		if (parseResult.Result is not { } document) return new DiagnosticOnlySourceProvider(parseResult.Diagnostics);
		var handlebarsFactory = new HandlebarsFactory(ControllerHandlebarsTemplateProcess.CreateHandlebars);
		var controllerTransformer = new CSharpControllerTransformer(settings, document, options, handlebarsFactory);

		var operationGrouping =
			new PathControllerSourceTransformer(settings.SchemaRegistry, document, controllerTransformer, (operation, path) =>
			{
				var key = $"x-{options.Extensions.ControllerName}";
				return operation.Extensions.TryGetValue(key, out var opOverride) ? opOverride?.GetValue<string>()
					: path.Extensions.TryGetValue(key, out var pathOverride) ? pathOverride?.GetValue<string>()
					: null;
			});

		var result = new CompositeOpenApiSourceProvider([
			new DiagnosticOnlySourceProvider(parseResult.Diagnostics),
			operationGrouping,
			new DotNetMvcAddServicesHelperTransformer(controllerTransformer, operationGrouping)
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
