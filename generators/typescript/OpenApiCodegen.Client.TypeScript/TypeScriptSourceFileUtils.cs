using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.OpenApi.TypeScript;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

internal static class TypeScriptSourceFileUtils
{
	public static Uri ToFileUri(string filePath) =>
		new Uri(Path.Combine(Directory.GetCurrentDirectory(), filePath));

	public static void WriteSource(string outputPath, bool excludeGitignore, IReadOnlyList<SourceEntry> sources)
	{
		foreach (var entry in sources)
		{
			var path = Path.Combine(outputPath, entry.Key);
			if (Path.GetDirectoryName(path) is string dir)
				Directory.CreateDirectory(dir);
			File.WriteAllText(path, entry.SourceText);
		}
		if (!excludeGitignore)
		{
			var path = Path.Combine(outputPath, ".gitignore");
			File.WriteAllText(path, "*");
		}
	}

	public static void Clean(string outputPath)
	{
		if (Directory.Exists(outputPath))
		{
			foreach (var entry in Directory.GetFiles(outputPath))
				File.Delete(entry);
			foreach (var entry in Directory.GetDirectories(outputPath))
				Directory.Delete(entry, true);
		}
	}

	public static void WriteSources(string outputPath, IReadOnlyList<SourceEntry> sources, bool removeOutdated)
	{
		if (!Directory.Exists(outputPath))
			Directory.CreateDirectory(outputPath);
		var existingFiles = Directory.GetFiles(outputPath, "*", enumerationOptions: new EnumerationOptions { RecurseSubdirectories = true });
		var existingDirectories = Directory.GetDirectories(outputPath, "*", SearchOption.AllDirectories);
		foreach (var entry in sources)
		{
			var path = Path.Combine(outputPath, entry.Key);
			if (Path.GetDirectoryName(path) is string dir)
				Directory.CreateDirectory(dir);
			File.WriteAllText(path, entry.SourceText);
		}
		if (removeOutdated)
		{
			foreach (var outdated in existingFiles.Except(sources.Select(x => Path.Combine(outputPath, x.Key))))
			{
				File.Delete(outdated);
			}
			foreach (var outdated in existingDirectories.Where(x => !sources.Any(s => Path.Combine(outputPath, s.Key).StartsWith(x))))
			{
				Directory.Delete(outdated);
			}
		}
	}

	public static TypeScriptSchemaOptions LoadOptions(string? optionsPath)
	{
		var defaultJson = TypeScriptSchemaOptions.DefaultOptionsJson.Value;
		return OptionsLoader.LoadOptions<TypeScriptSchemaOptions>([
			defaultJson,
			.. optionsPath is { Length: > 0 } ? new[] { OptionsLoader.LoadYamlFromFile(optionsPath) } : [],
		]);
	}

	public static string ToDiagnosticMessage(DiagnosticBase d)
	{
		var position = d.Location.Range is FileLocationRange { Start: var start }
			? $"({start.Line},{start.Column})"
			: "";
		var messageFormat = CommonDiagnostics.ResourceManager.GetString(d.GetType().FullName!)!;
		var message = string.Format(messageFormat, d.GetTextArguments().ToArray());
		return $"{d.Location.RetrievalUri.LocalPath}{position}: {message}";
	}

}
