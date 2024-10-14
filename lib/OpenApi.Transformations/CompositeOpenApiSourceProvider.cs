using System.Linq;

namespace DarkPatterns.OpenApi.Transformations;

public class CompositeOpenApiSourceProvider : ISourceProvider
{
	private readonly ISourceProvider[] sourceProviders;

	public CompositeOpenApiSourceProvider(params ISourceProvider[] sourceProviders)
	{
		this.sourceProviders = sourceProviders;
	}

	public SourcesResult GetSources()
	{
		return SourcesResult.Combine([
			.. sourceProviders.Select(t => t.GetSources()),
		]);
	}
}
