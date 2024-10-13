using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApi.TypeScript;

public static class HandlebarsFactoryDefaults
{
	public static HandlebarsFactory Default { get; } = new(TypeScriptHandlebarsCommon.CreateHandlebars);
}