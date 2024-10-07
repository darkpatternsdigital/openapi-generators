using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using System.Linq;

namespace DarkPatterns.OpenApi.CSharp
{
	public static class ClientTransformerFactory
	{
		public static ISourceProvider BuildCSharpClientSourceProvider(this OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, CSharpSchemaOptions options)
		{
			ISourceProvider? result;
			var handlebarsFactory = new HandlebarsFactory(ControllerHandlebarsTemplateProcess.CreateHandlebars);
			var schemaRegistry = new SchemaRegistry();
			var header = new Templates.PartialHeader(
				AppName: document.Info.Title,
				AppDescription: document.Info.Description,
				Version: document.Info.Version,
				InfoEmail: document.Info.Contact?.Email,
				CodeGeneratorVersionInfo: versionInfo
			);

			var controllerTransformer = new CSharpClientTransformer(schemaRegistry, documentRegistry, document, options, handlebarsFactory, header);
			var schemaSourceProvider = new CSharpSchemaSourceProvider(documentRegistry, schemaRegistry, options, handlebarsFactory, header);

			result = new CompositeOpenApiSourceProvider(
				controllerTransformer,
				schemaSourceProvider
			);
			return result;
		}

	}
}
