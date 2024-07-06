using PrincipleStudios.OpenApi.Transformations;
using PrincipleStudios.OpenApi.Transformations.Abstractions;
using System.Linq;

namespace PrincipleStudios.OpenApi.CSharp
{
	public static class PathControllerTransformerFactory
	{
		public static ISourceProvider BuildCSharpPathControllerSourceProvider(this OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, string? documentNamespace, CSharpServerSchemaOptions options)
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
			SchemaSourceProvider schemaProvider = new CSharpSchemaSourceProvider(documentRegistry, schemaRegistry, documentNamespace ?? "", options, handlebarsFactory, header);
			// ISchemaSourceResolver<InlineDataType> schemaResolver = new CSharpSchemaSourceResolver(documentNamespace ?? "", options, handlebarsFactory, versionInfo);
			var controllerTransformer = new CSharpControllerTransformer(documentRegistry, schemaRegistry, document, documentNamespace ?? "", options, versionInfo, handlebarsFactory);

			var operationGrouping =
				new PathControllerSourceTransformer(documentRegistry, document, controllerTransformer, (operation, path) =>
				{
					var key = $"x-{options.ControllerNameExtension}";
					return operation.Extensions.TryGetValue(key, out var opOverride) ? opOverride?.GetValue<string>()
						: path.Extensions.TryGetValue(key, out var pathOverride) ? pathOverride?.GetValue<string>()
						: null;
				});

			result = new CompositeOpenApiSourceProvider(
				operationGrouping,
				new DotNetMvcAddServicesHelperTransformer(controllerTransformer, operationGrouping),
				schemaProvider
			);
			return result;
		}

	}
}
