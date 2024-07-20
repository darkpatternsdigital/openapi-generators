using System;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApiCodegen;

namespace DarkPatterns.OpenApi.Transformations
{
	public interface IOpenApiOperationTransformer
	{
		SourceEntry TransformOperation(OpenApiPath path, string method, OpenApiOperation operation, OpenApiTransformDiagnostic diagnostic);
	}

}