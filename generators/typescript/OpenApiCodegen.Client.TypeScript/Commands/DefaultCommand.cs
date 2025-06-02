using System;
using System.Collections.Generic;
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

		var documentRegistry = new DocumentRegistry(ToResolverOptions(options, [
			ToResolver(inputPath),
			DocumentResolverFactory.RelativePathResolver
		]));
		var baseDocument = documentRegistry.ResolveDocument(
			ToInternalUri(Path.Combine(Directory.GetCurrentDirectory(), inputPath)),
			relativeDocument: null
		);
		var registry = new SchemaRegistry(documentRegistry);
		var parseResult = CommonParsers.DefaultParsers.Parse(baseDocument, registry);
		if (parseResult.Diagnostics.Count > 0)
		{
			foreach (var d in parseResult.Diagnostics)
				Console.Error.WriteLine(ToDiagnosticMessage(d));
			return Task.FromResult(3);
		}

		if (parseResult.Result is not { } document)
			return Task.FromResult(2);

		var transformer = TransformSettings.BuildComposite(registry, Program.GetVersionInfo(), [
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
		if (clean)
			TypeScriptSourceFileUtils.Clean(outputPath);
		TypeScriptSourceFileUtils.WriteSource(outputPath, excludeGitignore, sourcesResult.Sources);

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

	private static DocumentResolver ToResolver(string documentPath)
	{
		return DocumentResolverFactory.LoadAs(
				ToInternalUri(Path.Combine(Directory.GetCurrentDirectory(), documentPath)),
				File.ReadAllText(documentPath)
			);
	}

	private static DocumentRegistryOptions ToResolverOptions(TypeScriptSchemaOptions options, IReadOnlyList<DocumentResolver> resolvers) =>
		new DocumentRegistryOptions(resolvers, OpenApiTransforms.Matchers);

	private static TypeScriptSchemaOptions LoadOptions(string? optionsPath)
	{
		using var defaultJsonStream = TypeScriptSchemaOptions.GetDefaultOptionsJson();
		return OptionsLoader.LoadOptions<TypeScriptSchemaOptions>([defaultJsonStream], optionsPath is { Length: > 0 } ? [optionsPath] : []);
	}
}

