using System;
using System.Collections.Generic;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;

namespace PrincipleStudios.OpenApi.Transformations
{
	public class OpenApiTransformDiagnostic
	{
		public IList<DiagnosticBase> Diagnostics { get; } = new List<DiagnosticBase>();
		[Obsolete("OpenApiTransformError will be removed")]
		public IList<OpenApiTransformError> Errors { get; } = new List<OpenApiTransformError>();
	}
}