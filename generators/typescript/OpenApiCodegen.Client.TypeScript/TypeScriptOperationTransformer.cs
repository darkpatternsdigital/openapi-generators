using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.TypeScript;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

public class TypeScriptOperationTransformer(
	TransformSettings settings,
	TypeScriptSchemaOptions options,
	HandlebarsFactory handlebarsFactory
) : IOpenApiOperationTransformer
{
	private readonly TypeScriptInlineSchemas inlineSchemas = new TypeScriptInlineSchemas(options, settings.SchemaRegistry.DocumentRegistry);

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
			settings.SchemaRegistry.EnsureSchemasRegistered(schema);

		var template = new Templates.OperationTemplate(
			Header: settings.Header,

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
		var visitor = new OperationBuilderVisitor(settings.SchemaRegistry.DocumentRegistry, options);
		visitor.Visit(operation, httpMethod, new OperationBuilderVisitor.Argument(diagnostic, builder, path));

		return visitor.ToOperationTemplate(operation, httpMethod.ToUpper(), path.GetLastContextPart(), builder);
	}

	internal SourceEntry TransformBarrelFileHelper(IEnumerable<OperationDetail> operations, OpenApiTransformDiagnostic diagnostic)
	{
		var thisPath = $"operations/index.ts";
		return new SourceEntry(
			Key: thisPath,
			SourceText: handlebarsFactory.Handlebars.ProcessBarrelFile(new Templates.OperationBarrelFileModel(
				Header: settings.Header,
				Operations: (from op in operations
							 let operationName = OperationName(op.Operation, op.Method, op.Path.GetLastContextPart())
							 select new Templates.OperationReference(OperationFileName(operationName).ToNodePath(thisPath), operationName)
							 ).ToArray()
			))
		);
	}
}
