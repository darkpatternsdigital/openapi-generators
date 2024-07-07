using System;
using System.Collections.Generic;
using System.Linq;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;

namespace PrincipleStudios.OpenApi.Transformations.Specifications;

public interface IJsonDocumentNode
{
	NodeMetadata Metadata { get; }
	IEnumerable<IJsonDocumentNode> GetNestedNodes();
}

public abstract class JsonSchema : IJsonDocumentNode
{
	public abstract NodeMetadata Metadata { get; }
	public virtual IReadOnlyCollection<IJsonSchemaAnnotation> Annotations => Array.Empty<IJsonSchemaAnnotation>();
	public virtual bool? BoolValue => null;
	IEnumerable<IJsonDocumentNode> IJsonDocumentNode.GetNestedNodes() => GetNestedSchemas();
	public abstract IEnumerable<JsonSchema> GetNestedSchemas();

	public abstract IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, EvaluationContext evaluationContext);
}

public record EvaluationContext(DocumentRegistry DocumentRegistry);

public class JsonSchemaBool(NodeMetadata metadata, bool value) : JsonSchema
{
	public override NodeMetadata Metadata => metadata;

	public override bool? BoolValue => value;

	public override IEnumerable<JsonSchema> GetNestedSchemas() => Enumerable.Empty<JsonSchema>();

	public override IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, EvaluationContext evaluationContext)
	{
		return value
			? Enumerable.Empty<DiagnosticBase>()
			: [new FalseJsonSchemasFailDiagnostic(metadata.Id, evaluationContext.DocumentRegistry.ResolveLocation(nodeMetadata))];
	}
}

public record FalseJsonSchemasFailDiagnostic(Uri OriginalSchema, Location Location) : DiagnosticBase(Location);

public class AnnotatedJsonSchema : JsonSchema
{
	private readonly List<IJsonSchemaAnnotation> keywords;

	public AnnotatedJsonSchema(NodeMetadata metadata, IEnumerable<IJsonSchemaAnnotation> keywords)
	{
		this.Metadata = metadata;
		this.keywords = keywords.ToList();
	}

	public override NodeMetadata Metadata { get; }

	public override IReadOnlyCollection<IJsonSchemaAnnotation> Annotations => keywords.AsReadOnly();

	public override IEnumerable<JsonSchema> GetNestedSchemas() =>
		Annotations
			.SelectMany(a => a.GetReferencedSchemas());

	public override IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, EvaluationContext evaluationContext)
	{
		return (from keyword in Annotations
				let keywordResults = keyword.Evaluate(nodeMetadata, this, evaluationContext)
				from result in keywordResults
				select result);
	}
}

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

	IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, AnnotatedJsonSchema context, EvaluationContext evaluationContext);
}
