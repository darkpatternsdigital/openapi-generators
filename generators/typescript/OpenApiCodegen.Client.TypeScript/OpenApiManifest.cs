using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.TypeScript;
using YamlDotNet.Serialization;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

class OpenApiManifest
{
	private string? baseDirectory;
	public string BaseDirectory
	{
		get => baseDirectory ?? Directory.GetCurrentDirectory();
		set => baseDirectory = value;
	}

	[YamlMember(Alias = "outputPath")]
	public string? OutputPath { get; set; }

	[YamlMember(Alias = "removeOutdated")]
	public bool RemoveOutdated { get; set; } = false;

	[YamlMember(Alias = "inputs")]
	public OpenApiInput[] Inputs { get; set; } = [];

	internal IEnumerable<Uri> GetBaseDocuments()
	{
		return Inputs.Select(input => input.GetInputUri(BaseDirectory));
	}

	internal DocumentRegistryOptions ToRegistryOptions()
	{
		return new DocumentRegistryOptions(
			[
				.. Inputs.Select(x => x.ToResolver(BaseDirectory)),
				DocumentResolverFactory.RelativePathResolver
			],
			OpenApiTransforms.Matchers
		);
	}

	internal TypeScriptSchemaOptions GetTypeScriptSchemaOptions()
	{
		// TODO: load configuration options
		return TypeScriptSourceFileUtils.LoadOptions(null);
	}

	internal string? DetermineOutputPath()
	{
		return OutputPath == null
			? null
			: Path.Combine(BaseDirectory, OutputPath);
	}
}

class OpenApiInput
{
	[YamlMember(Alias = "path")]
	public string? Path { get; set; }
	// TODO: more options

	public string GetDocumentPath(string baseDirectory) => System.IO.Path.Combine(baseDirectory, Path ?? "api.yaml");
	public Uri GetInputUri(string baseDirectory) => TypeScriptSourceFileUtils.ToFileUri(GetDocumentPath(baseDirectory));
	public DocumentResolver ToResolver(string baseDirectory)
	{
		return DocumentResolverFactory.LoadAs(
			GetInputUri(baseDirectory),
			File.ReadAllText(GetDocumentPath(baseDirectory))
		);
	}
}