using HandlebarsDotNet;
using System.IO;
using BaseProcess = DarkPatterns.OpenApiCodegen.Handlebars.HandlebarsTemplateProcess;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApi.TypeScript;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

public static class OperationHandlebarsTemplateProcess
{
	public static IHandlebars CreateHandlebars()
	{
		var result = TypeScriptHandlebarsCommon.CreateHandlebars();

		result.AddTemplatesAdjacentToType(typeof(OperationHandlebarsTemplateProcess));

		return result;
	}

	public static string ProcessOperation(this IHandlebars handlebars, Templates.OperationTemplate operationTemplate)
	{
		var template = handlebars.Configuration.RegisteredTemplates["operation"];

		using var sr = new StringWriter();
		var dict = BaseProcess.ToDictionary(operationTemplate);
		template(sr, dict);
		return sr.ToString();
	}

	public static string ProcessBarrelFile(this IHandlebars handlebars, Templates.OperationBarrelFileModel barrelFileModel)
	{
		var template = handlebars.Configuration.RegisteredTemplates["operationBarrelFile"];

		using var sr = new StringWriter();
		var dict = BaseProcess.ToDictionary(barrelFileModel);
		template(sr, dict);
		return sr.ToString();
	}

}
