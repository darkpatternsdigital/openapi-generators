﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApi.Transformations;
using static DarkPatterns.OpenApiCodegen.Server.Mvc.OptionsHelpers;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using DarkPatterns.OpenApiCodegen.TestUtils;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc
{
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

			typeof(DarkPatterns.OpenApiCodegen.Json.Extensions.JsonStringEnumPropertyNameConverter).Assembly.Location,
		};

		public static byte[] GetGeneratedLibrary(string documentName)
		{
			var registry = DocumentLoader.CreateRegistry();
			var docResult = GetOpenApiDocument(documentName, registry);
			Assert.NotNull(docResult.Document);
			var options = LoadOptions();

			var transformer = docResult.Document.BuildCSharpPathControllerSourceProvider(registry, "", options);
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
				Assert.False(diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error, diagnostic.GetMessage());
			});
			Assert.True(result.Success);
			return ms.ToArray();
		}
	}
}
