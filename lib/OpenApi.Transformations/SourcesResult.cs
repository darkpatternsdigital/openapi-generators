using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.OpenApiCodegen;

namespace DarkPatterns.OpenApi.Transformations;

public record SourcesResult(IReadOnlyList<SourceEntry> Sources, IReadOnlyList<DiagnosticBase> Diagnostics)
{
	public static SourcesResult Empty { get; } = new([], []);

	internal static SourcesResult Combine(IReadOnlyList<SourcesResult> value)
	{
		return new(
			Sources: value.SelectMany(s => s.Sources).ToArray(),
			Diagnostics: value.SelectMany(s => s.Diagnostics).ToArray()
		);
	}
}
