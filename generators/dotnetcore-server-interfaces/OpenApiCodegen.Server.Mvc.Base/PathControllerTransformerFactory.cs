﻿using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using System.Linq;

namespace DarkPatterns.OpenApi.CSharp
{
	public static class PathControllerTransformerFactory
	{
		public static ISourceProvider BuildCSharpPathControllerSourceProvider(this OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, CSharpServerSchemaOptions options)
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
			var schemaProvider = new CSharpSchemaSourceProvider(documentRegistry, schemaRegistry, options, handlebarsFactory, header);
			var controllerTransformer = new CSharpControllerTransformer(documentRegistry, schemaRegistry, document, options, versionInfo, handlebarsFactory);

			var operationGrouping =
				new PathControllerSourceTransformer(documentRegistry, schemaRegistry, document, controllerTransformer, (operation, path) =>
				{
					var key = $"x-{options.Extensions.ControllerName}";
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
