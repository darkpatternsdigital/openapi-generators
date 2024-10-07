using System;
using System.Collections.Generic;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApi.Transformations
{
	public class OpenApiTransformDiagnostic
	{
		public IList<DiagnosticBase> Diagnostics { get; } = new List<DiagnosticBase>();
	}
}