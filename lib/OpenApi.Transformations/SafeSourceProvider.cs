using System;
using DarkPatterns.Json.Diagnostics;
using System.Xml.Linq;
using YamlDotNet.Serialization;
using DarkPatterns.Json.Documents;

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
	public static Func<Exception, SourcesResult> DefaultExceptionHandler(DocumentRegistry documentRegistry, NodeMetadata metadata)
	{
		return (ex) =>
			new(
				[],
				[.. ex.ToDiagnostics(documentRegistry, metadata)]
			);
	}
}
