using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DarkPatterns.OpenApi.Transformations.Diagnostics;

namespace DarkPatterns.OpenApi.Transformations.Specifications;

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
		switch (value)
		{
			case true: return Enumerable.Empty<DiagnosticBase>();
			case false: return [new FalseJsonSchemasFailDiagnostic(Metadata.Id, evaluationContext.DocumentRegistry.ResolveLocation(nodeMetadata))];
			default:

				return from keyword in Annotations
					   let keywordResults = keyword.Evaluate(nodeMetadata, this, evaluationContext)
					   from result in keywordResults
					   select result;
		}
	}

	public void FixupInPlace(JsonSchemaParserOptions options)
	{
		if (isFinalized) return;
		var stack = new Stack<IJsonSchemaFixupAnnotation>(Annotations.OfType<IJsonSchemaFixupAnnotation>());
		var fixupRan = new HashSet<IJsonSchemaFixupAnnotation>();
		var modifier = new JsonSchemaModifier(this,
			onReplace: (items) => stack = new(items.OfType<IJsonSchemaFixupAnnotation>()),
			onAdd: (items) =>
			{
				foreach (var item in items.OfType<IJsonSchemaFixupAnnotation>()) stack.Push(item);
			},
			onStop: () => stack.Clear()
		);
		while (stack.Count > 0)
		{
			var next = stack.Pop();
			if (fixupRan.Add(next))
				next.FixupInPlace(this, modifier, options);
		}
		isFinalized = true;
	}

	class JsonSchemaModifier(JsonSchema original, Action<IEnumerable<IJsonSchemaAnnotation>> onReplace, Action<IEnumerable<IJsonSchemaAnnotation>> onAdd, Action onStop) : IJsonSchemaModifier
	{
		public void AddAnnotations(IEnumerable<IJsonSchemaAnnotation> annotations)
		{
			onAdd(annotations);
			original.keywords.AddRange(annotations);
		}

		public void ReplaceAnnotations(IEnumerable<IJsonSchemaAnnotation> annotations, bool needsFixup)
		{
			if (needsFixup) onReplace(annotations);
			else onStop();
			original.keywords.Clear();
			original.keywords.AddRange(annotations);
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
}

public record EvaluationContext(DocumentRegistry DocumentRegistry);

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

	IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchema context, EvaluationContext evaluationContext);
}

public interface IJsonSchemaFixupAnnotation : IJsonSchemaAnnotation
{
	void FixupInPlace(JsonSchema schema, IJsonSchemaModifier modifier, JsonSchemaParserOptions options);
}