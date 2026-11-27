using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Linq;
using Xunit;
using DarkPatterns.OpenApi.CSharp;
using static DarkPatterns.OpenApiCodegen.Server.Mvc.OptionsHelpers;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using DarkPatterns.OpenApiCodegen.TestUtils;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApiCodegen.CSharp;

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

		typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location,
		typeof(Microsoft.Extensions.DependencyInjection.IMvcBuilder).Assembly.Location,
		typeof(Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions).Assembly.Location,

		typeof(Microsoft.Net.Http.Headers.MediaTypeHeaderValue).Assembly.Location,
		typeof(Microsoft.Extensions.Primitives.StringSegment).Assembly.Location,
		typeof(Microsoft.AspNetCore.Mvc.IActionResult).Assembly.Location,
		typeof(Microsoft.AspNetCore.Mvc.ObjectResult).Assembly.Location,
		typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.Location,
		typeof(Microsoft.AspNetCore.Http.IHeaderDictionary).Assembly.Location,
		typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location,
		typeof(Microsoft.AspNetCore.Authorization.IAllowAnonymous).Assembly.Location,
		typeof(Microsoft.AspNetCore.Authorization.Policy.IPolicyEvaluator).Assembly.Location,
		typeof(Microsoft.AspNetCore.Authentication.AuthenticationProperties).Assembly.Location,
		typeof(System.Security.Claims.ClaimsPrincipal).Assembly.Location,
		Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.ComponentModel.dll"),
		Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Memory.dll"),

		typeof(DarkPatterns.OpenApiCodegen.Json.Extensions.JsonStringEnumPropertyNameConverter).Assembly.Location,
	};

	/// <summary>Generated source text by file name, for checking what the generated code says (such as documentation)</summary>
	public static System.Collections.Generic.Dictionary<string, string> GetGeneratedSources(string documentName, bool jsonUnions = false)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = GetOpenApiDocument(documentName, registry);
		Assert.NotNull(docResult.Result);
		var options = LoadOptions();

		var transformer = TransformSettings.BuildComposite(registry, "", [
			(s) => new PathControllerTransformerFactory(s).Build(docResult, options),
			(s) => new CSharpSchemaSourceProvider(s, options, useJsonUnions: jsonUnions)
		]);

		var generated = transformer.GetSources();
		Assert.Empty(generated.Diagnostics);
		return generated.Sources.ToDictionary(e => e.Key, e => e.SourceText.ToString());
	}

	public static byte[] GetGeneratedLibrary(string documentName, bool jsonUnions = false)
	{
		var registry = DocumentLoader.CreateRegistry();
		var docResult = GetOpenApiDocument(documentName, registry);
		Assert.NotNull(docResult.Result);
		var options = LoadOptions();

		var transformer = TransformSettings.BuildComposite(registry, "", [
			(s) => new PathControllerTransformerFactory(s).Build(docResult, options),
			(s) => new CSharpSchemaSourceProvider(s, options, useJsonUnions: jsonUnions)
		]);
		registry.DocumentRegistry.Register(docResult.Result);
		var newDiagnostics = registry.RecursivelyFixupAll();
		Assert.Empty(newDiagnostics);

		var generated = transformer.GetSources();

		Assert.Empty(generated.Diagnostics);

		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);
		var syntaxTrees = generated.Sources.Select(e => CSharpSyntaxTree.ParseText(e.SourceText, options: parseOptions, path: e.Key)).ToArray();

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
			Assert.False(diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error, diagnostic.GetMessage());
		});
		Assert.True(result.Success);
		return ms.ToArray();
	}
}
