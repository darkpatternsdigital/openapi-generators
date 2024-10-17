using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.OpenApi.Transformations;

public class OperationGroupingSourceTransformer(
	ISchemaRegistry schemaRegistry,
	OpenApiDocument document,
	OperationGroupingSourceTransformer.OperationToGroup operationToGroup,
	IOpenApiOperationControllerTransformer operationControllerTransformer
) : ISourceProvider
{
	public delegate (string groupName, string? groupSummary, string? groupDescription) OperationToGroup(OpenApiOperation operation, OpenApiPath path);

	private readonly OperationGroupingVisitor visitor = new OperationGroupingVisitor(schemaRegistry.DocumentRegistry);

	private Dictionary<string, OperationGroupData> GetGroups(OpenApiTransformDiagnostic diagnostic)
	{
		var result = new Dictionary<string, OperationGroupData>();
		visitor.Visit(document, new OperationGroupingVisitor.Argument((operation, method, path) =>
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
			resultList.Operations.Add((operation, method, path));
		}, diagnostic));
		return result;
	}

	public IEnumerable<string> GetGroupNames(OpenApiTransformDiagnostic diagnostic)
	{
		return GetGroups(diagnostic).Keys;
	}

	public SourcesResult GetSources()
	{
		var diagnostic = new OpenApiTransformDiagnostic();
		var sources = GetGroups(diagnostic).Select(kvp => operationControllerTransformer.TransformController(kvp.Key, kvp.Value, diagnostic)).ToArray();
		return new SourcesResult(
			sources,
			[.. diagnostic.Diagnostics]
		);
	}

	class OperationGroupingVisitor(DocumentRegistry documentRegistry) : Abstractions.OpenApiDocumentVisitor<OperationGroupingVisitor.Argument>
	{
		public record Argument(RegisterOperationEntry RegisterSourceEntry, OpenApiTransformDiagnostic Diagnostic, OpenApiPath? Path = null);
		public delegate void RegisterOperationEntry(OpenApiOperation operation, string method, OpenApiPath path);

		public override void VisitWebhook(OpenApiPath value, string key, Argument argument)
		{
		}

		public override void Visit(OpenApiPath path, Argument argument)
		{
			base.Visit(path, argument with { Path = path });
		}

		public override void Visit(OpenApiOperation operation, string method, Argument argument)
		{
			if (argument is not { Path: OpenApiPath path })
				throw new ArgumentException("Cannot visit operation without a path", nameof(argument));

			try
			{
				argument.RegisterSourceEntry(operation, method, path);
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
