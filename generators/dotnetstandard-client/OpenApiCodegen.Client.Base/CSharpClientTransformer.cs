using DarkPatterns.OpenApi.CSharp.Templates;
using DarkPatterns.OpenApi.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApiCodegen;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.OpenApi.CSharp;

public class CSharpClientTransformer(TransformSettings settings, OpenApiDocument document, CSharpSchemaOptions options, HandlebarsFactory handlebarsFactory) : ISourceProvider
{
	public SourceEntry TransformOperations(OpenApiTransformDiagnostic diagnostic)
	{
		foreach (var schema in document.GetNestedNodes(recursive: true).OfType<JsonSchema>())
			settings.SchemaRegistry.EnsureSchemasRegistered(schema);
		var baseNamespace = options.DefaultNamespace;

		var className = CSharpNaming.ToClassName("operations", options.ReservedIdentifiers());

		var resultOperations = new List<Operation>();
		var visitor = new OperationVisitor(settings.SchemaRegistry, options, controllerClassName: className);
		visitor.Visit(document, new OperationVisitor.Argument(diagnostic, resultOperations.Add));

		resultOperations = (from operation in resultOperations
							select operation with { Path = operation.Path.Substring(1) }).ToList();

		var template = new Templates.FullTemplate(
			Header: settings.Header,

			PackageName: baseNamespace,
			ClassName: className,

			Operations: resultOperations.ToArray()
		);

		var entry = handlebarsFactory.Handlebars.ProcessController(template);
		return new SourceEntry(
			Key: $"{baseNamespace}.{className}.cs",
			SourceText: entry
		);
	}

	public string SanitizeGroupName(string groupName)
	{
		return CSharpNaming.ToClassName(groupName + " client", options.ReservedIdentifiers());
	}

	internal SourceEntry TransformAddServicesHelper(OpenApiTransformDiagnostic diagnostic)
	{
		var baseNamespace = options.DefaultNamespace;
		return new SourceEntry(
			Key: $"{baseNamespace}.AddServicesExtensions.cs",
			SourceText: handlebarsFactory.Handlebars.ProcessAddServices(new Templates.AddServicesModel(
				Header: settings.Header,
				MethodName: CSharpNaming.ToMethodName(document.Info.Title, options.ReservedIdentifiers()),
				PackageName: baseNamespace
			))
		);
	}

	public IEnumerable<SourceEntry> GetSources(OpenApiTransformDiagnostic diagnostic)
	{
		yield return TransformOperations(diagnostic);
	}
}
