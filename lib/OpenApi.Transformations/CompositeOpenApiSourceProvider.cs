using System.Collections.Generic;
using System.Linq;

namespace DarkPatterns.OpenApi.Transformations;

public class CompositeOpenApiSourceProvider(IReadOnlyList<ISourceProvider> sourceProviders) : ISourceProvider
{
	public SourcesResult GetSources()
	{
		return SourcesResult.Combine([
			.. sourceProviders.Select(t => t.GetSources()),
		]);
	}
}
