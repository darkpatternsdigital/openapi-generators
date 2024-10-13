using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.TypeScript;
using Microsoft.Win32;
using System.Reflection.Metadata;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

public class OperationTransformerFactory(TransformSettings settings)
{
	public ISourceProvider Build(OpenApiDocument document, TypeScriptSchemaOptions options)
	{
		ISourceProvider? result;
		var handlebarsFactory = new HandlebarsFactory(OperationHandlebarsTemplateProcess.CreateHandlebars);
		var schemaProvider = new TypeScriptSchemaSourceProvider(settings, options, handlebarsFactory);
		var operationTransformer = new TypeScriptOperationTransformer(settings, options, handlebarsFactory);

		var operationsSourceProvider = new OperationSourceTransformer(settings.SchemaRegistry.DocumentRegistry, document, operationTransformer);

		result = new CompositeOpenApiSourceProvider(
			operationsSourceProvider,
			new AllOperationsBarrelTransformer(operationsSourceProvider, operationTransformer),
			schemaProvider
		);
		return result;
	}

	public static CompositeOpenApiSourceProvider BuildComposite(OpenApiDocument document, Json.Documents.DocumentRegistry registry, string versionInfo, TypeScriptSchemaOptions options)
	{
		var header = new OpenApi.TypeScript.Templates.PartialHeader(
			AppName: document.Info.Title,
			AppDescription: document.Info.Description,
			Version: document.Info.Version,
			InfoEmail: document.Info.Contact?.Email,
			CodeGeneratorVersionInfo: versionInfo
		);
		var schemaRegistry = new SchemaRegistry(registry);
		var settings = new TransformSettings(schemaRegistry, header);
		var factory = new OperationTransformerFactory(settings);
		var schemaProvider = new TypeScriptSchemaSourceProvider(settings, options);

		return new CompositeOpenApiSourceProvider(
			factory.Build(document, options),
			schemaProvider
		);
	}
}
