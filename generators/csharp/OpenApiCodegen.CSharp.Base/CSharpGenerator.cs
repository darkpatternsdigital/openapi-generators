using DarkPatterns.OpenApi.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;
using DarkPatterns.OpenApiCodegen.CSharp.Client;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using DarkPatterns.OpenApiCodegen.CSharp.WebhookClient;
using Microsoft.Extensions.FileSystemGlobbing;

namespace DarkPatterns.OpenApiCodegen.CSharp;

public class CSharpGenerator : IOpenApiCodeGenerator
{
	const string propNamespace = "Namespace";
	const string propConfig = "Configuration";
	const string propIdentity = "identity";
	const string propLink = "link";
	const string propPathPrefix = "pathPrefix";
	const string propSchemaId = "schemaId";
	const string propProjectDir = "build_property.projectdir";

	const string typeMvcServer = "MvcServer";
	const string typeClient = "Client";
	const string typeWebhookClient = "WebhookClient";
	const string typeModels = "Models";
	const string typeConfig = "Config";
	const string sharedSourceGroup = "JsonSchema";

	// Each output's own metadata (`DPD{Output}{Name}`), applied over the merged metadata for that output
	private static readonly (string Output, string ItemType)[] outputItemTypes =
	[
		(typeMvcServer, "OpenApiSchemaMvcServer"),
		(typeClient, "OpenApiSchemaClient"),
		(typeWebhookClient, "OpenApiSchemaWebhookClient"),
	];
	private static readonly (string Key, string Name)[] outputMetadataNames =
	[
		(propNamespace, "Namespace"),
		(propLink, "Link"),
		(propPathPrefix, "PathPrefix"),
		(propConfig, "Configuration"),
	];

