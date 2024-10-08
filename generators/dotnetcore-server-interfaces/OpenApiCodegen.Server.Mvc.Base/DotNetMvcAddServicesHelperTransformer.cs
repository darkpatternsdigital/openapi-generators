﻿using DarkPatterns.OpenApi.Transformations;
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

		public IEnumerable<SourceEntry> GetSources(OpenApiTransformDiagnostic diagnostic)
		{
			yield return schemaTransformer.TransformAddServicesHelper(operationGrouping.GetGroupNames(diagnostic), diagnostic);
		}
	}
}
