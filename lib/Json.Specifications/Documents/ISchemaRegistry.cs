using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications;
using System;
using System.Collections.Generic;

namespace DarkPatterns.Json.Documents;

public interface ISchemaRegistry
{
	DocumentRegistry DocumentRegistry { get; }
	JsonSchema? FindSchema(Uri uri);
	IEnumerable<JsonSchema> GetSchemas();
	void EnsureSchemaRegistered(JsonSchema schema, IReadOnlyList<DiagnosticBase>? diagnostics = null);
	JsonSchema? ResolveSchema(NodeMetadata nodeMetadata, IJsonSchemaDialect dialect);
}