	private readonly IEnumerable<string> metadataKeys =
	[
		propNamespace,
		propConfig,
		propIdentity,
		propLink,
		propPathPrefix,
		propSchemaId,
		propProjectDir,
		.. from output in outputItemTypes
		   from name in outputMetadataNames
		   select ToOutputMetadataKey(output.Output, name.Name),
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
		var settings = new TransformSettings(schemaRegistry, GetVersionInfo(), GetProjectDirectory(additionalTextInfos));

		var docs = (from document in additionalTextInfos
					let loaded = registry.ResolveDocument(ToInternalUri(document), relativeDocument: null)
					select new { document, loaded }).ToArray();
		var metadataDiagnostics = new List<DiagnosticBase>();

		var mvcServerTransforms =
			(from e in docs
			 where e.document.Types.Contains(typeMvcServer)
			 let options = LoadOutputOptions(typeMvcServer, e.document, additionalTextInfos, metadataDiagnostics)
			 where options != null
			 let parseResult = CommonParsers.DefaultParsers.Parse(e.loaded, schemaRegistry)
			 select new PathControllerTransformerFactory(settings).Build(parseResult, options)).ToArray();
		var clientTransforms =
			(from e in docs
			 where e.document.Types.Contains(typeClient)
			 let options = LoadOutputOptions(typeClient, e.document, additionalTextInfos, metadataDiagnostics)
			 where options != null
			 let parseResult = CommonParsers.DefaultParsers.Parse(e.loaded, schemaRegistry)
			 select new ClientTransformerFactory(settings).Build(parseResult, options)).ToArray();
		var webhookTransforms =
			(from e in docs
			 where e.document.Types.Contains(typeWebhookClient)
			 let options = LoadOutputOptions(typeWebhookClient, e.document, additionalTextInfos, metadataDiagnostics)
			 where options != null
			 let parseResult = CommonParsers.DefaultParsers.Parse(e.loaded, schemaRegistry)
			 select new WebhookClientTransformerFactory(settings).Build(parseResult, options)).ToArray();
		var modelsDiagnostics =
			(from e in docs
			 where e.document.Types.Contains(typeModels)
			 select RegisterModels(e.loaded, CommonParsers.DefaultParsers.Parse(e.loaded, schemaRegistry), schemaRegistry)).ToArray();

		var allSchemasOptions = LoadOptionsFromMetadata(additionalTextInfos);

		var sourceProvider = new CompositeOpenApiSourceProvider([
			.. mvcServerTransforms,
			.. clientTransforms,
			.. webhookTransforms,
			.. modelsDiagnostics,
			new DiagnosticOnlySourceProvider(metadataDiagnostics),
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
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		using var serverJsonStream = CSharpServerSchemaOptions.GetServerDefaultOptionsJson();

		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>(
			[
				defaultJsonStream,
				serverJsonStream,
				.. additionalSchemas.Where(s => s.Types.Contains(typeConfig)).Select(f => new MemoryStream(Encoding.UTF8.GetBytes(f.Contents))),
			],
			[]
		);

		foreach (var entry in additionalSchemas)
		{
			var ns = GetStandardNamespace(entry.Metadata, result);
			if (result.DefaultNamespace != ns)
				result.NamespacesBySchema[ToInternalUri(entry)] = ns;
		}

		return result;
	}

	private static string ToOutputMetadataKey(string output, string name) => "DPD" + output + name;

	/// <summary>
	/// Loads the options for one output of a file. Without per-output metadata (such as when the
	/// generator isn't run through the package's props), the merged metadata is used.
	/// </summary>
	/// <returns>null when the file is listed as the same output more than once with different metadata</returns>
	private static CSharpServerSchemaOptions? LoadOutputOptions(string output, AdditionalTextInfo document, IEnumerable<AdditionalTextInfo> additionalSchemas, List<DiagnosticBase> diagnostics)
	{
		var itemType = outputItemTypes.Single(t => t.Output == output).ItemType;
		var metadata = new Dictionary<string, string?>();
		foreach (var entry in document.Metadata)
			metadata[entry.Key] = entry.Value;

		var conflicting = false;
		foreach (var (key, name) in outputMetadataNames)
		{
			if (!document.Metadata.TryGetValue(ToOutputMetadataKey(output, name), out var value) || value == null)
				continue;
			// The props join differing values with `|`; several Configuration files are all applied
			if (key == propConfig)
				value = value.Replace('|', ';');
			else if (value.Contains('|'))
			{
				diagnostics.Add(new ConflictingOutputMetadataDiagnostic(itemType, name, value.Replace('|', ';'), new Location(ToInternalUri(document))));
				conflicting = true;
			}
			metadata[key] = value;
		}

		return conflicting ? null : LoadOptionsFromMetadata(metadata, additionalSchemas);
	}

	private static CSharpServerSchemaOptions LoadOptionsFromMetadata(IReadOnlyDictionary<string, string?> entrypointMetadata, IEnumerable<AdditionalTextInfo> additionalSchemas)
	{
		entrypointMetadata.TryGetValue(propConfig, out var optionsFiles);
		entrypointMetadata.TryGetValue(propPathPrefix, out var pathPrefix);
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		using var serverJsonStream = CSharpServerSchemaOptions.GetServerDefaultOptionsJson();

		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>(
			[
				defaultJsonStream,
				serverJsonStream,
				.. additionalSchemas.Where(s => s.Types.Contains(typeConfig)).Select(f => new MemoryStream(Encoding.UTF8.GetBytes(f.Contents))),
			],
			optionsFiles is { Length: > 0 } s ? s.Split(';') : []
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

	/// <summary>
	/// Models output: parsing the document as OpenAPI gives its schemas the same names as in a server
	/// document. Each named schema (<c>#/components/schemas/*</c>) is registered so that
	/// <see cref="CSharpSchemaSourceProvider"/> generates it, even though no operation references it.
	/// </summary>
	private static DiagnosticOnlySourceProvider RegisterModels(IDocumentReference loaded, ParseResult<OpenApiDocument> parseResult, SchemaRegistry schemaRegistry)
	{
		if (parseResult.Result is { } document && loaded.RootNode?["components"]?["schemas"] is JsonObject schemas)
		{
			foreach (var name in schemas.Select(entry => entry.Key))
			{
				var id = new Uri(document.Id, "#/components/schemas/" + name.Replace("~", "~0").Replace("/", "~1"));
				if (schemaRegistry.ResolveSchema(new NodeMetadata(id), document.Dialect) is { } schema)
					schemaRegistry.EnsureSchemaRegistered(schema);
			}
			// Parsing only fixed up the nodes known at the time; complete the newly resolved schemas
			return new DiagnosticOnlySourceProvider([.. parseResult.Diagnostics, .. schemaRegistry.RecursivelyFixupAll()]);
		}
		return new DiagnosticOnlySourceProvider(parseResult.Diagnostics);
	}

	private static Uri? GetProjectDirectory(IEnumerable<AdditionalTextInfo> additionalTextInfos)
	{
		// ProjectDir is a global build property, so every file has the same value
		var projectDir = additionalTextInfos
			.Select(doc => doc.Metadata.TryGetValue(propProjectDir, out var dir) ? dir : null)
			.FirstOrDefault(dir => dir is { Length: > 0 });
		if (projectDir == null) return null;
		if (!projectDir.EndsWith("/") && !projectDir.EndsWith("\\"))
			projectDir += Path.DirectorySeparatorChar;
		return new Uri(projectDir);
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
