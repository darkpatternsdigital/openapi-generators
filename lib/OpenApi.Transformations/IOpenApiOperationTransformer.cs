using System;
using PrincipleStudios.OpenApi.Transformations.Abstractions;
using PrincipleStudios.OpenApiCodegen;

namespace PrincipleStudios.OpenApi.Transformations
{
	public interface IOpenApiOperationTransformer
	{
		SourceEntry TransformOperation(OpenApiPath path, string method, OpenApiOperation operation, OpenApiTransformDiagnostic diagnostic);
	}

}