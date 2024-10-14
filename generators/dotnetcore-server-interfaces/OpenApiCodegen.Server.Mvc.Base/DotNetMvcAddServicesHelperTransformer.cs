using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen;
using System.Collections.Generic;

namespace DarkPatterns.OpenApi.CSharp
{
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
}
