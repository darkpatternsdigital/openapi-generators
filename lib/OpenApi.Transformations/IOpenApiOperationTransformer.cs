using System;
using Microsoft.OpenApi.Models;
using PrincipleStudios.OpenApiCodegen;

namespace PrincipleStudios.OpenApi.Transformations
{
	[Obsolete("TODO: Refactor")]
	public interface IOpenApiOperationTransformer
	{
		SourceEntry TransformOperation(OpenApiOperation operation, OpenApiContext context, OpenApiTransformDiagnostic diagnostic);
	}

}