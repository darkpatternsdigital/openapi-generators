using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Diagnostics;

namespace DarkPatterns.OpenApiCodegen;

public class DiagnosticsShould
{
	[MemberData(nameof(DiagnosticBaseNames))]
	[Theory]
	public void Cover_all_DiagnosticBase_subtypes(string typeName)
	{
		Assert.True(TransformationDiagnostics.DiagnosticBy.ContainsKey(typeName), $"Missing diagnostic for '{typeName}'");
	}

	[MemberData(nameof(DiagnosticByKeys))]
	[Theory]
	public void Have_no_extra_diagnostics(string typeName)
	{
		Assert.True(ChildTypesOf(typeof(DiagnosticBase)).Any(t => t.FullName == typeName), $"Extra Diagnosic for '{typeName}'");
	}

	[MemberData(nameof(DiagnosticBaseNames))]
	[Theory]
	public void Have_translations_for_all_DiagnosticBase_subtypes(string typeName)
	{
		Assert.True(CommonDiagnostics.ResourceManager.GetString(typeName) != null, $"Missing diagnostic for '{typeName}'");
	}

	public static IEnumerable<object[]> DiagnosticByKeys()
	{
		return from key in TransformationDiagnostics.DiagnosticBy.Keys
			   select new object[] { key };
	}

	public static IEnumerable<object[]> DiagnosticBaseNames()
	{
		return from type in ChildTypesOf(typeof(DiagnosticBase))
			   select new object[] { type.FullName! };
	}

	private static IEnumerable<Type> ChildTypesOf(Type target)
	{
		return from asm in AppDomain.CurrentDomain.GetAssemblies()
					.Concat([
						typeof(OpenApi.Transformations.Specifications.UnableToParseDiagnostic).Assembly,
						typeof(OpenApi.Specifications.v3_0.InvalidNode).Assembly,
						typeof(Json.Loaders.YamlLoadDiagnostic).Assembly,
					]).Distinct()
			   from type in asm.GetTypes()
			   where type.IsAssignableTo(target)
			   where !type.IsAbstract
			   select type;
	}
}