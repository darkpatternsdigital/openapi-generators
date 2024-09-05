using DarkPatterns.OpenApi.CSharp.Templates;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApi.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApiCodegen;

namespace DarkPatterns.OpenApi.CSharp
{
	public class CSharpControllerTransformer : IOpenApiOperationControllerTransformer
	{
		private readonly DocumentRegistry documentRegistry;
		private readonly ISchemaRegistry schemaRegistry;
		private readonly OpenApiDocument document;
		private readonly CSharpServerSchemaOptions options;
		private readonly string versionInfo;
		private readonly HandlebarsFactory handlebarsFactory;

		public CSharpControllerTransformer(DocumentRegistry documentRegistry, ISchemaRegistry schemaRegistry, OpenApiDocument document, CSharpServerSchemaOptions options, string versionInfo, HandlebarsFactory handlebarsFactory)
		{
			this.documentRegistry = documentRegistry;
			this.schemaRegistry = schemaRegistry;
			this.document = document;
			this.options = options;
			this.versionInfo = versionInfo;
			this.handlebarsFactory = handlebarsFactory;
		}

		public SourceEntry TransformController(string groupName, OperationGroupData groupData, OpenApiTransformDiagnostic diagnostic)
		{
			var (summary, description, operations) = groupData;
			var baseNamespace = options.DefaultNamespace;

			var className = CSharpNaming.ToClassName(groupName + " base", options.ReservedIdentifiers());

			var resultOperations = new List<ControllerOperation>();
			var visitor = new ControllerOperationVisitor(documentRegistry, schemaRegistry, options, controllerClassName: className);
			foreach (var (operation, method, path) in operations)
				visitor.Visit(operation, method, new ControllerOperationVisitor.Argument(diagnostic, resultOperations.Add, CurrentPath: path));

			var template = new Templates.ControllerTemplate(
				Header: new Templates.PartialHeader(
					AppName: document.Info.Title,
					AppDescription: document.Info.Description,
					Version: document.Info.Version,
					InfoEmail: document.Info.Contact?.Email,
					CodeGeneratorVersionInfo: versionInfo
				),

				PackageName: baseNamespace,
				ClassName: className,
				HasDescriptionOrSummary: (summary?.Trim() + description?.Trim()) is { Length: > 0 },
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
			return new SourceEntry(
				Key: $"{baseNamespace}.AddServicesExtensions.cs",
				SourceText: handlebarsFactory.Handlebars.ProcessAddServices(new Templates.AddServicesModel(
					Header: new Templates.PartialHeader(
						AppName: document.Info.Title,
						AppDescription: document.Info.Description,
						Version: document.Info.Version,
						InfoEmail: document.Info.Contact?.Email,
						CodeGeneratorVersionInfo: versionInfo
					),
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
	}
}
