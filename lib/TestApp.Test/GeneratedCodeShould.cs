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
			.AddScrubber(ScrubMachineSpecifics);
	}

	private static void ScrubMachineSpecifics(StringBuilder text)
	{
		// Schemas without a SchemaId are identified by their absolute file URI. Named schemas use
		// `file:///C:/...` but inline schemas currently use `file://C:/...`, and Linux paths differ,
		// so match any number of slashes.
		var repoRootPath = new Uri(RepoRoot + Path.DirectorySeparatorChar).AbsolutePath.TrimStart('/');
		var repoRootUri = new Regex("file:/*" + Regex.Escape(repoRootPath), RegexOptions.IgnoreCase);
		var result = repoRootUri.Replace(text.ToString(), "{RepoRoot}/");
		// The generator version changes with every release
		result = GeneratorVersion().Replace(result, " v{Version})");
		text.Clear().Append(result);
	}

	[GeneratedRegex(@" v\d+(\.\d+){3}\)")]
	private static partial Regex GeneratorVersion();

	private static string ThisDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
