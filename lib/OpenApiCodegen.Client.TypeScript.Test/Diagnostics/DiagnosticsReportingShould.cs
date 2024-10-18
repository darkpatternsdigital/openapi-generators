using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen.TestUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;

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

		var settings = new Handlebars.TransformSettings(new SchemaRegistry(registry), "");
		var options = LoadOptions();

		var transformer = new OperationTransformerFactory(settings).Build(docResult.Document, options);

		var result = transformer.GetSources();
		return docResult.Diagnostics.Concat(result.Diagnostics);
	}

}
