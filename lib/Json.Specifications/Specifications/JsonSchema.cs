using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications;

public interface IJsonDocumentNode
{
	NodeMetadata Metadata { get; }
	IEnumerable<IJsonDocumentNode> GetNestedNodes();
}

public sealed class JsonSchema : IJsonDocumentNode
{
	private bool isFinalized;
	private bool? value;
	private readonly List<IJsonSchemaAnnotation> keywords;

	public JsonSchema(NodeMetadata metadata, bool value)
	{
		this.Metadata = metadata;
		this.value = value;
		this.keywords = new List<IJsonSchemaAnnotation>();
	}

	public JsonSchema(NodeMetadata metadata, IEnumerable<IJsonSchemaAnnotation> keywords)
	{
		this.value = null;
		this.Metadata = metadata;
		this.keywords = keywords.ToList();
	}

	public NodeMetadata Metadata { get; private set; }
	public bool? BoolValue => value;
	public bool IsFixupComplete => isFinalized;
	IEnumerable<IJsonDocumentNode> IJsonDocumentNode.GetNestedNodes() => GetNestedSchemas();

	public IReadOnlyCollection<IJsonSchemaAnnotation> Annotations => keywords.AsReadOnly();

	public IEnumerable<JsonSchema> GetNestedSchemas() =>
		Annotations
			.SelectMany(a => a.GetReferencedSchemas());

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, EvaluationContext evaluationContext)
	{
		var context = this.ResolveSchemaInfo();
		switch (value)
		{
			case true: return Enumerable.Empty<DiagnosticBase>();
			case false: return [new FalseJsonSchemasFailDiagnostic(Metadata.Id, evaluationContext.DocumentRegistry.ResolveLocation(nodeMetadata))];
			default:

				return from keyword in Annotations
					   let keywordResults = keyword.Evaluate(nodeMetadata, context, evaluationContext)
					   from result in keywordResults
					   select result;
		}
	}

	internal IEnumerable<DiagnosticBase> FixupInPlace(JsonSchemaParserOptions options)
	{
		if (isFinalized) return [];
		var stack = new Stack<IJsonSchemaFixupAnnotation>(Annotations.OfType<IJsonSchemaFixupAnnotation>());
		var fixupRan = new HashSet<IJsonSchemaFixupAnnotation>();
		var resultDiagnostics = new List<DiagnosticBase>();
		var modifier = new JsonSchemaModifier(this,
			onReplace: (items) => stack = new(items.OfType<IJsonSchemaFixupAnnotation>()),
			onAdd: (items) =>
			{
				foreach (var item in items.OfType<IJsonSchemaFixupAnnotation>()) stack.Push(item);
			},
			onStop: () => stack.Clear(),
			resultDiagnostics.AddRange
		);
		while (stack.Count > 0)
		{
			var next = stack.Pop();
			if (fixupRan.Add(next))
				next.FixupInPlace(this, modifier, options);
		}
		isFinalized = true;
		return [.. resultDiagnostics];
	}

	public IReadOnlyList<IJsonSchemaAnnotation> GetAllAnnotations()
	{
		var set = new List<IJsonSchemaAnnotation>(Annotations);
		var stack = new Stack<IJsonSchemaAnnotation>(Annotations);
		while (stack.Count > 0)
		{
			var annotation = stack.Pop();
			var dynamicAnnotations = annotation.GetDynamicAnnotations();
			set.AddRange(dynamicAnnotations);
			foreach (var entry in dynamicAnnotations)
				stack.Push(entry);
		}
		return [.. set];
	}

	class JsonSchemaModifier(
		JsonSchema original,
		Action<IEnumerable<IJsonSchemaAnnotation>> onReplace,
		Action<IEnumerable<IJsonSchemaAnnotation>> onAdd,
		Action onStop,
		Action<IEnumerable<DiagnosticBase>> addDiagnostics) : IJsonSchemaModifier
	{
		public void AddAnnotations(IEnumerable<IJsonSchemaAnnotation> annotations)
		{
			onAdd(annotations);
			original.keywords.AddRange(annotations);
		}

		public void AddDiagnostics(IEnumerable<DiagnosticBase> diagnostics)
		{
			addDiagnostics(diagnostics);
		}

		public void ReplaceAnnotations(IEnumerable<IJsonSchemaAnnotation> annotations, bool needsFixup)
		{
			var newRange = annotations.ToArray();
			if (needsFixup) onReplace(newRange);
			else onStop();
			original.keywords.Clear();
			original.keywords.AddRange(newRange);
		}

		public void SetBooleanSchemaValue(bool value)
		{
			onStop();
			original.keywords.Clear();
		}

		public void UpdateId(NodeMetadata metadata)
		{
			original.Metadata = metadata;
		}
	}
}

public interface IJsonSchemaModifier
{
	void UpdateId(NodeMetadata metadata);
	void AddAnnotations(IEnumerable<IJsonSchemaAnnotation> annotations);
	void ReplaceAnnotations(IEnumerable<IJsonSchemaAnnotation> annotations, bool needsFixup);
	void SetBooleanSchemaValue(bool value);
	void AddDiagnostics(IEnumerable<DiagnosticBase> diagnostics);
}

public record EvaluationContext(DocumentRegistry DocumentRegistry, IReadOnlyList<JsonSchema> SchemaStack)
{
	public EvaluationContext(DocumentRegistry DocumentRegistry) : this(DocumentRegistry, [])
	{
	}

	public EvaluationContext WithSchema(JsonSchema context)
	{
		return this with { SchemaStack = [.. SchemaStack, context] };
	}

	public bool TryPopSchema([NotNullWhen(true)] out JsonSchema? schema, [NotNullWhen(true)] out EvaluationContext? innerContext)
	{
		if (SchemaStack.Count == 0)
		{
			schema = null;
			innerContext = null;
			return false;
		}

		schema = SchemaStack[0];
		innerContext = this with { SchemaStack = SchemaStack.Skip(1).ToArray() };
		return true;
	}

}

public record FalseJsonSchemasFailDiagnostic(Uri OriginalSchema, Location Location) : DiagnosticBase(Location);

public interface IJsonSchemaKeyword
{
	DiagnosableResult<IJsonSchemaAnnotation> ParseAnnotation(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options);
}

public delegate DiagnosableResult<IJsonSchemaAnnotation> ParseAnnotation(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options);

public record JsonSchemaKeyword(ParseAnnotation ParseAnnotation) : IJsonSchemaKeyword
{
	DiagnosableResult<IJsonSchemaAnnotation> IJsonSchemaKeyword.ParseAnnotation(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		return ParseAnnotation(keyword, nodeInfo, options);
	}
}

public interface IJsonSchemaAnnotation
{
	string Keyword { get; }

	IEnumerable<JsonSchema> GetReferencedSchemas();

	IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext);

	IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations();
}

public interface IJsonSchemaRefAnnotation : IJsonSchemaAnnotation
{
	JsonSchema? ReferencedSchema { get; }
}

public interface IJsonSchemaFixupAnnotation : IJsonSchemaAnnotation
{
	void FixupInPlace(JsonSchema schema, IJsonSchemaModifier modifier, JsonSchemaParserOptions options);
}