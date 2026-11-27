using System.Collections.Generic;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// An options file sets <c>jsonUnions</c>, but the project doesn't reference System.Text.Json 11.
/// </summary>
public record JsonUnionsRequireSystemTextJson11Diagnostic(Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [];
}

/// <summary>
/// An options file sets <c>jsonUnions</c>, but <c>UnionAttribute</c> and <c>IUnion</c> aren't available.
/// </summary>
public record JsonUnionsRequireUnionTypesDiagnostic(Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [];
}
