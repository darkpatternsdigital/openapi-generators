using Microsoft.Extensions.Configuration;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApi.Transformations.Diagnostics;

namespace DarkPatterns.OpenApi.CSharp;

public class ClientGenerator : IOpenApiCodeGenerator
{
	const string propNamespace = "Namespace";
	const string propConfig = "Configuration";
	const string propIdentity = "identity";
	const string propLink = "link";
	private readonly IEnumerable<string> metadataKeys = new[]
	{
		propNamespace,
		propConfig,
		propIdentity,
		propLink,
	};
	public IEnumerable<string> MetadataKeys => metadataKeys;

	public GenerationResult Generate(string documentPath, string documentContents, IReadOnlyDictionary<string, string?> additionalTextMetadata)
	{
		var options = LoadOptionsFromMetadata(additionalTextMetadata);
		var (baseDocument, registry) = LoadDocument(documentPath, documentContents, options);
		var parseResult = CommonParsers.DefaultParsers.Parse(baseDocument, registry);
		var parsedDiagnostics = parseResult.Diagnostics.Select(DiagnosticsConversion.ToDiagnosticInfo).ToArray();
		if (!parseResult.HasDocument || parseResult.Document == null)
			return new GenerationResult(Array.Empty<OpenApiCodegen.SourceEntry>(), parsedDiagnostics);

		var sourceProvider = CreateSourceProvider(parseResult.Document, registry, options, additionalTextMetadata);
		var openApiDiagnostic = new OpenApiTransformDiagnostic();

		try
		{
			var sources = (from entry in sourceProvider.GetSources(openApiDiagnostic)
						   select new OpenApiCodegen.SourceEntry(entry.Key, entry.SourceText)).ToArray();

			return new GenerationResult(
				sources,
				parsedDiagnostics
			);
		}
#pragma warning disable CA1031 // Catching a general exception type here to turn it into a diagnostic for reporting
		catch (Exception ex)
		{
			var diagnostics = new List<DiagnosticBase>();
			diagnostics.AddExceptionAsDiagnostic(ex, registry, NodeMetadata.FromRoot(baseDocument));

			return new GenerationResult(
				Array.Empty<OpenApiCodegen.SourceEntry>(),
				parsedDiagnostics.Concat(parsedDiagnostics.Concat(diagnostics.Select(DiagnosticsConversion.ToDiagnosticInfo))).ToArray()
			);
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	private static ISourceProvider CreateSourceProvider(Transformations.Abstractions.OpenApiDocument document, DocumentRegistry registry, CSharpSchemaOptions options, IReadOnlyDictionary<string, string?> opt)
	{
		var documentNamespace = opt[propNamespace];
		if (string.IsNullOrEmpty(documentNamespace))
			documentNamespace = GetStandardNamespace(opt, options);

		return document.BuildCSharpClientSourceProvider(registry, GetVersionInfo(), documentNamespace, options);
	}

	private static CSharpSchemaOptions LoadOptionsFromMetadata(IReadOnlyDictionary<string, string?> additionalTextMetadata)
	{
		return LoadOptions(additionalTextMetadata[propConfig]);
	}

	private static CSharpSchemaOptions LoadOptions(string? optionsFiles)
	{
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		var builder = new ConfigurationBuilder();
		builder.AddYamlStream(defaultJsonStream);
		if (optionsFiles is { Length: > 0 })
		{
			foreach (var file in optionsFiles.Split(';'))
			{
				if (System.IO.File.Exists(file))
				{
					builder.AddYamlFile(file);
				}
			}
		}
		var result = builder.Build().Get<CSharpSchemaOptions>();
		// TODO - generate diagnostic instead of throwing exception
		if (result == null) throw new InvalidOperationException("Could not build schema options");
		return result;
	}

	private static string GetVersionInfo()
	{
		return $"{typeof(CSharpClientTransformer).FullName} v{typeof(CSharpClientTransformer).Assembly.GetName().Version}";
	}

	private static string? GetStandardNamespace(IReadOnlyDictionary<string, string?> opt, CSharpSchemaOptions options)
	{
		var identity = opt["identity"];
		var link = opt["link"];
		opt.TryGetValue("build_property.projectdir", out var projectDir);
		opt.TryGetValue("build_property.rootnamespace", out var rootNamespace);

		return CSharpNaming.ToNamespace(rootNamespace, projectDir, identity, link, options.ReservedIdentifiers());
	}

	private static Uri ToInternalUri(string documentPath) =>
		new Uri(new Uri(documentPath).AbsoluteUri);

	private static (IDocumentReference, DocumentRegistry) LoadDocument(string documentPath, string documentContents, CSharpSchemaOptions options)
	{
		return DocumentResolverFactory.FromInitialDocumentInMemory(
			ToInternalUri(documentPath),
			documentContents,
			ToResolverOptions(options)
		);
	}

	private static DocumentRegistryOptions ToResolverOptions(CSharpSchemaOptions options) =>
		new DocumentRegistryOptions([
		// TODO: use the `options` to determine how to resolve additional documents
		]);
}
