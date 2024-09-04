using Microsoft.Extensions.Configuration;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using System.IO;
using System.Net.Mime;

namespace DarkPatterns.OpenApi.CSharp;

public class MvcServerGenerator : IOpenApiCodeGenerator
{
	const string propNamespace = "Namespace";
	const string propConfig = "Configuration";
	const string propIdentity = "identity";
	const string propLink = "link";
	const string propPathPrefix = "pathPrefix";
	private readonly IEnumerable<string> metadataKeys = new[]
	{
		propNamespace,
		propConfig,
		propIdentity,
		propLink,
		propPathPrefix,
	};

	public IEnumerable<string> MetadataKeys => metadataKeys;

	public AdditionalTextInfo ToFileInfo(string documentPath, string documentContents, IReadOnlyDictionary<string, string?> additionalTextMetadata)
	{
		return new(Path: documentPath, Contents: documentContents, Metadata: additionalTextMetadata);
	}

	public GenerationResult Generate(AdditionalTextInfo entrypoint, IEnumerable<AdditionalTextInfo> other)
	{
		var options = LoadOptionsFromMetadata(entrypoint.Metadata, other);
		var (baseDocument, registry) = LoadDocument(entrypoint, options, other);
		var parseResult = CommonParsers.DefaultParsers.Parse(baseDocument, registry);
		var parsedDiagnostics = parseResult.Diagnostics.Select(DiagnosticsConversion.ToDiagnosticInfo).ToArray();
		if (!parseResult.HasDocument || parseResult.Document == null)
			return new GenerationResult(Array.Empty<OpenApiCodegen.SourceEntry>(), parsedDiagnostics);

		var sourceProvider = CreateSourceProvider(parseResult.Document, registry, options, entrypoint.Metadata);
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

	private static ISourceProvider CreateSourceProvider(OpenApiDocument document, DocumentRegistry registry, CSharpServerSchemaOptions options, IReadOnlyDictionary<string, string?> opt)
	{
		var documentNamespace = opt[propNamespace];
		if (string.IsNullOrEmpty(documentNamespace))
			documentNamespace = GetStandardNamespace(opt, options);

		return document.BuildCSharpPathControllerSourceProvider(registry, GetVersionInfo(), documentNamespace, options);
	}

	private static CSharpServerSchemaOptions LoadOptionsFromMetadata(IReadOnlyDictionary<string, string?> additionalTextMetadata, IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		return LoadOptions(additionalTextMetadata[propConfig], additionalTextMetadata[propPathPrefix]);
	}

	private static CSharpServerSchemaOptions LoadOptions(string? optionsFiles, string? pathPrefix)
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
		var result = builder.Build().Get<CSharpServerSchemaOptions>();
		// TODO - generate diagnostic instead of throwing exception
		if (result == null) throw new InvalidOperationException("Could not build schema options");

		if (pathPrefix != null)
			result.PathPrefix = pathPrefix;

		return result;
	}

	private static string GetVersionInfo()
	{
		return $"{typeof(CSharpControllerTransformer).FullName} v{typeof(CSharpControllerTransformer).Assembly.GetName().Version}";
	}

	private static string? GetStandardNamespace(IReadOnlyDictionary<string, string?> opt, CSharpSchemaOptions options)
	{
		var identity = opt[propIdentity];
		var link = opt[propLink];
		opt.TryGetValue("build_property.projectdir", out var projectDir);
		opt.TryGetValue("build_property.rootnamespace", out var rootNamespace);

		return CSharpNaming.ToNamespace(rootNamespace, projectDir, identity, link, options.ReservedIdentifiers());
	}

	private static Uri ToInternalUri(AdditionalTextInfo document) =>
		// TODO: check schema url from metadata
		new Uri(new Uri(document.Path).AbsoluteUri);

	private static (IDocumentReference, DocumentRegistry) LoadDocument(AdditionalTextInfo document, CSharpServerSchemaOptions options, IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		return DocumentResolverFactory.FromInitialDocumentInMemory(
			ToInternalUri(document),
			document.Contents,
			ToResolverOptions(options, additionalSchemas)
		);
	}

	private static DocumentRegistryOptions ToResolverOptions(CSharpServerSchemaOptions options, IEnumerable<AdditionalTextInfo> additionalSchemas) =>
		new DocumentRegistryOptions(
			additionalSchemas
				.Select(doc => DocumentResolverFactory.LoadAs(ToInternalUri(doc), doc.Contents))
				.ToArray()
		// TODO: use the `options` to determine how to resolve additional documents
		);
}
