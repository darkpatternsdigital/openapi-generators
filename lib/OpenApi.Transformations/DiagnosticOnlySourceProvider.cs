namespace DarkPatterns.OpenApi.Transformations;

public class DiagnosticOnlySourceProvider(System.Collections.Generic.IReadOnlyList<Json.Diagnostics.DiagnosticBase> diagnostics) : ISourceProvider
{
	public SourcesResult GetSources()
	{
		return new SourcesResult([], diagnostics);
	}
}