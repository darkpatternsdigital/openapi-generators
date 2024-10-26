using System;
using System.Collections.Generic;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApi.CSharp;

public record UnableToCreateInlineSchemaDiagnostic(Uri Uri, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Uri.OriginalString];
	internal static DiagnosticException.ToDiagnostic Builder(Uri uri) => (Location) => new UnableToCreateInlineSchemaDiagnostic(uri, Location);
}
