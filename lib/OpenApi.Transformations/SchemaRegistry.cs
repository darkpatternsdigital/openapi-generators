
using System;
using System.Collections.Generic;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations;

public interface ISchemaRegistry
{
	IEnumerable<JsonSchema> GetSchemas();
	void EnsureSchemasRegistered(JsonSchema schema);
}

public class SchemaRegistry : ISchemaRegistry
{
	// Not using Uri for the key because it doesn't compare fragments, which are necessary for this
	private Dictionary<string, JsonSchema> allSchemas = new();

	public void EnsureSchemasRegistered(JsonSchema schema)
	{
		string id = schema.Metadata.Id.OriginalString;
		if (allSchemas.ContainsKey(id)) return;
		allSchemas.Add(id, schema);

		if (schema is not AnnotatedJsonSchema annotated) return;
		foreach (var annotation in annotated.Annotations)
			foreach (var referencedSchema in annotation.GetReferencedSchemas())
				EnsureSchemasRegistered(referencedSchema);
	}

	public IEnumerable<JsonSchema> GetSchemas() => allSchemas.Values;
}