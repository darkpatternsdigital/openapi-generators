
using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Transformations;

public interface ISchemaRegistry
{
	DocumentRegistry DocumentRegistry { get; }
	JsonSchema? FindSchema(Uri uri);
	IEnumerable<JsonSchema> GetSchemas();
	void EnsureSchemasRegistered(JsonSchema schema);
}
