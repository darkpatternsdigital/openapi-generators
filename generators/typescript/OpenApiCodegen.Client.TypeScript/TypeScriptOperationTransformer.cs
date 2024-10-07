using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.TypeScript;
using DarkPatterns.OpenApi.TypeScript.Templates;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript
{
	public class TypeScriptOperationTransformer : IOpenApiOperationTransformer
	{
		private readonly DocumentRegistry documentRegistry;
		private readonly ISchemaRegistry schemaRegistry;
		private readonly TypeScriptInlineSchemas inlineSchemas;
		private readonly OpenApiDocument document;
		private readonly TypeScriptSchemaOptions options;
		private readonly string versionInfo;
		private readonly HandlebarsFactory handlebarsFactory;

		public TypeScriptOperationTransformer(DocumentRegistry documentRegistry, ISchemaRegistry schemaRegistry, OpenApiDocument document, TypeScriptSchemaOptions options, string versionInfo, HandlebarsFactory handlebarsFactory)
		{
			this.documentRegistry = documentRegistry;
			this.schemaRegistry = schemaRegistry;
			this.inlineSchemas = new TypeScriptInlineSchemas(options, documentRegistry);
			this.document = document;
			this.options = options;
			this.versionInfo = versionInfo;
			this.handlebarsFactory = handlebarsFactory;
		}

		public string OperationName(OpenApiOperation operation, string httpMethod, string path)
		{
			return TypeScriptNaming.ToMethodName(operation.OperationId ?? $"{httpMethod} {path}", options.ReservedIdentifiers());
		}

		private static string OperationFileName(string operationName)
		{
			return $"operations/{operationName}.ts";
		}


		public SourceEntry TransformOperation(OpenApiPath path, string method, OpenApiOperation operation, OpenApiTransformDiagnostic diagnostic)
		{
			foreach (var schema in operation.GetNestedNodes(recursive: true).OfType<JsonSchema>())
				schemaRegistry.EnsureSchemasRegistered(schema);

			var template = new Templates.OperationTemplate(
				Header: new PartialHeader(
					AppName: document.Info.Title,
					AppDescription: document.Info.Description,
					Version: document.Info.Version,
					InfoEmail: document.Info.Contact?.Email,
					CodeGeneratorVersionInfo: versionInfo
				),

				Operation: ToOperation(path, method, operation, diagnostic)
			);

			var entry = handlebarsFactory.Handlebars.ProcessOperation(template);
			return new SourceEntry(
				Key: OperationFileName(OperationName(operation, method, path.GetLastContextPart())),
				SourceText: entry
			);
		}

		private Templates.Operation ToOperation(OpenApiPath path, string httpMethod, OpenApiOperation operation, OpenApiTransformDiagnostic diagnostic)
		{
			var builder = new OperationBuilderVisitor.OperationBuilder(operation);
			var visitor = new OperationBuilderVisitor(documentRegistry, options);
			visitor.Visit(operation, httpMethod, new OperationBuilderVisitor.Argument(diagnostic, builder, path));

			return visitor.ToOperationTemplate(operation, httpMethod.ToUpper(), path.GetLastContextPart(), builder);
		}

		internal SourceEntry TransformBarrelFileHelper(IEnumerable<OperationDetail> operations, OpenApiTransformDiagnostic diagnostic)
		{
			var thisPath = $"operations/index.ts";
			return new SourceEntry(
				Key: thisPath,
				SourceText: handlebarsFactory.Handlebars.ProcessBarrelFile(new Templates.OperationBarrelFileModel(
					Header: new PartialHeader(
						AppName: document.Info.Title,
						AppDescription: document.Info.Description,
						Version: document.Info.Version,
						InfoEmail: document.Info.Contact?.Email,
						CodeGeneratorVersionInfo: versionInfo
					),
					Operations: (from op in operations
								 let operationName = OperationName(op.Operation, op.Method, op.Path.GetLastContextPart())
								 select new Templates.OperationReference(OperationFileName(operationName).ToNodePath(thisPath), operationName)
								 ).ToArray()
				))
			);
		}
	}
}
