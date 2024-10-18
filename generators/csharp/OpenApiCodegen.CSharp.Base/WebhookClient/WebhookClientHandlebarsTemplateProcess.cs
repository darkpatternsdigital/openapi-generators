using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.WebhookClient.Templates;
using DarkPatterns.OpenApiCodegen.Handlebars;
using HandlebarsDotNet;
using System.IO;

namespace DarkPatterns.OpenApiCodegen.CSharp.WebhookClient;

public static class WebhookClientHandlebarsTemplateProcess
{
	public static IHandlebars CreateHandlebars()
	{
		var result = CSharpHandlebarsCommon.CreateHandlebars();

		result.AddTemplatesAdjacentToType(typeof(WebhookClientHandlebarsTemplateProcess));

		return result;
	}

	public static string ProcessDocument(this IHandlebars handlebars, FullTemplate clientTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["webhooks"];

		using var sr = new StringWriter();
		var dict = HandlebarsTemplateProcess.ToDictionary(clientTemplate);
		template(sr, dict);
		return sr.ToString();
	}
}
