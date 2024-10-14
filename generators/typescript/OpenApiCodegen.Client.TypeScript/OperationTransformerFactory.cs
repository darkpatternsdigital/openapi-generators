using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.TypeScript;
using DarkPatterns.OpenApiCodegen.Handlebars;

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

		result = new CompositeOpenApiSourceProvider([
			operationsSourceProvider,
			new AllOperationsBarrelTransformer(operationsSourceProvider, operationTransformer),
			schemaProvider
		]);
		return result;
	}
}
