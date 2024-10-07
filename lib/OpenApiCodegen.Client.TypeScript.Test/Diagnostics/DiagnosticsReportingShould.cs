using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApiCodegen.TestUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Diagnostics;

using static OptionsHelpers;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;

public class DiagnosticsReportingShould
{
	[Fact]
	public static void Report_unresolved_external_references()
	{
		var diagnostic = GetDocumentDiagnostics("bad.yaml");
		Assert.True(
			diagnostic.OfType<CouldNotFindTargetNodeDiagnostic>().Any()
		);
	}

	private static IEnumerable<DiagnosticBase> GetDocumentDiagnostics(string name)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = GetOpenApiDocument(name, registry);
		if (docResult.Document == null) return docResult.Diagnostics;
		Assert.NotNull(docResult.Document);
		var document = docResult.Document;

		var options = LoadOptions();

		var transformer = document.BuildTypeScriptOperationSourceProvider(registry, "", options);
		OpenApiTransformDiagnostic diagnostic = new();

		transformer.GetSources(diagnostic).ToArray(); // force all sources to load to get diagnostics
		return docResult.Diagnostics.Concat(diagnostic.Diagnostics);
	}

}
