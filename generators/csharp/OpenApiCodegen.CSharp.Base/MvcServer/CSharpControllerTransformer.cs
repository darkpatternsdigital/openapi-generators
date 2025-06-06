using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

public class CSharpControllerTransformer(TransformSettings settings, OpenApiDocument document, CSharpServerSchemaOptions options, HandlebarsFactory handlebarsFactory) : IOpenApiOperationControllerTransformer
{
	public SourceEntry TransformController(string groupName, OperationGroupData groupData, OpenApiTransformDiagnostic diagnostic)
	{
		var (summary, description, operations) = groupData;
		var baseNamespace = options.DefaultNamespace;

		var className = CSharpNaming.ToClassName(groupName + " base", options.ReservedIdentifiers());

		var resultOperations = new List<ControllerOperation>();
		var visitor = new ControllerOperationVisitor(settings.SchemaRegistry, options, controllerClassName: className);
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

		var template = new Templates.ControllerTemplate(
			Header: settings.Header(document.Id),
			SecurityAttribute: options.SecurityAttribute,

			PackageName: baseNamespace,
			ClassName: className,
			HasDescriptionOrSummary: summary?.Trim() + description?.Trim() is { Length: > 0 },
			Summary: summary,
			Description: description,

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
		return CSharpNaming.ToClassName(groupName + " controller", options.ReservedIdentifiers());
	}

	internal SourceEntry TransformAddServicesHelper(IEnumerable<string> groups, OpenApiTransformDiagnostic diagnostic)
	{
		var baseNamespace = options.DefaultNamespace;
		var addServicesClassName = CSharpNaming.ToClassName(document.Info.Title + " extensions", options.ReservedIdentifiers());
		return new SourceEntry(
			Key: $"{baseNamespace}.{addServicesClassName}.cs",
			SourceText: handlebarsFactory.Handlebars.ProcessAddServices(new Templates.AddServicesModel(
				Header: settings.Header("Add MVC Services, useful for ensuring all controllers are mapped"),
				ClassName: addServicesClassName,
				MethodName: CSharpNaming.ToMethodName(document.Info.Title, options.ReservedIdentifiers()),
				PackageName: baseNamespace,
				Controllers: (from p in groups
							  let genericTypeName = CSharpNaming.ToClassName($"T {p}", options.ReservedIdentifiers())
							  let className = CSharpNaming.ToClassName(p + " base", options.ReservedIdentifiers())
							  select new Templates.ControllerReference(genericTypeName, className)
							  ).ToArray()
			))
		);
	}

	internal IEnumerable<SourceEntry> TransformSecurityPoliciesHelper(IEnumerable<string> initialSecuritySchemes, OpenApiTransformDiagnostic diagnostic)
	{
		var baseNamespace = options.DefaultNamespace;
		var securitySchemesClassName = CSharpNaming.ToClassName(document.Info.Title + " security schemes", options.ReservedIdentifiers());
		var schemes = initialSecuritySchemes.Distinct().ToArray();
		if (schemes.Length == 0)
			return Enumerable.Empty<SourceEntry>();
		return [new SourceEntry(
			Key: $"{baseNamespace}.{securitySchemesClassName}.cs",
			SourceText: handlebarsFactory.Handlebars.ProcessSecuritySchemes(new Templates.SecuritySchemesModel(
				Header: settings.Header("Lists security schemes to be used as policies for the API"),
				ClassName: securitySchemesClassName,
				PackageName: baseNamespace,
				Schemes: [
					.. from scheme in schemes.Distinct()
					   let propertyName = CSharpNaming.ToPropertyName(scheme, [.. options.ReservedIdentifiers(), "AllPolicies"])
					   select new Templates.SecuritySchemeReference(propertyName, scheme)
				]
			))
		)];
	}
}
