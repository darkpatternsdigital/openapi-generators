using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Commands.Manifest;

public class ManifestFileProperties
{
	[YamlMember(Alias = "input")] public required List<ManifestInputOptions> Input { get; set; }


	[YamlMember(Alias = "gitignore")] public bool Gitignore { get; set; } = true;
	[YamlMember(Alias = "clean")] public bool Clean { get; set; }

	// TODO: consider moving these into the input options
	[YamlMember(Alias = "optionsPath")] public required string OptionsPath { get; set; }
	[YamlMember(Alias = "outputPath")] public required string OutputPath { get; set; }

}

public class ManifestInputOptions
{
	[YamlMember(Alias = "path")] public required string Path { get; set; }
}