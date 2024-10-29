using Json.More;
using Json.Pointer;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.OpenApi.Abstractions;

namespace DarkPatterns.OpenApi.Transformations.Specifications.OpenApi3_1;

public class OpenApi3_1DocumentFactory
{
	private readonly SchemaRegistry schemaRegistry;

	public List<DiagnosticBase> Diagnostics { get; }

	public OpenApi3_1DocumentFactory(SchemaRegistry schemaRegistry, IEnumerable<DiagnosticBase> initialDiagnostics)
	{
		this.schemaRegistry = schemaRegistry;
		this.Diagnostics = initialDiagnostics.ToList();
	}

	internal OpenApiDocument? ConstructDocument(Uri baseUri, JsonNode jsonNode)
	{
		throw new NotImplementedException();
	}
}