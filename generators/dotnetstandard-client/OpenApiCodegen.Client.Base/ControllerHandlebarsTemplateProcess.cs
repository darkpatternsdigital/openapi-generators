using DarkPatterns.OpenApiCodegen.Handlebars;
using HandlebarsDotNet;
using System.IO;
using BaseProcess = DarkPatterns.OpenApiCodegen.Handlebars.HandlebarsTemplateProcess;

namespace DarkPatterns.OpenApi.CSharp
{
	public static class ControllerHandlebarsTemplateProcess
	{
		public static IHandlebars CreateHandlebars()
		{
			var result = CSharpHandlebarsCommon.CreateHandlebars();

			result.AddTemplatesFromAssembly(typeof(ControllerHandlebarsTemplateProcess).Assembly);

			return result;
		}

		public static string ProcessController(this IHandlebars handlebars, Templates.FullTemplate clientTemplate)
		{
			var template = handlebars.Configuration.RegisteredTemplates["client"];

			using var sr = new StringWriter();
			var dict = BaseProcess.ToDictionary(clientTemplate);
			template(sr, dict);
			return sr.ToString();
		}

		public static string ProcessAddServices(this IHandlebars handlebars, Templates.AddServicesModel addServices)
		{
			var template = handlebars.Configuration.RegisteredTemplates["addServices"];

			using var sr = new StringWriter();
			var dict = BaseProcess.ToDictionary(addServices);
			template(sr, dict);
			return sr.ToString();
		}
	}
}
