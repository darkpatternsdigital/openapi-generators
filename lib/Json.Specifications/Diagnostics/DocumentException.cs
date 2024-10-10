using System;

namespace DarkPatterns.Json.Diagnostics;

public class DocumentException : Exception
{
	private readonly DiagnosticBase diagnostic;
	public DiagnosticBase Diagnostic => diagnostic;

	public DocumentException(DiagnosticBase diagnostic, string message, Exception inner) : base(message, inner)
	{
		this.diagnostic = diagnostic;
	}
}
