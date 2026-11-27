using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MinimalApi.Templates;
using DarkPatterns.OpenApiCodegen.Handlebars;
using HandlebarsDotNet;
using System.IO;

namespace DarkPatterns.OpenApiCodegen.CSharp.MinimalApi;

public static class MinimalApiHandlebarsTemplateProcess
{
	public static IHandlebars CreateHandlebars()
	{
		var result = CSharpHandlebarsCommon.CreateHandlebars();

		result.AddTemplatesAdjacentToType(typeof(MinimalApiTemplate));

		return result;
	}

	public static string ProcessMinimalApi(this IHandlebars handlebars, MinimalApiTemplate minimalApiTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["minimalApi"];

		using var sr = new StringWriter();
		var dict = HandlebarsTemplateProcess.ToDictionary(minimalApiTemplate);
		template(sr, dict);
		return sr.ToString();
	}
}
