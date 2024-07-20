using System.Collections.Generic;
using DarkPatterns.OpenApi.Transformations.Diagnostics;

namespace DarkPatterns.OpenApi.Transformations.Specifications.Keywords;

public record UnableToParseKeyword(string Keyword, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Keyword];
}
