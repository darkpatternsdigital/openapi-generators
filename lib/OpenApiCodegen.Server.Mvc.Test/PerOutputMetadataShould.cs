using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// A file listed as several outputs (e.g. `OpenApiSchemaClient` and `OpenApiSchemaMvcServer`) gets
/// each output's metadata as `DPD{Output}*` (planning #3). The unprefixed metadata keeps the merged
/// values, which name the file's models.
/// </summary>
public class PerOutputMetadataShould
{
	[Fact]
	public void Generate_each_output_in_its_own_namespace()
	{
		var result = Generate(new()
		{
			["Namespace"] = "Shared",
			["DPDMvcServerNamespace"] = "Server",
			["DPDClientNamespace"] = "Client",
		});

		Assert.Empty(result.Diagnostics);
		var keys = result.Sources.Select(s => s.Key).ToArray();
		Assert.Contains("Server.DataControllerBase.cs", keys);
		Assert.Contains("Client.Operations.cs", keys);
		Assert.Contains("Shared.GetDataResponse.cs", keys);
		Assert.DoesNotContain(keys, k => k.StartsWith("Shared.") && (k.EndsWith("ControllerBase.cs") || k.EndsWith("Operations.cs")));
	}

	[Fact]
	public void Use_the_path_prefix_of_the_server_output()
	{
		var result = Generate(new()
		{
			["Namespace"] = "Shared",
			["pathPrefix"] = "/merged",
			["DPDMvcServerNamespace"] = "Server",
			["DPDMvcServerPathPrefix"] = "/server",
			["DPDClientNamespace"] = "Client",
			["DPDClientPathPrefix"] = "",
		});

		Assert.Empty(result.Diagnostics);
		var controller = result.Sources.Single(s => s.Key == "Server.DataControllerBase.cs").SourceText;
		Assert.Contains("\"/server/data\"", controller);
	}

	[Fact]
	public void Use_the_merged_metadata_without_per_output_metadata()
	{
		var result = Generate(new() { ["Namespace"] = "Shared" });

		Assert.Empty(result.Diagnostics);
		var keys = result.Sources.Select(s => s.Key).ToArray();
		Assert.Contains("Shared.DataControllerBase.cs", keys);
		Assert.Contains("Shared.Operations.cs", keys);
	}

	[Fact]
	public void Report_an_output_listed_twice_with_different_metadata()
	{
		var result = Generate(new()
		{
			["Namespace"] = "Shared",
			["DPDMvcServerNamespace"] = "First|Second",
			["DPDClientNamespace"] = "Client",
		});

		var diagnostic = Assert.Single(result.Diagnostics);
		Assert.Equal("DarkPatterns.OpenApiCodegen.CSharp.ConflictingOutputMetadataDiagnostic", diagnostic.Id);
		Assert.Equal(["OpenApiSchemaMvcServer", "Namespace", "First;Second"], diagnostic.Metadata);
		var keys = result.Sources.Select(s => s.Key).ToArray();
		Assert.DoesNotContain(keys, k => k.EndsWith("ControllerBase.cs"));
		Assert.Contains("Client.Operations.cs", keys);
	}

	private static GenerationResult Generate(Dictionary<string, string?> metadata)
	{
		const string schemaName = "simple-types.yaml";
		using var reader = new StreamReader(DocumentLoader.GetEmbeddedDocumentStream(new Uri(DocumentLoader.Embedded, schemaName)));
		var document = new AdditionalTextInfo(
			Path: Path.Combine(Path.GetTempPath(), schemaName),
			Contents: reader.ReadToEnd(),
			Types: ["MvcServer", "Client"],
			Metadata: metadata
		);
		return new CSharpGenerator().Generate([document]);
	}
}
