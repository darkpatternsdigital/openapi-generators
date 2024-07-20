using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApi.TypeScript;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript
{
	public static class OperationTransformerFactory
	{

		public static ISourceProvider BuildTypeScriptOperationSourceProvider(this OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, TypeScriptSchemaOptions options)
		{
			ISourceProvider? result;
			var handlebarsFactory = new HandlebarsFactory(OperationHandlebarsTemplateProcess.CreateHandlebars);
			var schemaRegistry = new SchemaRegistry();
			var header = new OpenApi.TypeScript.Templates.PartialHeader(
				AppName: document.Info.Title,
				AppDescription: document.Info.Description,
				Version: document.Info.Version,
				InfoEmail: document.Info.Contact?.Email,
				CodeGeneratorVersionInfo: versionInfo
			);
			var schemaProvider = new TypeScriptSchemaSourceProvider(documentRegistry, schemaRegistry, options, handlebarsFactory, header);
			var operationTransformer = new TypeScriptOperationTransformer(documentRegistry, schemaRegistry, document, options, versionInfo, handlebarsFactory);

			var operationsSourceProvider = new OperationSourceTransformer(documentRegistry, document, operationTransformer);

			result = new CompositeOpenApiSourceProvider(
				operationsSourceProvider,
				new AllOperationsBarrelTransformer(operationsSourceProvider, operationTransformer),
				schemaProvider
			);
			return result;
		}

	}

	public class AllOperationsBarrelTransformer : ISourceProvider
	{
		private readonly OperationSourceTransformer operationsSourceProvider;
		private TypeScriptOperationTransformer operationTransformer;

		public AllOperationsBarrelTransformer(OperationSourceTransformer operationsSourceProvider, TypeScriptOperationTransformer operationTransformer)
		{
			this.operationsSourceProvider = operationsSourceProvider;
			this.operationTransformer = operationTransformer;
		}

		public IEnumerable<SourceEntry> GetSources(OpenApiTransformDiagnostic diagnostic)
		{
			yield return operationTransformer.TransformBarrelFileHelper(operationsSourceProvider.GetOperations(diagnostic), diagnostic);
		}
	}
}
