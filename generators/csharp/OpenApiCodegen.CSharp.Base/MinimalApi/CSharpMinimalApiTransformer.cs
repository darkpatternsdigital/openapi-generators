using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Transformations;
using System;
using System.Collections.Generic;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApiCodegen.CSharp.MinimalApi;

public class CSharpMinimalApiTransformer(TransformSettings settings, OpenApiDocument document, CSharpServerSchemaOptions options, HandlebarsFactory handlebarsFactory) : IOpenApiOperationControllerTransformer
{
	public SourceEntry TransformController(string groupName, OperationGroupData groupData, OpenApiTransformDiagnostic diagnostic)
	{
		var (summary, description, operations) = groupData;
		var baseNamespace = options.DefaultNamespace;

		var interfaceName = CSharpNaming.ToClassName("I " + groupName, options.ReservedIdentifiers());

		var resultOperations = new List<ControllerOperation>();
		var visitor = new ControllerOperationVisitor(settings.SchemaRegistry, options, controllerClassName: interfaceName, documentSecurityRequirements: document.SecurityRequirements, securitySchemes: document.SecuritySchemes, toSourceReference: settings.ToSourceReference, operationNameScope: null);
		foreach (var (operation, method, path) in operations)
		{
			try
			{
				visitor.Visit(operation, method, new ControllerOperationVisitor.Argument(diagnostic, resultOperations.Add, CurrentPath: path));
			}
			catch (Exception ex)
			{
				diagnostic.Diagnostics.AddRange(ex.ToDiagnostics(settings.SchemaRegistry.DocumentRegistry, operation.Metadata));
			}
		}

		var template = new Templates.MinimalApiTemplate(
			Header: settings.Header(document.Id),

			PackageName: baseNamespace,
			InterfaceName: interfaceName,
			EndpointsClassName: CSharpNaming.ToClassName(groupName + " endpoints", options.ReservedIdentifiers()),
			MapMethodName: CSharpNaming.ToMethodName("map " + groupName, options.ReservedIdentifiers()),

			Summary: summary,
			Description: description,

			Operations: resultOperations.ToArray()
		);

		return new SourceEntry(
			Key: $"{baseNamespace}.{interfaceName}.cs",
			SourceText: handlebarsFactory.Handlebars.ProcessMinimalApi(template)
		);
	}

	public string SanitizeGroupName(string groupName)
	{
		return CSharpNaming.ToClassName(groupName + " api", options.ReservedIdentifiers());
	}
}
