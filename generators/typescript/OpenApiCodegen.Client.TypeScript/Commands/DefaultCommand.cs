using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApi.TypeScript;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Commands;

[Verb("single-file", isDefault: true, HelpText = "Get information about your current environment")]
public class DefaultOptions
{
	[Option('o', "options", HelpText = "Path to the options file")]
	public string? OptionsPath { get; set; }

	[Option('x', "exclude-gitignore", HelpText = "Do not emit gitignore file")]
	public bool ExcludeGitignore { get; set; }

	[Option('c', "clean", HelpText = "Clean output path before generating")]
	public bool Clean { get; set; }

	[Value(0, MetaName = "input-openapi-document", Required = true, HelpText = "Path to the Open API document to convert")]
	public required string InputPath { get; set; }

	[Value(1, MetaName = "output-path", Required = true, HelpText = "Path under which to generate the TypeScript files")]
	public required string OutputPath { get; set; }

}

internal class DefaultCommand : ICommandBase<DefaultOptions>
{
	public Task<int> Run(DefaultOptions opts)
	{
		var inputPath = opts.InputPath;
		var outputPath = opts.OutputPath;
		var optionsPath = opts.OptionsPath;
		var excludeGitignore = opts.ExcludeGitignore;
		var clean = opts.Clean;

		var options = LoadOptions(optionsPath);

		var (baseDocument, registry) = LoadDocument(inputPath, options);
		var parseResult = CommonParsers.DefaultParsers.Parse(baseDocument, registry);
		if (parseResult.Diagnostics.Count > 0)
		{
			foreach (var d in parseResult.Diagnostics)
				Console.Error.WriteLine(ToDiagnosticMessage(d));
			return Task.FromResult(3);
		}

		if (parseResult.Result is not { } document)
			return Task.FromResult(2);

		var transformer = TransformSettings.BuildComposite(registry, GetVersionInfo(), [
			(s) => new OperationTransformerFactory(s).Build(document, options),
					(s) => new TypeScriptSchemaSourceProvider(s, options)
		]);

		var sourcesResult = transformer.GetSources();
		foreach (var error in sourcesResult.Diagnostics.Distinct())
		{
			var formattedText = string.Format(CommonDiagnostics.ResourceManager.GetString(error.GetType().FullName!)!, [.. error.GetTextArguments()]);
			Console.Error.WriteLine(
				$"{null}{"DPDOPENAPI000"}: {null} {error.Location.RetrievalUri.LocalPath}({error.Location.Range?.Start.Line ?? 0},{error.Location.Range?.Start.Column ?? 0}-{error.Location.Range?.Start.Line ?? 0},{error.Location.Range?.Start.Column ?? 0}) {formattedText}"
			);
		}
		if (clean && System.IO.Directory.Exists(outputPath))
		{
			foreach (var entry in System.IO.Directory.GetFiles(outputPath))
				System.IO.File.Delete(entry);
			foreach (var entry in System.IO.Directory.GetDirectories(outputPath))
				System.IO.Directory.Delete(entry, true);
		}
		foreach (var entry in sourcesResult.Sources)
		{
			var path = System.IO.Path.Combine(outputPath, entry.Key);
			if (System.IO.Path.GetDirectoryName(path) is string dir)
				System.IO.Directory.CreateDirectory(dir);
			System.IO.File.WriteAllText(path, entry.SourceText);
		}
		if (!excludeGitignore)
		{
			var path = System.IO.Path.Combine(outputPath, ".gitignore");
			System.IO.File.WriteAllText(path, "*");
		}

		return Task.FromResult(0);
	}

	private static string ToDiagnosticMessage(DiagnosticBase d)
	{
		var position = d.Location.Range is FileLocationRange { Start: var start }
			? $"({start.Line},{start.Column})"
			: "";
		var messageFormat = CommonDiagnostics.ResourceManager.GetString(d.GetType().FullName!)!;
		var message = string.Format(messageFormat, d.GetTextArguments().ToArray());
		return $"{d.Location.RetrievalUri.LocalPath}{position}: {message}";
	}

	private static Uri ToInternalUri(string documentPath) =>
		new Uri(new Uri(documentPath).AbsoluteUri);

	private static (IDocumentReference, SchemaRegistry) LoadDocument(string documentPath, TypeScriptSchemaOptions options)
	{
		return DocumentResolverFactory.FromInitialDocumentInMemory(
			ToInternalUri(Path.Combine(Directory.GetCurrentDirectory(), documentPath)),
			File.ReadAllText(documentPath),
			ToResolverOptions(options)
		);
	}

	private static DocumentRegistryOptions ToResolverOptions(TypeScriptSchemaOptions options) =>
		new DocumentRegistryOptions([
		// TODO: use the `options` to determine how to resolve additional documents
		], OpenApiTransforms.Matchers);

	private static string GetVersionInfo()
	{
		return $"{typeof(Program).Namespace} v{typeof(Program).Assembly.GetName().Version}";
	}

	private static TypeScriptSchemaOptions LoadOptions(string? optionsPath)
	{
		using var defaultJsonStream = TypeScriptSchemaOptions.GetDefaultOptionsJson();
		return OptionsLoader.LoadOptions<TypeScriptSchemaOptions>([defaultJsonStream], optionsPath is { Length: > 0 } ? [optionsPath] : []);
	}
}

