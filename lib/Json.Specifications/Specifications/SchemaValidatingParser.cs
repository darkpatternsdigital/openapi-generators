using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;
using System;
using System.Collections.Generic;

namespace DarkPatterns.Json.Specifications;

public abstract class SchemaValidatingParser<TInterface> : IParser<TInterface>
	where TInterface : class, IReferenceableDocumentNode
{
	private readonly Func<SchemaRegistry, JsonSchema> schemaResolver;

	protected SchemaValidatingParser(Func<SchemaRegistry, JsonSchema> schemaResolver)
	{
		this.schemaResolver = schemaResolver;
	}

	public abstract bool CanParse(IDocumentReference documentReference);

	public ParseResult<TInterface> Parse(IDocumentReference documentReference, SchemaRegistry schemaRegistry)
	{
		if (!CanParse(documentReference)) throw new ArgumentException(Errors.ParserCannotHandleDocument, nameof(documentReference));

		var schema = schemaResolver(schemaRegistry);
		var evaluationResults = schema.Evaluate(ResolvableNode.FromRoot(schemaRegistry.DocumentRegistry, documentReference), new EvaluationContext(schemaRegistry.DocumentRegistry));
		return Construct(documentReference, evaluationResults, schemaRegistry);
	}

	protected abstract ParseResult<TInterface> Construct(IDocumentReference documentReference, IEnumerable<DiagnosticBase> diagnostics, SchemaRegistry schemaRegistry);
}
