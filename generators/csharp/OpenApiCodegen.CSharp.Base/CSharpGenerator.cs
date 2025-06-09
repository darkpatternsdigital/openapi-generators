using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen.CSharp.Client;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;
using DarkPatterns.OpenApiCodegen.CSharp.WebhookClient;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApiCodegen.CSharp;

public class CSharpGenerator : IOpenApiCodeGenerator
{
	const string propNamespace = "Namespace";
	const string propConfig = "Configuration";
	const string propIdentity = "identity";
	const string propLink = "link";
	const string propPathPrefix = "pathPrefix";
	const string propSchemaId = "schemaId";

	const string typeMvcServer = "MvcServer";
	const string typeClient = "Client";
	const string typeWebhookClient = "WebhookClient";
	const string typeConfig = "Config";
	const string sharedSourceGroup = "JsonSchema";
	private readonly IEnumerable<string> metadataKeys =
	[
		propNamespace,
		propConfig,
		propIdentity,
		propLink,
		propPathPrefix,
		propSchemaId,
	];

	public IEnumerable<string> MetadataKeys => metadataKeys;

	public AdditionalTextInfo ToFileInfo(string documentPath, string documentContents, IReadOnlyList<string> types, IReadOnlyDictionary<string, string?> additionalTextMetadata)
	{
		return new(Path: documentPath, Contents: documentContents, Types: types, Metadata: additionalTextMetadata);
	}

	public GenerationResult Generate(IEnumerable<AdditionalTextInfo> additionalTextInfos)
	{
		var registry = new DocumentRegistry(ToRegistryOptions(additionalTextInfos));
		var schemaRegistry = new SchemaRegistry(registry);
		var settings = new TransformSettings(schemaRegistry, GetVersionInfo());

		var docs = (from document in additionalTextInfos
					let loaded = registry.ResolveDocument(ToInternalUri(document), relativeDocument: null)
					let options = LoadOptionsFromMetadata(document.Metadata, additionalTextInfos)
					select new { document, loaded, options }).ToArray();

		var mvcServerTransforms =
			(from e in docs
			 where e.document.Types.Contains(typeMvcServer)
			 let parseResult = CommonParsers.DefaultParsers.Parse(e.loaded, schemaRegistry)
			 select new PathControllerTransformerFactory(settings).Build(parseResult, e.options)).ToArray();
		var clientTransforms =
			(from e in docs
			 where e.document.Types.Contains(typeClient)
			 let parseResult = CommonParsers.DefaultParsers.Parse(e.loaded, schemaRegistry)
			 select new ClientTransformerFactory(settings).Build(parseResult, e.options)).ToArray();
		var webhookTransforms =
			(from e in docs
			 where e.document.Types.Contains(typeWebhookClient)
			 let parseResult = CommonParsers.DefaultParsers.Parse(e.loaded, schemaRegistry)
			 select new WebhookClientTransformerFactory(settings).Build(parseResult, e.options)).ToArray();

		var allSchemasOptions = LoadOptionsFromMetadata(additionalTextInfos);

		var sourceProvider = new CompositeOpenApiSourceProvider([
			.. mvcServerTransforms,
			.. clientTransforms,
			.. webhookTransforms,
			new CSharpSchemaSourceProvider(settings, allSchemasOptions),
		]);

		var result = sourceProvider.GetSources();

		return new GenerationResult(
			result.Sources,
			[.. result.Diagnostics.Select(DiagnosticsConversion.GetConverter(ToPathResolver(additionalTextInfos)))]
		);
	}

	private static CSharpServerSchemaOptions LoadOptionsFromMetadata(IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		var defaultJson = CSharpSchemaOptions.DefaultOptionsJson.Value;
		var serverJson = CSharpServerSchemaOptions.ServerDefaultOptionsJson.Value;

		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>(
			[
				defaultJson,
				serverJson,
				.. additionalSchemas.Where(s => s.Types.Contains(typeConfig)).Select(f => OptionsLoader.LoadYaml(f.Contents)),
			]
		);

		foreach (var entry in additionalSchemas)
		{
			var ns = GetStandardNamespace(entry.Metadata, result);
			if (result.DefaultNamespace != ns)
				result.NamespacesBySchema[ToInternalUri(entry)] = ns;
		}

		return result;
	}

	private static CSharpServerSchemaOptions LoadOptionsFromMetadata(IReadOnlyDictionary<string, string?> entrypointMetadata, IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		entrypointMetadata.TryGetValue(propConfig, out var optionsFiles);
		entrypointMetadata.TryGetValue(propPathPrefix, out var pathPrefix);
		var defaultJson = CSharpSchemaOptions.DefaultOptionsJson.Value;
		var serverJson = CSharpServerSchemaOptions.ServerDefaultOptionsJson.Value;

		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>(
			[
				defaultJson,
				serverJson,
				.. additionalSchemas.Where(s => s.Types.Contains(typeConfig)).Select(f => OptionsLoader.LoadYaml(f.Contents)),
				.. optionsFiles is { Length: > 0 } s
					? s.Split(';').Select(OptionsLoader.LoadYamlFromFile)
					: []
			]
		);

		if (pathPrefix != null)
			result.PathPrefix = pathPrefix;

		result.DefaultNamespace = GetStandardNamespace(entrypointMetadata, result);
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
		return $"{typeof(CSharpGenerator).FullName} v{typeof(CSharpGenerator).Assembly.GetName().Version}";
	}

	private static string GetStandardNamespace(IReadOnlyDictionary<string, string?> metadata, CSharpSchemaOptions options)
	{
		if (metadata.TryGetValue(propNamespace, out var fullNamespace)) return fullNamespace!;
		metadata.TryGetValue(propIdentity, out var identity);
		metadata.TryGetValue(propLink, out var link);
		metadata.TryGetValue("build_property.projectdir", out var projectDir);
		metadata.TryGetValue("build_property.rootnamespace", out var rootNamespace);

		return CSharpNaming.ToNamespace(rootNamespace, projectDir, identity, link, options.ReservedIdentifiers());
	}

	private static Uri ToInternalUri(AdditionalTextInfo document) =>
		document.Metadata.TryGetValue(propSchemaId, out var schemaId) && schemaId is { Length: > 0 } ? new Uri(schemaId) :
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
				.ToArray(),
			OpenApiTransforms.Matchers
		);
}
