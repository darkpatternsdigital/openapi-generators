using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace DarkPatterns.OpenApiCodegen;

internal static class BuiltGenerator
{
	public static IIncrementalGenerator Create()
	{
		// The analyzer embeds the generator's dependencies as resources, so load the built analyzer
		// rather than compiling its source into this test. Its output path mirrors this project's.
		var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
		var analyzerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
			"OpenApiCodegen.CSharp.Roslyn4.1", configuration, "DarkPatterns.OpenApiCodegen.CSharp.Roslyn4_1.dll"));
		var generatorType = Assembly.LoadFrom(analyzerPath).GetType("DarkPatterns.OpenApiCodegen.CSharp.OpenApiCSharpGenerator", throwOnError: true)!;
		return (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
	}
}
