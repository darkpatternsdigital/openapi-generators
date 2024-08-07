﻿using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using System;
using System.Collections.Generic;

namespace DarkPatterns.OpenApi.Transformations.Specifications;

public abstract class SchemaValidatingParser<TInterface> : IParser<TInterface>
	where TInterface : class, IReferenceableDocumentNode
{
	private readonly Func<DocumentRegistry, JsonSchema> schemaResolver;

	protected SchemaValidatingParser(Func<DocumentRegistry, JsonSchema> schemaResolver)
	{
		this.schemaResolver = schemaResolver;
	}

	public abstract bool CanParse(IDocumentReference documentReference);

	public ParseResult<TInterface> Parse(IDocumentReference documentReference, DocumentRegistry documentRegistry)
	{
		if (!CanParse(documentReference)) throw new ArgumentException(Errors.ParserCannotHandleDocument, nameof(documentReference));

		var schema = schemaResolver(documentRegistry);
		var evaluationResults = schema.Evaluate(ResolvableNode.FromRoot(documentRegistry, documentReference), new EvaluationContext(documentRegistry));
		return Construct(documentReference, evaluationResults, documentRegistry);
	}

	protected abstract ParseResult<TInterface> Construct(IDocumentReference documentReference, IEnumerable<DiagnosticBase> diagnostics, DocumentRegistry documentRegistry);
}
