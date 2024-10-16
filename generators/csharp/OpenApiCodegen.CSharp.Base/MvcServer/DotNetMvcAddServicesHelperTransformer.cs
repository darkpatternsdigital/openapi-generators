using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

public class DotNetMvcAddServicesHelperTransformer : ISourceProvider
{
	private CSharpControllerTransformer schemaTransformer;
	private readonly OperationGroupingSourceTransformer operationGrouping;

	public DotNetMvcAddServicesHelperTransformer(CSharpControllerTransformer schemaTransformer, OperationGroupingSourceTransformer operationGrouping)
	{
		this.schemaTransformer = schemaTransformer;
		this.operationGrouping = operationGrouping;
	}

	public SourcesResult GetSources()
	{
		OpenApiTransformDiagnostic diagnostic = new();
		return new([schemaTransformer.TransformAddServicesHelper(operationGrouping.GetGroupNames(diagnostic), diagnostic)], [.. diagnostic.Diagnostics]);
	}
}
