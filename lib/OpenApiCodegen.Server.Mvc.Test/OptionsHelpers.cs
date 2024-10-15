using Microsoft.Extensions.Configuration;
using DarkPatterns.OpenApi.CSharp;
using System;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc;

public static class OptionsHelpers
{
	public static CSharpServerSchemaOptions LoadOptions(Action<IConfigurationBuilder>? configureBuilder = null)
	{
		using var defaultJsonStream = CSharpSchemaOptions.GetDefaultOptionsJson();
		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>([defaultJsonStream], [], configureBuilder);
		result.DefaultNamespace = "DPD.Controller";
		return result;
	}
}
