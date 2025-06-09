using DarkPatterns.OpenApi.CSharp;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp;

public static class OptionsHelpers
{
	public static CSharpSchemaOptions LoadOptions()
	{
		var defaultJson = CSharpSchemaOptions.DefaultOptionsJson.Value;
		var result = OptionsLoader.LoadOptions<CSharpSchemaOptions>([defaultJson]);
		result.DefaultNamespace = "DPD.Controller";
		return result;
	}
}
