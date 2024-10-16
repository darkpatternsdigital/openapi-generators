using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Diagnostics;

public static class ExceptionHandlingExtensions
{
	public static IEnumerable<DiagnosticBase> ToDiagnostics(this Exception original, DocumentRegistry documentRegistry, NodeMetadata currentNode)
	{
		switch (original)
		{
			case DocumentException ex:
				return [ex.Diagnostic];
			case DiagnosticException ex:
				return [ex.ConstructDiagnostic(documentRegistry.ResolveLocation(currentNode))];
			case MultipleDiagnosticException ex:
				return ex.Diagnostics;
			default:
				return [new UnhandledExceptionDiagnostic(original, documentRegistry.ResolveLocation(currentNode))];
		}
	}
}