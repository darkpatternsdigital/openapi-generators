using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.Json.Documents;

public record UnableToParseSchema(Location Location) : DiagnosticBase(Location)
{
	public static DiagnosticException.ToDiagnostic Builder() => (Location) => new UnableToParseSchema(Location);
}
