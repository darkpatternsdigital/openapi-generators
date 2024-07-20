using System;
using System.Collections.Generic;

namespace DarkPatterns.OpenApi.Transformations.Diagnostics;

public static class ExceptionHandlingExtensions
{
	public static void AddExceptionAsDiagnostic(this List<DiagnosticBase> diagnostics, Exception original, DocumentRegistry documentRegistry, NodeMetadata currentNode)
	{
		switch (original)
		{
			case DocumentException ex:
				diagnostics.Add(ex.Diagnostic);
				break;
			case DiagnosticException ex:
				diagnostics.Add(ex.ConstructDiagnostic(documentRegistry.ResolveLocation(currentNode)));
				break;
			case MultipleDiagnosticException ex:
				diagnostics.AddRange(ex.Diagnostics);
				break;
			default:
				diagnostics.Add(new UnhandledExceptionDiagnostic(original, documentRegistry.ResolveLocation(currentNode)));
				break;
		}
	}
}