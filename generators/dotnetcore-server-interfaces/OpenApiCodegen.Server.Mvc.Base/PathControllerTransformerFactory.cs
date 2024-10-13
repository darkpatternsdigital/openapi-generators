using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;

namespace DarkPatterns.OpenApi.CSharp;

public class PathControllerTransformerFactory(TransformSettings settings)
{
	public ISourceProvider Build(OpenApiDocument document, CSharpServerSchemaOptions options)
	{
		ISourceProvider? result;
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

		result = new CompositeOpenApiSourceProvider(
			operationGrouping,
			new DotNetMvcAddServicesHelperTransformer(controllerTransformer, operationGrouping)
		);
		return result;
	}

	public static CompositeOpenApiSourceProvider BuildComposite(OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, CSharpServerSchemaOptions options)
	{
		var handlebarsFactory = new HandlebarsFactory(ControllerHandlebarsTemplateProcess.CreateHandlebars);
		var schemaRegistry = new SchemaRegistry(documentRegistry);
		var header = new Templates.PartialHeader(
			AppName: document.Info.Title,
			AppDescription: document.Info.Description,
			Version: document.Info.Version,
			InfoEmail: document.Info.Contact?.Email,
			CodeGeneratorVersionInfo: versionInfo
		);
		var settings = new TransformSettings(schemaRegistry, header);
		var schemaProvider = new CSharpSchemaSourceProvider(settings, options);
		var factory = new PathControllerTransformerFactory(settings);

		return new CompositeOpenApiSourceProvider(
			factory.Build(document, options),
			schemaProvider
		);
	}

}
