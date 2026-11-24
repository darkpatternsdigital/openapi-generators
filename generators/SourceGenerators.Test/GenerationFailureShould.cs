using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DarkPatterns.OpenApiCodegen;

/// <summary>
/// Roslyn's own CS8785 warning only names the exception type, which hides the cause of
/// exceptions such as TypeInitializationException, so the generator reports the whole exception.
/// </summary>
public class GenerationFailureShould
{
	[Fact]
	public void Report_the_exception_with_its_details()
	{
		// Options files aren't validated yet, so invalid YAML throws during generation
		var optionsFile = new InMemoryAdditionalText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "options.yaml"), "mapType: [");
		var driver = CSharpGeneratorDriver.Create(
			[BuiltGenerator.Create().AsSourceGenerator()],
			additionalTexts: [optionsFile],
			optionsProvider: new MetadataOptionsProvider(new Dictionary<string, string>
			{
				["build_metadata.additionalfiles.DPDSchemaOptions"] = "true",
			})
		);

		var result = driver.RunGenerators(CSharpCompilation.Create("Consumer")).GetRunResult();

		var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "DPDAPI003");
		Assert.DoesNotContain(result.Diagnostics, d => d.Id == "CS8785");
		// The stack trace shows where it was thrown, which CS8785 leaves out
		Assert.Contains(" at DarkPatterns.", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
	}

	private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
	{
		public override string Path => path;
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
	}

	private sealed class MetadataOptionsProvider(IReadOnlyDictionary<string, string> fileOptions) : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(ImmutableDictionary<string, string>.Empty);
		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Options(fileOptions);

		private sealed class Options(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
		{
			public override bool TryGetValue(string key, out string value) =>
				values.TryGetValue(key, out value!);
		}
	}
}
