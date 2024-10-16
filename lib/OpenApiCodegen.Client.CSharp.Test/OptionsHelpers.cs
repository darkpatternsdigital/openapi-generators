using Microsoft.Extensions.Configuration;
using DarkPatterns.OpenApi.CSharp;
using System;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp;

public static class OptionsHelpers
{
	public static CSharpSchemaOptions LoadOptions(Action<IConfigurationBuilder>? configureBuilder = null)
	{
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		var result = OptionsLoader.LoadOptions<CSharpSchemaOptions>([defaultJsonStream], [], configureBuilder);
		result.DefaultNamespace = "DPD.Controller";
		return result;
	}
}
