using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// oneOf models are System.Text.Json union types when the framework provides union support (.NET 11+),
/// or when an options file sets <c>jsonUnions</c>. The Roslyn generator describes the compilation with
/// <c>compilation.*</c> metadata.
/// </summary>
public class JsonUnionsOptionShould
{
	private const string UnionAttribute = "[global::System.Runtime.CompilerServices.Union]";

	private static readonly Dictionary<string, string?> Net11 = new()
	{
		["compilation.jsonTypeClassifier"] = "true",
		["compilation.frameworkUnionTypes"] = "true",
		["compilation.unionTypes"] = "true",
	};
	private static readonly Dictionary<string, string?> Net10 = new()
	{
		["compilation.jsonTypeClassifier"] = "false",
		["compilation.frameworkUnionTypes"] = "false",
		["compilation.unionTypes"] = "false",
	};
	private static readonly Dictionary<string, string?> Net10WithPackages = new()
	{
		["compilation.jsonTypeClassifier"] = "true",
		["compilation.frameworkUnionTypes"] = "false",
		["compilation.unionTypes"] = "true",
	};

	[Fact]
	public void Use_json_unions_when_the_framework_supports_them()
	{
		var result = Generate(Net11, options: null);

		Assert.Empty(result.Diagnostics);
		Assert.Contains(UnionAttribute, PetSource(result));
	}

	[Fact]
	public void Use_the_json_converter_below_net11_by_default()
	{
		var result = Generate(Net10, options: null);

		Assert.Empty(result.Diagnostics);
		Assert.DoesNotContain(UnionAttribute, PetSource(result));
	}

	[Fact]
	public void Use_the_json_converter_when_the_options_file_turns_json_unions_off()
	{
		var result = Generate(Net11, options: "jsonUnions: false");

		Assert.Empty(result.Diagnostics);
		Assert.DoesNotContain(UnionAttribute, PetSource(result));
	}

	[Fact]
	public void Use_json_unions_below_net11_when_the_options_file_turns_them_on()
	{
		var result = Generate(Net10WithPackages, options: "jsonUnions: true");

		Assert.Empty(result.Diagnostics);
		Assert.Contains(UnionAttribute, PetSource(result));
	}

	[Theory]
	[InlineData(false, true, new[] { "DarkPatterns.OpenApiCodegen.CSharp.JsonUnionsRequireSystemTextJson11Diagnostic" })]
	[InlineData(true, false, new[] { "DarkPatterns.OpenApiCodegen.CSharp.JsonUnionsRequireUnionTypesDiagnostic" })]
	[InlineData(false, false, new[] { "DarkPatterns.OpenApiCodegen.CSharp.JsonUnionsRequireSystemTextJson11Diagnostic", "DarkPatterns.OpenApiCodegen.CSharp.JsonUnionsRequireUnionTypesDiagnostic" })]
	public void Report_missing_json_union_support_at_the_options_file(bool jsonTypeClassifier, bool unionTypes, string[] expectedIds)
	{
		var result = Generate(new()
		{
			["compilation.jsonTypeClassifier"] = jsonTypeClassifier ? "true" : "false",
			["compilation.frameworkUnionTypes"] = "false",
			["compilation.unionTypes"] = unionTypes ? "true" : "false",
		}, options: "jsonUnions: true");

		Assert.Equal(expectedIds, result.Diagnostics.Select(d => d.Id).Order().ToArray());
		Assert.All(result.Diagnostics, d => Assert.EndsWith("options.yaml", d.Location.FilePath));
	}

	[Fact]
	public void Not_report_json_union_support_without_compilation_details()
	{
		// As when the generator is run without Roslyn, such as in tests
		var result = Generate([], options: "jsonUnions: true");

		Assert.Empty(result.Diagnostics);
		Assert.Contains(UnionAttribute, PetSource(result));
	}

	private static string PetSource(GenerationResult result) =>
		result.Sources.Single(s => s.Key == "DarkPatterns.OpenApiCodegen.Server.Mvc.Test.Pet.cs").SourceText;

	private static GenerationResult Generate(Dictionary<string, string?> compilationMetadata, string? options)
	{
		const string schemaName = "one-of.yaml";
		using var reader = new StreamReader(DocumentLoader.GetEmbeddedDocumentStream(new Uri(DocumentLoader.Embedded, schemaName)));
		var metadata = new Dictionary<string, string?>(compilationMetadata) { ["Namespace"] = "DarkPatterns.OpenApiCodegen.Server.Mvc.Test" };
		AdditionalTextInfo[] files = [
			new(
				Path: Path.Combine(Path.GetTempPath(), schemaName),
				Contents: reader.ReadToEnd(),
				Types: ["MvcServer"],
				Metadata: metadata
			),
			.. options == null ? Array.Empty<AdditionalTextInfo>() : [
				new AdditionalTextInfo(
					Path: Path.Combine(Path.GetTempPath(), "options.yaml"),
					Contents: options,
					Types: ["Config"],
					Metadata: metadata
				),
			],
		];
		return new CSharpGenerator().Generate(files);
	}
}
