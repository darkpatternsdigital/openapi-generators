using System.Collections.Generic;
using DarkPatterns.OpenApiCodegen;

namespace DarkPatterns.OpenApi.Transformations
{
	public interface ISourceProvider
	{
		IEnumerable<SourceEntry> GetSources(OpenApiTransformDiagnostic diagnostic);
	}
}