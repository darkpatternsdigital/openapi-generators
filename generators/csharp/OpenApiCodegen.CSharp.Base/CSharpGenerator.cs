using DarkPatterns.OpenApi.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;
using DarkPatterns.OpenApiCodegen.CSharp.Client;

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

		var mvcServerTransforms =
			(from document in additionalTextInfos.Where(f => f.Types.Contains(typeMvcServer))
			 let loaded = registry.ResolveDocument(ToInternalUri(document), relativeDocument: null)
			 let parseResult = CommonParsers.DefaultParsers.Parse(loaded, registry)
			 let options = LoadOptionsFromMetadata(document.Metadata, additionalTextInfos)
			 select new PathControllerTransformerFactory(settings).Build(parseResult, options)).ToArray();
		var clientTransforms =
			(from document in additionalTextInfos.Where(f => f.Types.Contains(typeClient))
			 let loaded = registry.ResolveDocument(ToInternalUri(document), relativeDocument: null)
			 let parseResult = CommonParsers.DefaultParsers.Parse(loaded, registry)
			 let options = LoadOptionsFromMetadata(document.Metadata, additionalTextInfos)
			 select new ClientTransformerFactory(settings).Build(parseResult, options)).ToArray();

		var sourceProvider = new CompositeOpenApiSourceProvider([
			.. mvcServerTransforms,
			.. clientTransforms,
			new CSharpSchemaSourceProvider(settings, LoadOptionsFromMetadata(additionalTextInfos)),
		]);

		var result = sourceProvider.GetSources();

		return new GenerationResult(
			result.Sources,
			[.. result.Diagnostics.Select(DiagnosticsConversion.GetConverter(ToPathResolver(additionalTextInfos)))]
		);
	}

	private static CSharpServerSchemaOptions LoadOptionsFromMetadata(IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		using var serverJsonStream = CSharpServerSchemaOptions.GetServerDefaultOptionsJson();
		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>([defaultJsonStream, serverJsonStream], []);

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
		var optionsFiles = entrypointMetadata[propConfig];
		var pathPrefix = entrypointMetadata[propPathPrefix];
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		using var serverJsonStream = CSharpServerSchemaOptions.GetServerDefaultOptionsJson();
		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>([defaultJsonStream, serverJsonStream], optionsFiles is { Length: > 0 } s ? s.Split(';') : []);

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
		return $"{typeof(CSharpControllerTransformer).FullName} v{typeof(CSharpControllerTransformer).Assembly.GetName().Version}";
	}

	private static string GetStandardNamespace(IReadOnlyDictionary<string, string?> metadata, CSharpSchemaOptions options)
	{
		var fullNamespace = metadata[propNamespace];
		if (fullNamespace != null) return fullNamespace;
		var identity = metadata[propIdentity];
		var link = metadata[propLink];
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
				.ToArray()
		);
}
