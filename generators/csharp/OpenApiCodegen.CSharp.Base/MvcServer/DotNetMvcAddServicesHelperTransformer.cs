using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

public class DotNetMvcAddServicesHelperTransformer(
	CSharpControllerTransformer schemaTransformer,
	OperationGroupingSourceTransformer operationGrouping) : ISourceProvider
{
	public SourcesResult GetSources()
	{
		OpenApiTransformDiagnostic diagnostic = new();
		return new(
			[schemaTransformer.TransformAddServicesHelper(operationGrouping.GetGroupNames(diagnostic), diagnostic)],
			[.. diagnostic.Diagnostics]
		);
	}
}
