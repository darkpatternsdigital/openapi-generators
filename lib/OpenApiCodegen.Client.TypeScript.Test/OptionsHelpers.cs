using DarkPatterns.OpenApi.TypeScript;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript
{
	public static class OptionsHelpers
	{
		public static TypeScriptSchemaOptions LoadOptions()
		{
			var defaultJson = TypeScriptSchemaOptions.DefaultOptionsJson.Value;
			return OptionsLoader.LoadOptions<TypeScriptSchemaOptions>([defaultJson]);
		}
	}
}
