using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DarkPatterns.OpenApiCodegen;

/// <summary>
/// oneOf models use System.Text.Json 11's union support and the UnionAttribute and IUnion types. They're on when
/// the framework provides them (.NET 11+), or when an options file sets jsonUnions, so the generator checks for them.
/// </summary>
public class JsonUnionsReferenceShould
{
	private const string UnionTypesSource = """
		namespace System.Runtime.CompilerServices
		{
			public sealed class UnionAttribute : System.Attribute { }
			public interface IUnion { object? Value { get; } }
		}
		""";
	private const string TypeClassifierSource = "namespace System.Text.Json.Serialization { public abstract class JsonTypeClassifierFactory<T> { } }";

	private const string OneOfSchema = """
		openapi: 3.0.3
		info:
		  title: OneOf
		  version: 1.0.0
		paths:
		  /value:
		    get:
		      operationId: getValue
		      responses:
		        '200':
		          description: OK
		          content:
		            application/json:
		              schema:
		                $ref: '#/components/schemas/IdOrName'
		components:
		  schemas:
		    IdOrName:
		      oneOf:
		        - type: integer
		        - type: string
		""";

	public enum UnionTypes { None, Source, Reference }

	[Theory]
	// Not set: nothing to check
	[InlineData(null, false, UnionTypes.None, null, new string[0])]
	[InlineData("false", false, UnionTypes.None, null, new string[0])]
	// On, as on .NET 11, or with System.Text.Json 11 and the union types
	[InlineData("true", true, UnionTypes.Reference, null, new string[0])]
	[InlineData("true", true, UnionTypes.Source, null, new string[0])]
	// On, with PolySharp generating the union types, which the generator can't see
	[InlineData("true", true, UnionTypes.None, "1.17.1", new string[0])]
	// On, without System.Text.Json 11
	[InlineData("true", false, UnionTypes.Source, null, new[] { "DPDAPI004" })]
	// On, without the union types, or with a PolySharp version that doesn't have them
	[InlineData("true", true, UnionTypes.None, null, new[] { "DPDAPI005" })]
	[InlineData("true", true, UnionTypes.None, "1.16.0", new[] { "DPDAPI005" })]
	[InlineData("true", false, UnionTypes.None, null, new[] { "DPDAPI004", "DPDAPI005" })]
	public void Report_missing_json_union_support(string? jsonUnions, bool withTypeClassifier, UnionTypes unionTypes, string? polySharpVersion, string[] expectedDiagnosticIds)
	{
		var result = Run(jsonUnions, withTypeClassifier, unionTypes, polySharpVersion);

		var actual = result.Diagnostics.Select(d => d.Id).Where(id => id is "DPDAPI004" or "DPDAPI005").Order().ToArray();
		Assert.Equal(expectedDiagnosticIds, actual);
		Assert.All(result.Diagnostics.Where(d => d.Id is "DPDAPI004" or "DPDAPI005"), d => Assert.EndsWith("options.yaml", d.Location.GetLineSpan().Path));
	}

	[Theory]
	// The framework provides union support, as on .NET 11
	[InlineData(null, true, UnionTypes.Reference, true)]
	[InlineData("false", true, UnionTypes.Reference, false)]
	// Below .NET 11, only with the option
	[InlineData(null, true, UnionTypes.Source, false)]
	[InlineData("true", true, UnionTypes.Source, true)]
	public void Use_json_unions_when_the_framework_supports_them_or_the_options_file_sets_them(string? jsonUnions, bool withTypeClassifier, UnionTypes unionTypes, bool expected)
	{
		var result = Run(jsonUnions, withTypeClassifier, unionTypes, polySharpVersion: null);

		var source = result.Results.Single().GeneratedSources.Single(s => s.HintName.EndsWith(".IdOrName.cs", StringComparison.Ordinal)).SourceText.ToString();
		Assert.Equal(expected, source.Contains("[global::System.Runtime.CompilerServices.Union]", StringComparison.Ordinal));
	}

	private static GeneratorDriverRunResult Run(string? jsonUnions, bool withTypeClassifier, UnionTypes unionTypes, string? polySharpVersion)
	{
		MetadataReference[] references = [
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			.. withTypeClassifier || unionTypes == UnionTypes.Reference
				? [CreateFrameworkReference(withTypeClassifier, unionTypes == UnionTypes.Reference)]
				: Array.Empty<MetadataReference>(),
		];
		var compilation = CSharpCompilation.Create(
			"Consumer",
			unionTypes == UnionTypes.Source ? [CSharpSyntaxTree.ParseText(UnionTypesSource)] : [],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
		var schemaPath = Path.Combine(Path.GetTempPath(), "one-of.yaml");
		var optionsPath = Path.Combine(Path.GetTempPath(), "options.yaml");
		AdditionalText[] additionalTexts = [
			new InMemoryAdditionalText(schemaPath, OneOfSchema),
			.. jsonUnions == null ? Array.Empty<AdditionalText>() : [new InMemoryAdditionalText(optionsPath, $"jsonUnions: {jsonUnions}")],
		];

		return CSharpGeneratorDriver.Create(
			[BuiltGenerator.Create().AsSourceGenerator()],
			additionalTexts: additionalTexts,
			optionsProvider: new OptionsProvider(
				new Dictionary<string, string> { ["build_property._DPDPolySharpVersion"] = polySharpVersion ?? "" },
				new Dictionary<string, Dictionary<string, string>>
				{
					[schemaPath] = new() { ["build_metadata.additionalfiles.DPDGenerateModels"] = "true", ["build_metadata.additionalfiles.Namespace"] = "Consumer" },
					[optionsPath] = new() { ["build_metadata.additionalfiles.DPDSchemaOptions"] = "true" },
				}
			)
		).RunGenerators(compilation).GetRunResult();
	}

	// Stands in for the framework's System.Text.Json and System.Runtime
	private static MetadataReference CreateFrameworkReference(bool withTypeClassifier, bool withUnionTypes)
	{
		var library = CSharpCompilation.Create(
			"Framework",
			[
				.. withTypeClassifier ? [CSharpSyntaxTree.ParseText(TypeClassifierSource)] : Array.Empty<SyntaxTree>(),
				.. withUnionTypes ? [CSharpSyntaxTree.ParseText(UnionTypesSource)] : Array.Empty<SyntaxTree>(),
			],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
		);
		using var stream = new MemoryStream();
		var emitResult = library.Emit(stream);
		Assert.True(emitResult.Success);
		return MetadataReference.CreateFromImage(stream.ToArray());
	}

	private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
	{
		public override string Path => path;
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
	}

	private sealed class OptionsProvider(IReadOnlyDictionary<string, string> globalOptions, IReadOnlyDictionary<string, Dictionary<string, string>> fileOptions) : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(globalOptions);
		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
			new Options(fileOptions.TryGetValue(textFile.Path, out var options) ? options : new Dictionary<string, string>());

		private sealed class Options(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
		{
			public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
				values.TryGetValue(key, out value);
		}
	}
}
