using DarkPatterns.OpenApi.Transformations;
using System.Collections.Generic;

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

	public IEnumerable<SourceEntry> GetSources(OpenApiTransformDiagnostic diagnostic)
	{
		yield return operationTransformer.TransformBarrelFileHelper(operationsSourceProvider.GetOperations(diagnostic), diagnostic);
	}
}
