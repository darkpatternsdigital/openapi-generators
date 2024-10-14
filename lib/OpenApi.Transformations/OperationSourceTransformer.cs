using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using System.IO;
using System.Linq;

namespace DarkPatterns.OpenApi.Transformations;

public record OperationDetail(OpenApiPath Path, string Method, OpenApiOperation Operation);
public class OperationSourceTransformer(DocumentRegistry documentRegistry, OpenApiDocument document, IOpenApiOperationTransformer operationTransformer) : ISourceProvider
{
	private readonly OpenApiDocument document = document;
	private readonly IOpenApiOperationTransformer operationTransformer = operationTransformer;
	private static readonly OperationVisitor visitor = new();
	public SourcesResult GetSources()
	{
		var diagnostic = new OpenApiTransformDiagnostic();
		var sources = from opDetail in GetOperations(diagnostic)
					  select operationTransformer.TransformOperation(opDetail.Path, opDetail.Method, opDetail.Operation, diagnostic);
		return new SourcesResult(
			sources.ToArray(),
			[.. diagnostic.Diagnostics]
		);
	}

	public List<OperationDetail> GetOperations(OpenApiTransformDiagnostic diagnostic)
	{
		var operations = new List<OperationDetail>();
		visitor.Visit(document, new OperationVisitor.Argument(documentRegistry, (path, method, operation) =>
		{
			operations.Add(new(path, method, operation));
		}, diagnostic));
		return operations;
	}

	class OperationVisitor : OpenApiDocumentVisitor<OperationVisitor.Argument>
	{
		public record Argument(DocumentRegistry DocumentRegistry, RegisterOperationEntry RegisterSourceEntry, OpenApiTransformDiagnostic Diagnostic, OpenApiPath? Path = null);
		public delegate void RegisterOperationEntry(OpenApiPath path, string method, OpenApiOperation operation);

		public override void Visit(OpenApiPath path, Argument argument)
		{
			base.Visit(path, argument with { Path = path });
		}

		public override void Visit(OpenApiOperation operation, string method, Argument argument)
		{
			if (argument.Path is null)
				throw new ArgumentException("Cannot visit operation without path. Did you visit a full document?", nameof(argument));

			try
			{
				argument.RegisterSourceEntry(argument.Path, method, operation);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				argument.Diagnostic.Diagnostics.Add(new UnhandledExceptionDiagnostic(ex, argument.DocumentRegistry.ResolveLocation(operation.Metadata)));
			}
		}

		public override void Visit(OpenApiInfo ignored, Argument argument) { }
		public override void Visit(OpenApiSecurityRequirement ignored, Argument argument) { }
	}
}
