using DarkPatterns.OpenApiCodegen.Handlebars;
using HandlebarsDotNet;

namespace DarkPatterns.OpenApi.CSharp;

public static class HandlebarsFactoryDefaults
{
	public static HandlebarsFactory Default { get; } = new(CSharpHandlebarsCommon.CreateHandlebars);
}