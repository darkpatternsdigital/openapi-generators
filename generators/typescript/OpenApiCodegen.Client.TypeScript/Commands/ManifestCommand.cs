using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApi.TypeScript;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Commands;

[Verb("manifest", HelpText = "Generate TypeScript from a manifest specifying one or more source OpenAPI files")]
public class ManifestOptions
{
	[Value(0, MetaName = "input-manifest", Required = true, HelpText = "Path to the manifest listing the OpenAPI files to convert")]
	public required string InputPath { get; set; }

}

internal class ManifestCommand : ICommandBase<ManifestOptions>
{
	public Task<int> Run(ManifestOptions opts)
	{
		var manifest = LoadManifest(opts.InputPath);
		manifest.BaseDirectory = Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), opts.InputPath))!;

		var outputPath = manifest.DetermineOutputPath();
		if (outputPath == null || (!outputPath.StartsWith(Directory.GetCurrentDirectory()) && !outputPath.StartsWith(manifest.BaseDirectory)))
		{
			Console.Error.WriteLine("A valid `outputPath` is required in the manifest, which must be either within the current directory or the manifest's directory.");
			return Task.FromResult(1);
		}

		var documentRegistry = new DocumentRegistry(manifest.ToRegistryOptions());
		var registry = new SchemaRegistry(documentRegistry);

		var parseResults = manifest.GetBaseDocuments()
			.Select(fileUri => documentRegistry.ResolveDocument(fileUri, relativeDocument: null))
			.Select(baseDocument => CommonParsers.DefaultParsers.Parse(baseDocument, registry))
			.ToArray();

		foreach (var d in parseResults.SelectMany(r => r.Diagnostics).Distinct())
			Console.Error.WriteLine(TypeScriptSourceFileUtils.ToDiagnosticMessage(d));

		var options = manifest.GetTypeScriptSchemaOptions();
		var sourceProviders = new List<Func<TransformSettings, ISourceProvider>>();
		sourceProviders.AddRange(parseResults
				.Select(x => x.Result)
				.OfType<OpenApiDocument>()
				.Select<OpenApiDocument, Func<TransformSettings, ISourceProvider>>(document =>
					s => new OperationTransformerFactory(s).Build(document, options)
				));
		sourceProviders.Add(s => new TypeScriptSchemaSourceProvider(s, options));
		sourceProviders.Add(_ => new GitIgnoreSourceProvider());
		var transformer = TransformSettings.BuildComposite(registry, Program.GetVersionInfo(), sourceProviders.ToArray());

		var sourcesResult = transformer.GetSources();
		foreach (var error in sourcesResult.Diagnostics.Distinct())
		{
			var formattedText = string.Format(CommonDiagnostics.ResourceManager.GetString(error.GetType().FullName!)!, [.. error.GetTextArguments()]);
			Console.Error.WriteLine(
				$"{null}{"DPDOPENAPI000"}: {null} {error.Location.RetrievalUri.LocalPath}({error.Location.Range?.Start.Line ?? 0},{error.Location.Range?.Start.Column ?? 0}-{error.Location.Range?.Start.Line ?? 0},{error.Location.Range?.Start.Column ?? 0}) {formattedText}"
			);
		}
		TypeScriptSourceFileUtils.WriteSources(outputPath, sourcesResult.Sources, manifest.RemoveOutdated);

		return Task.FromResult(
			parseResults.Any(x => x.Result == null)	? 2 : 0
		);
	}

	private OpenApiManifest LoadManifest(string inputPath)
	{
		using var streamReader = new StreamReader(inputPath);
		var deserializer = new YamlDotNet.Serialization.Deserializer();
		return deserializer.Deserialize<OpenApiManifest>(streamReader);
	}
}
