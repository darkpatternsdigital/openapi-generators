using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

internal class GitIgnoreSourceProvider : ISourceProvider
{
	public SourcesResult GetSources()
	{
		return new SourcesResult(
			Diagnostics: [],
			Sources: [new(Key : ".gitignore", SourceText : "*")]
		);
	}
}