using System.Collections.Generic;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.Json.Specifications.Keywords;

public record UnableToParseKeyword(string Keyword, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Keyword];
}
