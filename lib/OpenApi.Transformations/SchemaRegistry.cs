
using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Transformations;

public class SchemaRegistry(DocumentRegistry documentRegistry) : ISchemaRegistry
{
	public DocumentRegistry DocumentRegistry => documentRegistry;

	// Not using Uri for the key because it doesn't compare fragments, which are necessary for this
	private Dictionary<string, JsonSchema> allSchemas = new();

	public JsonSchema? FindSchema(Uri uri) =>
		allSchemas.TryGetValue(uri.OriginalString, out var result) ? result : null;

	public void EnsureSchemasRegistered(JsonSchema schema)
	{
		string id = schema.Metadata.Id.OriginalString;
		if (allSchemas.ContainsKey(id)) return;
		allSchemas.Add(id, schema);

		foreach (var annotation in schema.Annotations)
			foreach (var referencedSchema in annotation.GetReferencedSchemas())
				EnsureSchemasRegistered(referencedSchema);
	}

	public IEnumerable<JsonSchema> GetSchemas() => allSchemas.Values;
}