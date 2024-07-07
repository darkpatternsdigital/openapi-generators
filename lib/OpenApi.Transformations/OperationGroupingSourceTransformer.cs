using PrincipleStudios.OpenApi.Transformations.Abstractions;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;
using PrincipleStudios.OpenApi.Transformations.Specifications;
using PrincipleStudios.OpenApiCodegen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrincipleStudios.OpenApi.Transformations;

public class OperationGroupingSourceTransformer : ISourceProvider
{
	public delegate (string groupName, string? groupSummary, string? groupDescription) OperationToGroup(OpenApiOperation operation, OpenApiPath path);
	private readonly OpenApiDocument document;
	private readonly OperationToGroup operationToGroup;
	private readonly IOpenApiOperationControllerTransformer operationControllerTransformer;
	private readonly OperationGroupingVisitor visitor;
	private readonly ISchemaRegistry schemaRegistry;

	public OperationGroupingSourceTransformer(DocumentRegistry documentRegistry, ISchemaRegistry schemaRegistry, OpenApiDocument document, OperationToGroup operationToGroup, IOpenApiOperationControllerTransformer operationControllerTransformer)
	{
		this.visitor = new OperationGroupingVisitor(documentRegistry);
		this.schemaRegistry = schemaRegistry;
		this.document = document;
		this.operationToGroup = operationToGroup;
		this.operationControllerTransformer = operationControllerTransformer;
	}

	private Dictionary<string, OperationGroupData> GetGroups(OpenApiTransformDiagnostic diagnostic)
	{
		var result = new Dictionary<string, OperationGroupData>();
		visitor.Visit(document, new OperationGroupingVisitor.Argument((operation, path) =>
		{
			var (group, summary, description) = operationToGroup(operation, path);
			group = operationControllerTransformer.SanitizeGroupName(group);
			var resultList = result[group] = result.TryGetValue(group, out var list) ? list : new()
			{
				Summary = summary,
				Description = description,
			};
			if (resultList.Summary != summary)
				resultList.Summary = null;
			if (resultList.Description != description)
				resultList.Description = null;
			foreach (var referencedSchema in operation.GetNestedNodes(recursive: true).OfType<JsonSchema>())
				schemaRegistry.EnsureSchemasRegistered(referencedSchema);
			resultList.Operations.Add((operation, path));
		}, diagnostic));
		return result;
	}

	public IEnumerable<string> GetGroupNames(OpenApiTransformDiagnostic diagnostic)
	{
		return GetGroups(diagnostic).Keys;
	}

	public IEnumerable<SourceEntry> GetSources(OpenApiTransformDiagnostic diagnostic)
	{
		return GetGroups(diagnostic).Select(kvp => operationControllerTransformer.TransformController(kvp.Key, kvp.Value, diagnostic)).ToArray();
	}

	class OperationGroupingVisitor(DocumentRegistry documentRegistry) : Abstractions.OpenApiDocumentVisitor<OperationGroupingVisitor.Argument>
	{
		public record Argument(RegisterOperationEntry RegisterSourceEntry, OpenApiTransformDiagnostic Diagnostic, OpenApiPath? Path = null);
		public delegate void RegisterOperationEntry(OpenApiOperation operation, OpenApiPath path);

		public override void Visit(OpenApiPath path, Argument argument)
		{
			base.Visit(path, argument with { Path = path });
		}

		public override void Visit(OpenApiOperation operation, Argument argument)
		{
			if (argument is not { Path: OpenApiPath path })
				throw new ArgumentException("Cannot visit operation without a path", nameof(argument));

			try
			{
				argument.RegisterSourceEntry(operation, path);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				argument.Diagnostic.Diagnostics.Add(new UnableToGroupOperation(documentRegistry.ResolveLocation(operation.Id), ex.Message));
			}
		}
	}
}

public record UnableToGroupOperation(Location Location, string Message) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Message];
}
