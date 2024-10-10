using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DarkPatterns.Json.Documents;

public interface IParser<TResult>
	where TResult : class, IReferenceableDocumentNode
{
	bool CanParse(IDocumentReference documentReference);
	ParseResult<TResult> Parse(IDocumentReference documentReference, DocumentRegistry documentRegistry);
}

public record ParseResult<TResult>(TResult? Document, IReadOnlyList<Diagnostics.DiagnosticBase> Diagnostics)
	where TResult : class, IReferenceableDocumentNode
{
	public bool HasDocument => Document != null;
}
