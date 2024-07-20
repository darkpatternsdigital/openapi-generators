using System;
using System.Collections.Generic;
using DarkPatterns.OpenApi.Transformations.Diagnostics;

namespace DarkPatterns.OpenApi.Transformations
{
	public class OpenApiTransformDiagnostic
	{
		public IList<DiagnosticBase> Diagnostics { get; } = new List<DiagnosticBase>();
	}
}