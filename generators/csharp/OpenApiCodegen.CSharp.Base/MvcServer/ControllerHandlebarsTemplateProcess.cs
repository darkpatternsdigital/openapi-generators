using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;
using DarkPatterns.OpenApiCodegen.Handlebars;
using HandlebarsDotNet;
using System.IO;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

public static class ControllerHandlebarsTemplateProcess
{
	public static IHandlebars CreateHandlebars()
	{
		var result = CSharpHandlebarsCommon.CreateHandlebars();

		result.AddTemplatesAdjacentToType(typeof(ControllerTemplate));

		return result;
	}

	public static string ProcessController(this IHandlebars handlebars, ControllerTemplate controllerTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["controller"];

		using var sr = new StringWriter();
		var dict = HandlebarsTemplateProcess.ToDictionary(controllerTemplate);
		template(sr, dict);
		return sr.ToString();
	}

	public static string ProcessAddServices(this IHandlebars handlebars, AddServicesModel addServices)
	{
		var template = handlebars.Configuration.RegisteredTemplates["addServices"];

		using var sr = new StringWriter();
		var dict = HandlebarsTemplateProcess.ToDictionary(addServices);
		template(sr, dict);
		return sr.ToString();
	}
}
