using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.TypeScript;
using System;
using System.IO;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript
{
	class Program
	{
		static void Main(string[] args)
		{
			var commandLineApplication = new CommandLineApplication(true);
			commandLineApplication.Description = GetVersionInfo();
			commandLineApplication.HelpOption("-? | -h | --help");
			commandLineApplication.Option("-o | --options", "Path to the options file", CommandOptionType.SingleValue);
			commandLineApplication.Option("-x | --exclude-gitignore", "Do not emit gitignore file", CommandOptionType.NoValue);
			commandLineApplication.Option("-c | --clean", "Clean output path before generating", CommandOptionType.NoValue);
			commandLineApplication.Argument("input-openapi-document", "Path to the Open API document to convert");
			commandLineApplication.Argument("output-path", "Path under which to generate the TypeScript files");
			commandLineApplication.OnExecute(() =>
			{
				if (commandLineApplication.Arguments.Any(arg => arg.Value == null))
				{
					commandLineApplication.ShowHelp();
					return 1;
				}

				var inputPath = commandLineApplication.Arguments.Single(arg => arg.Name == "input-openapi-document").Value;
				var outputPath = commandLineApplication.Arguments.Single(arg => arg.Name == "output-path").Value;
				var optionsPath = commandLineApplication.Options.Find(opt => opt.LongName == "options")?.Value();
				var excludeGitignore = commandLineApplication.Options.Find(opt => opt.LongName == "exclude-gitignore")?.HasValue() ?? false;
				var clean = commandLineApplication.Options.Find(opt => opt.LongName == "clean")?.HasValue() ?? false;

				var options = LoadOptions(optionsPath);

				var (baseDocument, registry) = LoadDocument(inputPath, options);
				var parseResult = CommonParsers.DefaultParsers.Parse(baseDocument, registry);
				if (parseResult.Diagnostics.Count > 0)
				{
					foreach (var d in parseResult.Diagnostics)
						Console.Error.WriteLine(ToDiagnosticMessage(d));
					return 3;
				}

				if (parseResult.Document == null)
					return 2;

				var transformer = parseResult.Document.BuildTypeScriptOperationSourceProvider(registry, GetVersionInfo(), options);

				var diagnostic = new OpenApiTransformDiagnostic();
				var entries = transformer.GetSources(diagnostic).ToArray();
				foreach (var error in diagnostic.Diagnostics)
				{
#pragma warning disable CA2241 // CommandLineApplication does not follow standard format string format
					commandLineApplication.Error.WriteLine(
						"{subcategory}{errorCode}: {helpKeyword} {file}({lineNumber},{columnNumber}-{endLineNumber},{endColumnNumber}) {message}",
						null, "DPDOPENAPI000", null, error.Location.RetrievalUri.LocalPath, error.Location.Range?.Start.Line ?? 0, error.Location.Range?.Start.Column ?? 0, error.Location.Range?.End.Line ?? 0, error.Location.Range?.End.Column ?? 0,
						string.Format(CommonDiagnostics.ResourceManager.GetString(error.GetType().FullName!)!, error.GetTextArguments())
					);
#pragma warning restore CA2241
				}
				if (clean && System.IO.Directory.Exists(outputPath))
				{
					foreach (var entry in System.IO.Directory.GetFiles(outputPath))
						System.IO.File.Delete(entry);
					foreach (var entry in System.IO.Directory.GetDirectories(outputPath))
						System.IO.Directory.Delete(entry, true);
				}
				foreach (var entry in entries)
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

				return 0;
			});
			commandLineApplication.Execute(args);
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

		private static (IDocumentReference, DocumentRegistry) LoadDocument(string documentPath, TypeScriptSchemaOptions options)
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
			]);

		private static string GetVersionInfo()
		{
			return $"{typeof(Program).Namespace} v{typeof(Program).Assembly.GetName().Version}";
		}

		private static TypeScriptSchemaOptions LoadOptions(string? optionsPath)
		{
			using var defaultJsonStream = TypeScriptSchemaOptions.GetDefaultOptionsJson();
			var builder = new ConfigurationBuilder();
			builder.AddYamlStream(defaultJsonStream);
			if (optionsPath is { Length: > 0 })
				builder.AddYamlFile(Path.Combine(Directory.GetCurrentDirectory(), optionsPath));
			var result = builder.Build().Get<TypeScriptSchemaOptions>()
				?? throw new InvalidOperationException("Could not construct options");
			return result;
		}
	}
}
