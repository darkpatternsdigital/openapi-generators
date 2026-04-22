using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.TypeScript;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

public class OperationTransformerFactory(TransformSettings settings)
{
	public ISourceProvider Build(OpenApiDocument document)
	{
		ISourceProvider? result;
		var handlebarsFactory = new HandlebarsFactory(OperationHandlebarsTemplateProcess.CreateHandlebars);
		var schemaProvider = new TypeScriptSchemaSourceProvider(settings, handlebarsFactory);
		var operationTransformer = new TypeScriptOperationTransformer(settings, handlebarsFactory);

		var operationsSourceProvider = new OperationSourceTransformer(settings.SchemaRegistry.DocumentRegistry, document, operationTransformer);

		result = new CompositeOpenApiSourceProvider([
			operationsSourceProvider,
			new AllOperationsBarrelTransformer(operationsSourceProvider, operationTransformer),
			schemaProvider
		]);
		return result;
	}

	public ISourceProvider BuildOperations(OpenApiDocument document)
	{
		ISourceProvider? result;
		var handlebarsFactory = new HandlebarsFactory(OperationHandlebarsTemplateProcess.CreateHandlebars);
		var operationTransformer = new TypeScriptOperationTransformer(settings, handlebarsFactory);

		var operationsSourceProvider = new OperationSourceTransformer(settings.SchemaRegistry.DocumentRegistry, document, operationTransformer);

		result = new CompositeOpenApiSourceProvider([
			operationsSourceProvider,
			new AllOperationsBarrelTransformer(operationsSourceProvider, operationTransformer)
		]);
		return result;
	}
}
