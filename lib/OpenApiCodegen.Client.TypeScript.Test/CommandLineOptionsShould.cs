using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

/// <summary>
/// Compatibility: real-world builds pass CLI options that must keep working (planning #11).
/// </summary>
public class CommandLineOptionsShould
{
	[Fact]
	public async Task Ignore_an_options_path_that_does_not_exist()
	{
		var input = Path.Combine(SolutionConfiguration.SolutionRoot, "schemas", "petstore.yaml");
		var root = Directory.CreateTempSubdirectory().FullName;
		try
		{
			var withoutOptions = Path.Combine(root, "without-options");
			var withMissingOptions = Path.Combine(root, "with-missing-options");

			Assert.Equal(0, await RunCli(input, withoutOptions));
			Assert.Equal(0, await RunCli(input, withMissingOptions, "-o", Path.Combine(root, "does-not-exist.yaml")));

			var expected = ReadAllFiles(withoutOptions);
			Assert.NotEmpty(expected);
			Assert.Equal(expected, ReadAllFiles(withMissingOptions));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public async Task Report_parse_diagnostics_without_crashing()
	{
		var input = Path.Combine(SolutionConfiguration.SolutionRoot, "schemas", "oas-parser-invalid-parameter-in.yml");
		var output = Directory.CreateTempSubdirectory().FullName;
		try
		{
			var (exitCode, errors) = await ExecuteCli(input, output);

			// The CLI doesn't return its exit code yet, so it is 0 rather than 3; it must not crash, though
			Assert.Equal(0, exitCode);
			Assert.Contains("oas-parser-invalid-parameter-in.yml(", errors);
			Assert.DoesNotContain("Unhandled exception", errors);
		}
		finally
		{
			Directory.Delete(output, recursive: true);
		}
	}

	private static async Task<int> RunCli(string input, string output, params string[] options)
	{
		var (exitCode, errors) = await ExecuteCli(input, output, options);
		Assert.Equal("", errors);
		return exitCode;
	}

	private static Task<(int ExitCode, string Errors)> ExecuteCli(string input, string output, params string[] options)
	{
		var startInfo = new ProcessStartInfo("dotnet");
		startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "DarkPatterns.OpenApiCodegen.Client.TypeScript.dll"));
		foreach (var option in options)
			startInfo.ArgumentList.Add(option);
		startInfo.ArgumentList.Add(input);
		startInfo.ArgumentList.Add(output);

		return ProcessUtility.ExecuteProcess(startInfo, (process, ct) => process.StandardError.ReadToEndAsync(ct));
	}

	private static (string Path, string Contents)[] ReadAllFiles(string directory) =>
		Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
			.Select(file => (Path.GetRelativePath(directory, file), File.ReadAllText(file)))
			.OrderBy(file => file.Item1, StringComparer.Ordinal)
			.ToArray();
}
