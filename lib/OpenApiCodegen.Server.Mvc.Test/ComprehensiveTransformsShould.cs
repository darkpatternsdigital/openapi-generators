﻿using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications.Keywords;
using DarkPatterns.OpenApiCodegen.TestUtils;
using System.Linq;
using Xunit;
using static DarkPatterns.OpenApiCodegen.Server.Mvc.OptionsHelpers;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

namespace DarkPatterns.OpenApiCodegen.CSharp;

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
	[InlineData("request-ref.yaml")]
	[InlineData("response-ref.yaml")]
	[InlineData("openapi-3.0-callbacks.yaml")]
	[Theory]
	public void Compile_api_documents_included_in_the_TestApp(string name)
	{
		DynamicCompilation.GetGeneratedLibrary(name);
	}

	private static DiagnosticBase[] GetDocumentDiagnostics(string name)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = GetOpenApiDocument(name, registry);
		Assert.NotNull(docResult.Result);
		var options = LoadOptions();

		var transformer = TransformSettings.BuildComposite(registry, "", [
			(s) => new PathControllerTransformerFactory(s).Build(docResult, options),
			(s) => new CSharpSchemaSourceProvider(s, options)
		]);

		try
		{
			var generated = transformer.GetSources();
			return [.. generated.Diagnostics];
		}
		catch
		{
			return [.. docResult.Diagnostics];
		}
	}

	[Fact]
	public void Reports_diagnostics_for_bad_yaml()
	{
		var diagnostics = GetDocumentDiagnostics("bad.yaml");

		Assert.Collection(diagnostics,
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
		);
	}

	[Fact]
	public void Reports_diagnostics_for_bad_2_yaml()
	{
		var diagnostics = GetDocumentDiagnostics("bad.2.yaml");

		Assert.Collection(diagnostics,
			(DiagnosticBase diag) =>
			{
				Assert.IsType<UnableToCreateInlineSchemaDiagnostic>(diag);
				Assert.Equal("proj://embedded/bad.2.yaml", diag.Location.RetrievalUri.OriginalString);
				Assert.Equal(18, diag.Location.Range?.Start.Line);
				Assert.Equal(25, diag.Location.Range?.Start.Column);
				Assert.Equal(18, diag.Location.Range?.End.Line);
				Assert.Equal(55, diag.Location.Range?.End.Column);
			}
		);
	}

}
