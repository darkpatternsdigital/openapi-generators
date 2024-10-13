using Microsoft.Extensions.Configuration;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.CSharp;

public class ClientGenerator : IOpenApiCodeGenerator
{
	const string propNamespace = "Namespace";
	const string propConfig = "Configuration";
	const string propIdentity = "identity";
	const string propLink = "link";
	const string propSchemaId = "schemaId";
	private readonly IEnumerable<string> metadataKeys = new[]
	{
		propNamespace,
		propConfig,
		propIdentity,
		propLink,
	};
	public IEnumerable<string> MetadataKeys => metadataKeys;

	public AdditionalTextInfo ToFileInfo(string documentPath, string documentContents, IReadOnlyDictionary<string, string?> additionalTextMetadata)
	{
		return new(Path: documentPath, Contents: documentContents, Metadata: additionalTextMetadata);
	}

	public GenerationResult Generate(AdditionalTextInfo entrypoint, IEnumerable<AdditionalTextInfo> other)
	{
		var options = LoadOptionsFromMetadata(entrypoint.Metadata, other);
		var (baseDocument, registry, pathResolver) = LoadDocument(entrypoint, options, other);
		var diagnosticConverter = DiagnosticsConversion.GetConverter(pathResolver);
		var parseResult = CommonParsers.DefaultParsers.Parse(baseDocument, registry);
		var parsedDiagnostics = parseResult.Diagnostics.Select(diagnosticConverter).ToArray();
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
				parsedDiagnostics.Concat(parsedDiagnostics.Concat(diagnostics.Select(diagnosticConverter))).ToArray()
			);
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	private static ISourceProvider CreateSourceProvider(Abstractions.OpenApiDocument document, DocumentRegistry registry, CSharpSchemaOptions options, IReadOnlyDictionary<string, string?> opt)
	{
		return ClientTransformerFactory.BuildComposite(document, registry, GetVersionInfo(), options);
	}

	private static CSharpSchemaOptions LoadOptionsFromMetadata(IReadOnlyDictionary<string, string?> entrypointMetadata, IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		var fullNamespace = entrypointMetadata[propNamespace];
		var optionsFiles = entrypointMetadata[propConfig];
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

		result.DefaultNamespace = fullNamespace ?? GetStandardNamespace(entrypointMetadata, result);
		foreach (var entry in additionalSchemas)
		{
			var ns = GetStandardNamespace(entry.Metadata, result);
			if (result.DefaultNamespace != ns)
				result.NamespacesBySchema[ToInternalUri(entry)] = ns;
		}
		return result;
	}

	private static string GetVersionInfo()
	{
		return $"{typeof(CSharpClientTransformer).FullName} v{typeof(CSharpClientTransformer).Assembly.GetName().Version}";
	}

	private static string GetStandardNamespace(IReadOnlyDictionary<string, string?> metadata, CSharpSchemaOptions options)
	{
		var identity = metadata["identity"];
		var link = metadata["link"];
		metadata.TryGetValue("build_property.projectdir", out var projectDir);
		metadata.TryGetValue("build_property.rootnamespace", out var rootNamespace);

		return CSharpNaming.ToNamespace(rootNamespace, projectDir, identity, link, options.ReservedIdentifiers());
	}

	private static Uri ToInternalUri(AdditionalTextInfo document) =>
		document.Metadata.TryGetValue(propSchemaId, out var schemaId) ? new Uri(schemaId) :
		new Uri(new Uri(document.Path).AbsoluteUri);

	private static (IDocumentReference, DocumentRegistry, PathResolver) LoadDocument(AdditionalTextInfo document, CSharpSchemaOptions options, IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		var paths = additionalSchemas.ConcatOne(document).Distinct().ToLookup(ToInternalUri, doc => doc.Path);

		var (docRef, reg) = DocumentResolverFactory.FromInitialDocumentInMemory(
			ToInternalUri(document),
			document.Contents,
			ToResolverOptions(options, additionalSchemas)
		);
		return (docRef, reg, (uri) => paths[uri].FirstOrDefault());
	}

	private static DocumentRegistryOptions ToResolverOptions(CSharpSchemaOptions options, IEnumerable<AdditionalTextInfo> additionalSchemas) =>
		new DocumentRegistryOptions(
			additionalSchemas
				.Select(doc => DocumentResolverFactory.LoadAs(ToInternalUri(doc), doc.Contents))
				.ToArray()
		);
}
