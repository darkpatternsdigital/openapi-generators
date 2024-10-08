﻿using HandlebarsDotNet;
using System.IO;
using System.Linq;

namespace DarkPatterns.OpenApi.CSharp
{
	public static class ControllerHandlebarsTemplateProcess
	{
		public static IHandlebars CreateHandlebars()
		{
			var result = HandlebarsTemplateProcess.CreateHandlebars();

			foreach (var resourceName in typeof(ControllerHandlebarsTemplateProcess).Assembly.GetManifestResourceNames().Where(n => n.EndsWith(".handlebars")))
				result.AddTemplate(typeof(ControllerHandlebarsTemplateProcess).Assembly, resourceName);

			return result;
		}

		public static string ProcessController(this IHandlebars handlebars, Templates.FullTemplate clientTemplate)
		{
			var template = handlebars.Configuration.RegisteredTemplates["client"];

			using var sr = new StringWriter();
			var dict = HandlebarsTemplateProcess.ToDictionary<Templates.FullTemplate>(clientTemplate);
			template(sr, dict);
			return sr.ToString();
		}

		public static string ProcessAddServices(this IHandlebars handlebars, Templates.AddServicesModel addServices)
		{
			var template = handlebars.Configuration.RegisteredTemplates["addServices"];

			using var sr = new StringWriter();
			var dict = HandlebarsTemplateProcess.ToDictionary<Templates.AddServicesModel>(addServices);
			template(sr, dict);
			return sr.ToString();
		}
	}
}
