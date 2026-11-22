using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

/// <summary>
/// Snapshots everything the source generator emits for TestApp (EmitCompilerGeneratedFiles),
/// so changes to generated code show up as diffs in review.
/// </summary>
public partial class GeneratedCodeShould
{
	private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(ThisDirectory(), "..", ".."));

	[Fact]
	public Task MatchSnapshots()
	{
		var generatorOutput = Directory.GetDirectories(
			Path.Combine(RepoRoot, "lib", "TestApp", "generated"),
			"DarkPatterns.OpenApiCodegen.CSharp.OpenApiCSharpGenerator",
			SearchOption.AllDirectories
		).Single();

		return Verifier.VerifyDirectory(generatorOutput)
			.UseDirectory("Snapshots")
			.AddScrubber(ScrubGeneratorVersion);
	}

	// The generator version changes with every release
	private static void ScrubGeneratorVersion(StringBuilder text)
	{
		var result = GeneratorVersion().Replace(text.ToString(), " v{Version})");
		text.Clear().Append(result);
	}

	[GeneratedRegex(@" v\d+(\.\d+){3}\)")]
	private static partial Regex GeneratorVersion();

	private static string ThisDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
