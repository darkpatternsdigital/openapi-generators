using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

public class AllOperationsBarrelTransformer : ISourceProvider
{
	private readonly OperationSourceTransformer operationsSourceProvider;
	private TypeScriptOperationTransformer operationTransformer;

	public AllOperationsBarrelTransformer(OperationSourceTransformer operationsSourceProvider, TypeScriptOperationTransformer operationTransformer)
	{
		this.operationsSourceProvider = operationsSourceProvider;
		this.operationTransformer = operationTransformer;
	}

	public SourcesResult GetSources()
	{
		OpenApiTransformDiagnostic diagnostic = new();
		return new([operationTransformer.TransformBarrelFileHelper(operationsSourceProvider.GetOperations(diagnostic), diagnostic)], [.. diagnostic.Diagnostics]);
	}
}
