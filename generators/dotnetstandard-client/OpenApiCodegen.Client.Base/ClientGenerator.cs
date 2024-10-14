using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApi.CSharp;

public class ClientGenerator : IOpenApiCodeGenerator
{
	const string propNamespace = "Namespace";
	const string propConfig = "Configuration";
	const string propIdentity = "identity";
	const string propLink = "link";
	const string propSchemaId = "schemaId";

	const string sourceGroup = "OpenApiClientInterface";
	const string sharedSourceGroup = "JsonSchemaDocument";

	private readonly IEnumerable<string> metadataKeys = new[]
	{
		propNamespace,
		propConfig,
		propIdentity,
		propLink,
	};
	public IEnumerable<string> MetadataKeys => metadataKeys;

	public AdditionalTextInfo ToFileInfo(string documentPath, string documentContents, IReadOnlyList<string> types, IReadOnlyDictionary<string, string?> additionalTextMetadata)
	{
		return new(Path: documentPath, Contents: documentContents, Types: types, Metadata: additionalTextMetadata);
	}

	public GenerationResult Generate(IEnumerable<AdditionalTextInfo> additionalTextInfos)
	{
		var registry = new DocumentRegistry(ToRegistryOptions(additionalTextInfos));
		var schemaRegistry = new SchemaRegistry(registry);
		var pathResolver = ToPathResolver(additionalTextInfos);

		var entrypoints = additionalTextInfos.Where(f => f.Types.Contains(sourceGroup));
		return entrypoints.Select(ep => GenerateOneDocument(ep, schemaRegistry, LoadOptionsFromMetadata(ep.Metadata, additionalTextInfos), pathResolver))
			.Aggregate((prev, next) => new GenerationResult([.. prev.Sources, .. next.Sources], [.. prev.Diagnostics, .. next.Diagnostics]));
	}

	public static GenerationResult GenerateOneDocument(AdditionalTextInfo entrypoint, SchemaRegistry schemaRegistry, CSharpSchemaOptions options, PathResolver pathResolver)
	{
		var baseDocument = schemaRegistry.DocumentRegistry.ResolveDocument(ToInternalUri(entrypoint), null);
		var diagnosticConverter = DiagnosticsConversion.GetConverter(pathResolver);
		var parseResult = CommonParsers.DefaultParsers.Parse(baseDocument, schemaRegistry.DocumentRegistry);
		var parsedDiagnostics = parseResult.Diagnostics;
		if (!parseResult.HasDocument || parseResult.Document == null)
			return new GenerationResult([], Convert(parsedDiagnostics));

		var sourceProvider = TransformSettings.BuildComposite(schemaRegistry, GetVersionInfo(), [
			(s) => new ClientTransformerFactory(s).Build(parseResult.Document, options),
			(s) => new CSharpSchemaSourceProvider(s, options)
		]);
		var openApiDiagnostic = new OpenApiTransformDiagnostic();

		try
		{
			var result = sourceProvider.GetSources();

			return new GenerationResult(
				result.Sources,
				Convert([.. parsedDiagnostics, .. result.Diagnostics])
			);
		}
		catch (Exception) when (parsedDiagnostics is not [])
		{
			return new GenerationResult(
				[],
				Convert(parsedDiagnostics)
			);
		}
#pragma warning disable CA1031 // Catching a general exception type here to turn it into a diagnostic for reporting
		catch (Exception ex)
		{
			return new GenerationResult(
				[],
				Convert(ex.ToDiagnostics(schemaRegistry.DocumentRegistry, NodeMetadata.FromRoot(baseDocument)))
			);
		}
#pragma warning restore CA1031 // Do not catch general exception types

		DiagnosticInfo[] Convert(IEnumerable<DiagnosticBase> diagnostics)
		{
			return diagnostics.Select(diagnosticConverter).ToArray();
		}
	}

	private static CSharpSchemaOptions LoadOptionsFromMetadata(IReadOnlyDictionary<string, string?> entrypointMetadata, IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		var fullNamespace = entrypointMetadata[propNamespace];
		var optionsFiles = entrypointMetadata[propConfig];
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		var result = OptionsLoader.LoadOptions<CSharpSchemaOptions>([defaultJsonStream], optionsFiles is { Length: > 0 } s ? s.Split(';') : []);

		result.DefaultNamespace = fullNamespace ?? GetStandardNamespace(entrypointMetadata, result);
		foreach (var entry in additionalSchemas)
		{
			// TODO: This ignores the propNamespace
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

	private static PathResolver ToPathResolver(IEnumerable<AdditionalTextInfo> files)
	{
		var paths = files.Distinct().ToLookup(ToInternalUri, doc => doc.Path);
		return (uri) => paths[uri].FirstOrDefault();
	}

	private static DocumentRegistryOptions ToRegistryOptions(IEnumerable<AdditionalTextInfo> additionalSchemas) =>
		new DocumentRegistryOptions(
			additionalSchemas
				.Select(doc => DocumentResolverFactory.LoadAs(ToInternalUri(doc), doc.Contents))
				.ToArray()
		);
}
