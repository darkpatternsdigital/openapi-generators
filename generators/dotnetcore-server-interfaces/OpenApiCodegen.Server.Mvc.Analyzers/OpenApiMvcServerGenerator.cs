using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Linq;

#if NETSTANDARD2_0
#nullable disable warnings
#endif

namespace DarkPatterns.OpenApiCodegen.Server.Mvc
{
	[Generator]
	public sealed class OpenApiMvcServerGenerator : BaseGenerator
	{
		private const string sourceItemGroupKey = "SourceItemGroup";
		private static readonly DiagnosticDescriptor IncludeDependentDll = new DiagnosticDescriptor(id: "DPDAPICTRL001",
																									title: "Include a reference to DarkPatterns.OpenApiCodegen.Json.Extensions",
																									messageFormat: "Include a reference to DarkPatterns.OpenApiCodegen.Json.Extensions",
																									category: "DarkPatterns.OpenApiCodegen.Server.Mvc",
																									DiagnosticSeverity.Warning,
																									isEnabledByDefault: true);

		public OpenApiMvcServerGenerator() : base("DarkPatterns.OpenApi.CSharp.MvcServerGenerator", "DarkPatterns.OpenApiCodegen.Server.Mvc.Base")
		{
		}

		protected override void ReportCompilationDiagnostics(Compilation compilation, CompilerApis apis)
		{
			// check that the users compilation references the expected library
			if (!compilation.ReferencedAssemblyNames.Any(static ai => ai.Name.Equals("DarkPatterns.OpenApiCodegen.Json.Extensions", StringComparison.OrdinalIgnoreCase)))
			{
				apis.ReportDiagnostic(Diagnostic.Create(IncludeDependentDll, Location.None));
			}
		}

		protected override string[] GetFileTypes(AdditionalTextWithOptions additionalText)
		{
			var type = additionalText.ConfigOptions.GetAdditionalFilesMetadata(sourceItemGroupKey);
			if (type == null) return [];

			// sourceGroup or sharedSourceGroup
			return [type];
		}
	}
}
