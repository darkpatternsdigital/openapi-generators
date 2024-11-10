using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApiCodegen;

public class DiagnosticsShould
{
	[MemberData(nameof(DiagnosticBaseNames), LanguageSpecificInclusions.CSharp)]
	[Theory]
	public void Have_roslyn_diagnostics_for_all_csharp_diagnostics(string typeName)
	{
		/// Check to see that there is a mapping of <see cref="TransformationDiagnosticAttribute" /> for every diagnostic
		Assert.True(TransformationDiagnostics.DiagnosticBy.ContainsKey(typeName), $"Missing diagnostic for '{typeName}'; add a field for it.");
	}

	[MemberData(nameof(DiagnosticByKeys))]
	[Theory]
	public void Have_no_extra_roslyn_diagnostics(string typeName)
	{
		// Remove the referenced diagnostic or rename it
		Assert.True(ChildTypesOf(typeof(DiagnosticBase), LanguageSpecificInclusions.CSharp).Any(t => t.FullName == typeName), $"Extra Diagnosic for '{typeName}'; you either need to delete it or rename it.");
	}

	[MemberData(nameof(DiagnosticBaseNames), LanguageSpecificInclusions.All)]
	[Theory]
	public void Have_translations_for_all_DiagnosticBase_subtypes(string typeName)
	{
		// Go to CommonDiagnostics and ensure the given name is a key in that XML file.
		Assert.True(CommonDiagnostics.ResourceManager.GetString(typeName) != null, $"Missing translation for '{typeName}' in CommonDiagnostics");
	}

	public static IEnumerable<object[]> DiagnosticByKeys()
	{
		return from key in TransformationDiagnostics.DiagnosticBy.Keys
			   select new object[] { key };
	}

	public static IEnumerable<object[]> DiagnosticBaseNames(LanguageSpecificInclusions mode)
	{
		return from type in ChildTypesOf(typeof(DiagnosticBase), mode)
			   select new object[] { type.FullName! };
	}

	[Flags]
	public enum LanguageSpecificInclusions : uint
	{
		CSharp = 1,
		TypeScript = 2,

		All = 0xffffffff,
	}

	private static IEnumerable<Type> ChildTypesOf(Type target, LanguageSpecificInclusions mode)
	{
		return from asm in AppDomain.CurrentDomain.GetAssemblies()
					.Concat([
						typeof(OpenApi.Transformations.Specifications.UnableToParseDiagnostic).Assembly,
						typeof(OpenApi.Specifications.v3_0.InvalidNode).Assembly,
						typeof(Json.Loaders.YamlLoadDiagnostic).Assembly,
					])
					// By referencing them in this method, the assemblies will
					// be in the CurrentDomain, so we need to exclude:

					// If you don't have C#, remove these types
					.Except(!mode.HasFlag(LanguageSpecificInclusions.CSharp) ? [
						typeof(CSharp.MvcServer.PathControllerTransformerFactory).Assembly,
						typeof(OpenApi.CSharp.UnableToCreateInlineSchemaDiagnostic).Assembly,
					] : [])
					// if you don't have TS, remove these types
					.Except(!mode.HasFlag(LanguageSpecificInclusions.TypeScript) ? [
						typeof(OpenApi.TypeScript.UnableToGenerateSchema).Assembly,
					] : []).Distinct()
			   from type in asm.GetTypes()
			   where type.IsAssignableTo(target)
			   where !type.IsAbstract
			   select type;
	}
}