using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DarkPatterns.OpenApiCodegen;

/// <summary>
/// The C# generator's output uses DarkPatterns.OpenApiCodegen.Json.Extensions, which consumers
/// reference themselves, so the generator checks that reference.
/// </summary>
public class JsonExtensionsReferenceShould
{
	private const string JsonExtensionsAssemblyName = "DarkPatterns.OpenApiCodegen.Json.Extensions";

	[Theory]
	[InlineData(null, "DPDAPI001")]
	[InlineData("0.19.0.0", "DPDAPI002")]
	[InlineData("0.20.0.0", null)]
	[InlineData("1.0.0.0", null)]
	public void Report_a_missing_or_outdated_reference(string? referencedVersion, string? expectedDiagnosticId)
	{
		MetadataReference[] references = [
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			.. referencedVersion == null ? Array.Empty<MetadataReference>() : [CreateJsonExtensionsReference(referencedVersion)],
		];
		var compilation = CSharpCompilation.Create("Consumer", references: references);

		var result = CSharpGeneratorDriver.Create(BuiltGenerator.Create()).RunGenerators(compilation).GetRunResult();

		var actual = result.Diagnostics.Select(d => d.Id).Where(id => id.StartsWith("DPDAPI")).ToArray();
		Assert.Equal(expectedDiagnosticId == null ? [] : [expectedDiagnosticId], actual);
	}

	private static MetadataReference CreateJsonExtensionsReference(string version)
	{
		var library = CSharpCompilation.Create(
			JsonExtensionsAssemblyName,
			[CSharpSyntaxTree.ParseText($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]")],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
		using var stream = new MemoryStream();
		var emitResult = library.Emit(stream);
		Assert.True(emitResult.Success);
		return MetadataReference.CreateFromImage(stream.ToArray());
	}
}
