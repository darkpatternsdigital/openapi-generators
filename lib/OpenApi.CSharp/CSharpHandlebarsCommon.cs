using HandlebarsDotNet;
using DarkPatterns.OpenApi.CSharp.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using DarkPatterns.OpenApiCodegen.Handlebars;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;
using BaseProcess = DarkPatterns.OpenApiCodegen.Handlebars.HandlebarsTemplateProcess;

namespace DarkPatterns.OpenApi.CSharp;

public static class CSharpHandlebarsCommon
{
	public static IHandlebars CreateHandlebars()
	{
		var result = BaseProcess.CreateHandlebars();

		result.RegisterHelper(
			"escapeverbatimstring",
			(context, parameters) =>
				parameters[0] is string s
					? s.ToString().Replace("\"", "\"\"")
					: parameters[0]
		);

		result.AddTemplatesAdjacentToType(typeof(Templates.Model));

		return result;
	}

	public static string ProcessModel(
		PartialHeader header,
		string packageName,
		Uri schemaId,
		Model model,
		IHandlebars? handlebars = null
	)
	{
		handlebars ??= CreateHandlebars();
		var (templateName, dict) = model switch
		{
			ObjectModel m => ("objectmodel", ToTemplate(m)),
			EnumModel m => ("enumModel", ToTemplate(m)),
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
			return BaseProcess.ToDictionary<ModelTemplate<TModel>>(new(Header: header, PackageName: packageName, SourceSchemaId: schemaId.OriginalString, Model: m));
		}
	}
}
