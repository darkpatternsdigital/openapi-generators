using System;
using System.Collections.Generic;

namespace PrincipleStudios.OpenApi.Transformations.Diagnostics;

public record UnhandledExceptionDiagnostic(Exception Exception, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Exception.GetType().FullName, Exception.ToString()];
}