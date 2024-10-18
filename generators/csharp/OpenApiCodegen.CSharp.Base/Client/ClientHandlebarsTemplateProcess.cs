using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.Client.Templates;
using DarkPatterns.OpenApiCodegen.Handlebars;
using HandlebarsDotNet;
using System.IO;

namespace DarkPatterns.OpenApiCodegen.CSharp.Client;

public static class ClientHandlebarsTemplateProcess
{
	public static IHandlebars CreateHandlebars()
	{
		var result = CSharpHandlebarsCommon.CreateHandlebars();

		result.AddTemplatesAdjacentToType(typeof(ClientHandlebarsTemplateProcess));

		return result;
	}

	public static string ProcessClient(this IHandlebars handlebars, FullTemplate clientTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["client"];

		using var sr = new StringWriter();
		var dict = HandlebarsTemplateProcess.ToDictionary(clientTemplate);
		template(sr, dict);
		return sr.ToString();
	}
}
