using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using Xunit;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApi.Transformations;
using static DarkPatterns.OpenApiCodegen.Client.CSharp.OptionsHelpers;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using DarkPatterns.OpenApiCodegen.TestUtils;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp;

internal class DynamicCompilation
{
	public static readonly string[] SystemTextCompilationRefPaths = {
			Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll"),
			Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"),
			typeof(System.AttributeUsageAttribute).Assembly.Location,
			typeof(System.Linq.Enumerable).Assembly.Location,
			typeof(System.ComponentModel.TypeConverter).Assembly.Location,
			typeof(System.ComponentModel.TypeConverterAttribute).Assembly.Location,
			typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location,
			typeof(System.Text.Json.JsonSerializer).Assembly.Location,
			typeof(System.Net.Http.Json.JsonContent).Assembly.Location,

			Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Net.Http.dll"),
			Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Net.Primitives.dll"),
			typeof(Uri).Assembly.Location,
			typeof(System.Web.HttpUtility).Assembly.Location,
			typeof(System.Collections.Specialized.NameValueCollection).Assembly.Location,
			typeof(DarkPatterns.OpenApiCodegen.Json.Extensions.JsonStringEnumPropertyNameConverter).Assembly.Location,
	};

	public static byte[] GetGeneratedLibrary(string documentName, Action<CSharpSchemaOptions>? configureOptions = null)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = GetOpenApiDocument(documentName, registry);
		Assert.NotNull(docResult.Document);
		var document = docResult.Document;
		var options = LoadOptions();
		configureOptions?.Invoke(options);

		var transformer = ClientTransformerFactory.Build(document, registry, "", options);
		OpenApiTransformDiagnostic diagnostic = new();

		var entries = transformer.GetSources(diagnostic).ToArray();

		Assert.Empty(diagnostic.Diagnostics);

		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);
		var syntaxTrees = entries.Select(e => CSharpSyntaxTree.ParseText(e.SourceText, options: parseOptions, path: e.Key)).ToArray();

		string assemblyName = Path.GetRandomFileName();
		MetadataReference[] references = SystemTextCompilationRefPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

		CSharpCompilation compilation = CSharpCompilation.Create(assemblyName)
			.WithReferences(references)
			.AddSyntaxTrees(syntaxTrees)
			.WithOptions(new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
			)
		);

		using var ms = new MemoryStream();
		var result = compilation.Emit(ms);

		Assert.All(result.Diagnostics, diagnostic =>
		{
			Assert.False(diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
		});
		Assert.True(result.Success);
		return ms.ToArray();
	}
}
