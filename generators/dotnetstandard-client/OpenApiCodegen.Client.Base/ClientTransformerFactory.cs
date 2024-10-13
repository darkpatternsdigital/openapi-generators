using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;

namespace DarkPatterns.OpenApi.CSharp;

public class ClientTransformerFactory(TransformSettings settings)
{
	public CSharpClientTransformer Build(OpenApiDocument document, CSharpSchemaOptions options)
	{
		var handlebarsFactory = new HandlebarsFactory(ControllerHandlebarsTemplateProcess.CreateHandlebars);
		return new CSharpClientTransformer(settings, document, options, handlebarsFactory);
	}

	public static ISourceProvider Build(OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, CSharpSchemaOptions options)
	{
		var schemaRegistry = new SchemaRegistry(documentRegistry);
		var header = new Templates.PartialHeader(
			AppName: document.Info.Title,
			AppDescription: document.Info.Description,
			Version: document.Info.Version,
			InfoEmail: document.Info.Contact?.Email,
			CodeGeneratorVersionInfo: versionInfo
		);

		var settings = new TransformSettings(schemaRegistry, header);
		var factory = new ClientTransformerFactory(settings);
		var clientTransformer = factory.Build(document, options);
		var schemaSourceProvider = new CSharpSchemaSourceProvider(settings, options);

		return new CompositeOpenApiSourceProvider(
			clientTransformer,
			schemaSourceProvider
		);
	}

}
