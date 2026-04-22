using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.TypeScript;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.Json.Diagnostics;
using System.IO;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

public class TypeScriptOperationTransformer(
	TransformSettings settings,
	HandlebarsFactory handlebarsFactory
) : IOpenApiOperationTransformer
{
	public string OperationName(OpenApiOperation operation, string httpMethod, string path)
	{
		if (!settings.SchemaRegistry.DocumentRegistry.TryGetDocumentSettings<TypeScriptSchemaOptions>(operation.Metadata.Id, out var options))
			throw new DiagnosticException(MissingTypeScriptSchemaOptionsDiagnostic.Builder(operation.Metadata.Id));
		return TypeScriptNaming.ToMethodName(operation.OperationId ?? $"{httpMethod} {path}", options.ReservedIdentifiers());
	}

	private static string OperationFileName(string operationName)
	{
		return $"operations/{operationName}.ts";
	}


	public SourceEntry TransformOperation(OpenApiPath path, string method, OpenApiOperation operation, OpenApiTransformDiagnostic diagnostic)
	{
		foreach (var schema in operation.GetNestedNodes(recursive: true).OfType<JsonSchema>())
			settings.SchemaRegistry.EnsureSchemaRegistered(schema);

		var template = new Templates.OperationTemplate(
			Header: settings.Header(operation.Id),

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
		if (!settings.SchemaRegistry.DocumentRegistry.TryGetDocumentSettings<TypeScriptSchemaOptions>(operation.Metadata.Id, out var options))
			throw new DiagnosticException(MissingTypeScriptSchemaOptionsDiagnostic.Builder(operation.Metadata.Id));
		var builder = new OperationBuilderVisitor.OperationBuilder(operation);
		var visitor = new OperationBuilderVisitor(settings.SchemaRegistry.DocumentRegistry, options);
		visitor.Visit(operation, httpMethod, new OperationBuilderVisitor.Argument(diagnostic, builder, path));

		return visitor.ToOperationTemplate(operation, httpMethod.ToUpper(), path.GetLastContextPart(), builder);
	}

	internal IEnumerable<SourceEntry> TransformBarrelFileHelper(IEnumerable<OperationDetail> operations, OpenApiTransformDiagnostic diagnostic)
	{
		return from operation in operations
			   let options = settings.SchemaRegistry.DocumentRegistry.GetDocumentSettings<TypeScriptSchemaOptions>(operation.Operation.Metadata.Id)
			   where options != null
			   group operation by options.OperationsFolder into operationsByFolder
			   let thisPath = Path.Join(operationsByFolder.Key, "index.ts")
			   select new SourceEntry(
				 Key: thisPath,
				 SourceText: handlebarsFactory.Handlebars.ProcessBarrelFile(new Templates.OperationBarrelFileModel(
					 Header: settings.Header("All operations"),
					 Operations: (from op in operationsByFolder
								  let operationName = OperationName(op.Operation, op.Method, op.Path.GetLastContextPart())
								  select new Templates.OperationReference(OperationFileName(operationName).ToRelativeNodePath(thisPath), operationName)
								  ).ToArray()
				 ))
			 );
	}
}
