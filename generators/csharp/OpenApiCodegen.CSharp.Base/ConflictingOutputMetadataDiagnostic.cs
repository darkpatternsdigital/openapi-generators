using System.Collections.Generic;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// The same file is listed more than once as the same item type with different metadata, so MSBuild
/// joins the values with <c>;</c>.
/// </summary>
public record ConflictingOutputMetadataDiagnostic(string ItemType, string MetadataName, string Value, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [ItemType, MetadataName, Value];
}
