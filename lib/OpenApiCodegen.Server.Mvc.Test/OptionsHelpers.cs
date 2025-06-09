using System.Collections.Generic;
using System.Text.Json.Nodes;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc;

public static class OptionsHelpers
{
	public static CSharpServerSchemaOptions LoadOptions(IEnumerable<JsonNode>? nodes = null)
	{
		var defaultJsonStream = CSharpSchemaOptions.DefaultOptionsJson.Value;
		var result = OptionsLoader.LoadOptions<CSharpServerSchemaOptions>([defaultJsonStream, .. nodes ?? []]);
		result.DefaultNamespace = "DPD.Controller";
		return result;
	}
}
