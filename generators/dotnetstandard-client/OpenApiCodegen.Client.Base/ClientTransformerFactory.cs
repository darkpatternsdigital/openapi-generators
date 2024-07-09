using PrincipleStudios.OpenApi.Transformations;
using PrincipleStudios.OpenApi.Transformations.Abstractions;
using System.Linq;

namespace PrincipleStudios.OpenApi.CSharp
{
	public static class ClientTransformerFactory
	{
		public static ISourceProvider BuildCSharpClientSourceProvider(this OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, string? documentNamespace, CSharpSchemaOptions options)
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

			var controllerTransformer = new CSharpClientTransformer(schemaRegistry, documentRegistry, document, documentNamespace ?? "", options, handlebarsFactory, header);
			var schemaSourceProvider = new CSharpSchemaSourceProvider(documentRegistry, schemaRegistry, documentNamespace ?? "", options, handlebarsFactory, header, [document]);

			result = new CompositeOpenApiSourceProvider(
				controllerTransformer,
				schemaSourceProvider
			);
			return result;
		}

	}
}
