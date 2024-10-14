using System;

namespace DarkPatterns.OpenApi.Transformations;

public class SafeSourceProvider(ISourceProvider original, Func<Exception, SourcesResult> exceptionHandler) : ISourceProvider
{
	public SourcesResult GetSources()
	{
		try
		{
			return original.GetSources();
		}
		catch (Exception ex)
		{
			return exceptionHandler(ex);
		}
	}
}
