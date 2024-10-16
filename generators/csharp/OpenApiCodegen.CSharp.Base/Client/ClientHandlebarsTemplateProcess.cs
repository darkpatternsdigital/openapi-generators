using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.Client.Templates;
using DarkPatterns.OpenApiCodegen.Handlebars;
using HandlebarsDotNet;
using System.IO;
using BaseProcess = DarkPatterns.OpenApiCodegen.Handlebars.HandlebarsTemplateProcess;

namespace DarkPatterns.OpenApiCodegen.CSharp.Client;

public static class ClientHandlebarsTemplateProcess
{
	public static IHandlebars CreateHandlebars()
	{
		var result = CSharpHandlebarsCommon.CreateHandlebars();

		result.AddTemplatesFromAssembly(typeof(ClientHandlebarsTemplateProcess).Assembly);

		return result;
	}

	public static string ProcessController(this IHandlebars handlebars, FullTemplate clientTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["client"];

		using var sr = new StringWriter();
		var dict = BaseProcess.ToDictionary(clientTemplate);
		template(sr, dict);
		return sr.ToString();
	}

	public static string ProcessAddServices(this IHandlebars handlebars, AddServicesModel addServices)
	{
		var template = handlebars.Configuration.RegisteredTemplates["addServices"];

		using var sr = new StringWriter();
		var dict = BaseProcess.ToDictionary(addServices);
		template(sr, dict);
		return sr.ToString();
	}
}
