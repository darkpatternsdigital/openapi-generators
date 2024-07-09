using Microsoft.Win32;
using PrincipleStudios.OpenApi.CSharp;
using PrincipleStudios.OpenApi.Transformations;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords;
using PrincipleStudios.OpenApiCodegen.TestUtils;
using System.Linq;
using Xunit;
using static PrincipleStudios.OpenApiCodegen.Server.Mvc.OptionsHelpers;
using static PrincipleStudios.OpenApiCodegen.TestUtils.DocumentHelpers;

namespace PrincipleStudios.OpenApiCodegen.Server.Mvc
{
	public class ComprehensiveTransformsShould
	{
		/// <summary>
		/// These tests should match the same set of yaml that is in the TestApp. If the TestApp
		/// builds, these should, too. However, this contributes to code coverage.
		/// </summary>
		[Trait("Category", "RepeatMsBuild")]
		[InlineData("all-of.yaml")]
		[InlineData("enum.yaml")]
		[InlineData("controller-extension.yaml")]
		[InlineData("regex-escape.yaml")]
		[InlineData("validation-min-max.yaml")]
		[InlineData("headers.yaml")]
		[InlineData("oauth.yaml")]
		[InlineData("form.yaml")]
		[InlineData("one-of.yaml")]
		[InlineData("nullable-vs-optional.yaml")]
		[InlineData("nullable-vs-optional-legacy.yaml")]
		[InlineData("annotations.yaml")]
		[InlineData("response-ref.yaml")]
		[Theory]
		public void Compile_api_documents_included_in_the_TestApp(string name)
		{
			DynamicCompilation.GetGeneratedLibrary(name);
		}

		private static DiagnosticBase[] GetDocumentDiagnostics(string name)
		{
			var registry = DocumentLoader.CreateRegistry();
			var docResult = GetOpenApiDocument(name, registry);
			Assert.NotNull(docResult.Document);
			var options = LoadOptions();

			var transformer = docResult.Document.BuildCSharpPathControllerSourceProvider(registry, "", "PS.Controller", options);
			OpenApiTransformDiagnostic diagnostic = new();

			try
			{
				var sources = transformer.GetSources(diagnostic).ToArray(); // force all sources to load to get diagnostics
				return docResult.Diagnostics.Concat(diagnostic.Diagnostics).ToArray();
			}
			catch
			{
				return docResult.Diagnostics.ToArray();
			}
		}

		[Fact]
		public void Report_unresolved_external_references()
		{
			var diagnostics = GetDocumentDiagnostics("bad.yaml");

			Assert.Collection(diagnostics, [
				(DiagnosticBase diag) =>
				{
					Assert.IsType<UnableToParseKeyword>(diag);
					Assert.Equal("proj://embedded/bad.yaml", diag.Location.RetrievalUri.OriginalString);
					Assert.Equal(26, diag.Location.Range?.Start.Line);
					Assert.Equal(23, diag.Location.Range?.Start.Column);
				},
				(DiagnosticBase diag) =>
				{
					var targetNodeDiagnostic = Assert.IsType<CouldNotFindTargetNodeDiagnostic>(diag);
					Assert.Equal("proj://embedded/bad.yaml", diag.Location.RetrievalUri.OriginalString);
					Assert.Equal(75, diag.Location.Range?.Start.Line);
					Assert.Equal(17, diag.Location.Range?.Start.Column);
					Assert.Equal("proj://embedded/petstore.yaml#/Pet", targetNodeDiagnostic.Uri.OriginalString);
				}
			]);
		}

	}
}
