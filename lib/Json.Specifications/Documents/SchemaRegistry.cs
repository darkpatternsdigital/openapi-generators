using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkPatterns.Json.Documents;

public class SchemaRegistry(DocumentRegistry documentRegistry) : ISchemaRegistry
{
	// Not using Uri for the key because it doesn't compare fragments, which are necessary for this
	private readonly Dictionary<string, ParseResult<JsonSchema>> allSchemas = [];

	public DocumentRegistry DocumentRegistry => documentRegistry;

	public JsonSchema? FindSchema(Uri uri) =>
		allSchemas.TryGetValue(uri.OriginalString, out var result) ? result.Result : null;

	private void AddDiagnostics(JsonSchema schema, IEnumerable<DiagnosticBase> diagnostics)
	{
		var id = schema.Metadata.Id.OriginalString;
		if (allSchemas.TryGetValue(id, out var parseResult))
		{
			allSchemas[id] = parseResult with { Diagnostics = [.. parseResult.Diagnostics, .. diagnostics] };
		}
		else
		{
			allSchemas[id] = new ParseResult<JsonSchema>(schema, [.. diagnostics]);
		}
	}

	public void EnsureSchemaRegistered(JsonSchema schema, IReadOnlyList<DiagnosticBase>? diagnostics = null)
	{
		string id = schema.Metadata.Id.OriginalString;
		if (allSchemas.ContainsKey(id)) return;
		allSchemas.Add(id, new ParseResult<JsonSchema>(schema, diagnostics ?? []));

		foreach (var annotation in schema.Annotations)
			foreach (var referencedSchema in annotation.GetReferencedSchemas())
				EnsureSchemaRegistered(referencedSchema);
	}

	public IEnumerable<JsonSchema> GetSchemas() => from parsed in allSchemas.Values
												   let result = parsed.Result
												   where result != null
												   select result;

	public JsonSchema? ResolveSchema(NodeMetadata nodeMetadata, IJsonSchemaDialect dialect)
	{
		if (FindSchema(nodeMetadata.Id) is { } schema) return schema;
		if (!DocumentRegistry.TryGetNode<JsonSchema>(nodeMetadata.Id, out var result))
		{
			var deserialized = JsonSchemaParser.Deserialize(
				DocumentRegistry.ResolveMetadataNode(nodeMetadata),
				new JsonSchemaParserOptions(this, dialect)
			).Fold<JsonSchema?>(s => s, _ => null);
			if (deserialized != null)
				DocumentRegistry.Register(deserialized);
			result = deserialized;
		}
		return result;
	}

	public IEnumerable<DiagnosticBase> RecursivelyFixupAll()
	{
		var finalResult = new List<DiagnosticBase>();
		var nodes = DocumentRegistry.GetAllRegisteredNodes().ToArray();
		var processed = new HashSet<string>();
		var stack = new Stack<IJsonDocumentNode>(nodes);
		while (stack.Count > 0)
		{
			var nextNode = stack.Pop();
			if (nextNode is JsonSchema schema && schema.IsFixupComplete == false)
			{
				var doc = documentRegistry.ResolveDocumentFromMetadata(schema.Metadata);
				var options = new JsonSchemaParserOptions(this, doc.Dialect);
				var newDiagnostics = schema.FixupInPlace(options);
				AddDiagnostics(schema, newDiagnostics);
				finalResult.AddRange(newDiagnostics);
			}
			if (processed.Contains(nextNode.Metadata.Id.OriginalString))
				continue;
			processed.Add(nextNode.Metadata.Id.OriginalString);
			DocumentRegistry.Register(nextNode);
			foreach (var upcoming in nextNode.GetNestedNodes(false))
				stack.Push(upcoming);
		}
		return finalResult;
	}

}