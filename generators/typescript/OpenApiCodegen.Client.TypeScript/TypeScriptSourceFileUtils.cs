using System;
using System.Collections.Generic;
using System.IO;
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
	}

	public static void Clean(string outputPath)
	{
		if (System.IO.Directory.Exists(outputPath))
		{
			foreach (var entry in System.IO.Directory.GetFiles(outputPath))
				System.IO.File.Delete(entry);
			foreach (var entry in System.IO.Directory.GetDirectories(outputPath))
				System.IO.Directory.Delete(entry, true);
		}
	}

	public static TypeScriptSchemaOptions LoadOptions(string? optionsPath)
	{
		using var defaultJsonStream = TypeScriptSchemaOptions.GetDefaultOptionsJson();
		return OptionsLoader.LoadOptions<TypeScriptSchemaOptions>([defaultJsonStream], optionsPath is { Length: > 0 } ? [optionsPath] : []);
	}
}
