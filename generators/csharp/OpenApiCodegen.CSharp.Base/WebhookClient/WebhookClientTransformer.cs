using DarkPatterns.OpenApi.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApiCodegen.CSharp.WebhookClient.Templates;
using DarkPatterns.OpenApi.CSharp;

namespace DarkPatterns.OpenApiCodegen.CSharp.WebhookClient;

public class WebhookClientTransformer(TransformSettings settings, OpenApiDocument document, CSharpSchemaOptions options, HandlebarsFactory handlebarsFactory) : ISourceProvider
{
	public SourceEntry TransformOperations(OpenApiTransformDiagnostic diagnostic)
	{
		foreach (var schema in document.GetNestedNodes(recursive: true).OfType<JsonSchema>())
			settings.SchemaRegistry.EnsureSchemaRegistered(schema);
		var baseNamespace = options.DefaultNamespace;

		var className = CSharpNaming.ToClassName("webhook operations", options.ReservedIdentifiers());

		var resultOperations = new List<Operation>();
		var visitor = new OperationVisitor(settings.SchemaRegistry, options, controllerClassName: className);
		visitor.Visit(document, new OperationVisitor.Argument(diagnostic, resultOperations.Add));

		resultOperations = (from operation in resultOperations
							select operation).ToList();

		var template = new FullTemplate(
			Header: settings.Header(document.Id),

			PackageName: baseNamespace,
			ClassName: className,

			Operations: resultOperations.ToArray()
		);

		var entry = handlebarsFactory.Handlebars.ProcessDocument(template);
		return new SourceEntry(
			Key: $"{baseNamespace}.{className}.cs",
			SourceText: entry
		);
	}

	public SourcesResult GetSources()
	{
		var diagnostic = new OpenApiTransformDiagnostic();
		return new([TransformOperations(diagnostic)], [.. diagnostic.Diagnostics]);
	}
}
