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

		result.AddTemplatesAdjacentToType(typeof(MinimalApiHandlebarsTemplateProcess));

		return result;
	}

	public static string ProcessSetup(this IHandlebars handlebars, SetupTemplate serviceExtensionsTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["setup"];

		using var sr = new StringWriter();
		var dict = Handlebars.HandlebarsTemplateProcess.ToDictionary(serviceExtensionsTemplate);
		template(sr, dict);
		return sr.ToString();
	}

	public static string ProcessRequestHandler(this IHandlebars handlebars, RequestHandlerTemplate serviceExtensionsTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["request-handler"];

		using var sr = new StringWriter();
		var dict = Handlebars.HandlebarsTemplateProcess.ToDictionary(serviceExtensionsTemplate);
		template(sr, dict);
		return sr.ToString();
	}
}
