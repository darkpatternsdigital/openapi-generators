using HandlebarsDotNet;
using DarkPatterns.OpenApi.TypeScript.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;
using BaseProcess = DarkPatterns.OpenApiCodegen.Handlebars.HandlebarsTemplateProcess;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApi.TypeScript;

public static class TypeScriptHandlebarsCommon
{
	public static IHandlebars CreateHandlebars()
	{
		var result = BaseProcess.CreateHandlebars();

		result.AddTemplatesFromAssembly(typeof(TypeScriptHandlebarsCommon).Assembly);

		return result;
	}

	public static string ProcessModel(
		PartialHeader header,
		string packageName,
		Model model,
		IHandlebars? handlebars = null
	)
	{
		handlebars ??= CreateHandlebars();
		var (templateName, dict) = model switch
		{
			ObjectModel m => ("objectmodel", ToTemplate(m)),
			EnumModel m => ("enummodel", ToTemplate(m)),
			ArrayModel m => ("arraymodel", ToTemplate(m)),
			TypeUnionModel m => ("typeUnionModel", ToTemplate(m)),
			_ => throw new NotImplementedException()
		};
		var template = handlebars.Configuration.RegisteredTemplates[templateName];

		using var sr = new StringWriter();
		template(sr, dict);
		return sr.ToString();

		IDictionary<string, object?> ToTemplate<TModel>(TModel m)
			where TModel : Model
		{
			return BaseProcess.ToDictionary<ModelTemplate<TModel>>(new(Header: header, PackageName: packageName, Model: m));
		}
	}

	public static string ProcessModelBarrelFile(
		ModelBarrelFile modelBarrelFile,
		IHandlebars? handlebars = null
	)
	{
		handlebars ??= CreateHandlebars();
		var dict = BaseProcess.ToDictionary(modelBarrelFile);

		using var sr = new StringWriter();
		handlebars.Configuration.RegisteredTemplates["modelBarrelFile"](sr, dict);
		return sr.ToString();
	}
}
