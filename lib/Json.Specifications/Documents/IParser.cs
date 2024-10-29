using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DarkPatterns.Json.Documents;

public interface IParser<TResult>
	where TResult : class, IReferenceableDocumentNode
{
	bool CanParse(IDocumentReference documentReference);
	ParseResult<TResult> Parse(IDocumentReference documentReference, SchemaRegistry schemaRegistry);
}

public record ParseResult<TResult>(TResult? Result, IReadOnlyList<Diagnostics.DiagnosticBase> Diagnostics)
	where TResult : class
{
	public bool HasResult => Result != null;
}
