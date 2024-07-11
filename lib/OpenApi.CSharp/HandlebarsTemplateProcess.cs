using HandlebarsDotNet;
using PrincipleStudios.OpenApi.CSharp.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace PrincipleStudios.OpenApi.CSharp
{
	public static class HandlebarsTemplateProcess
	{
		public static IHandlebars CreateHandlebars()
		{
			var result = Handlebars.Create();

			result.RegisterHelper(
				"linewrap",
				(context, parameters) =>
					parameters[0] is string s
						? s.ToString().Replace("\r", "").Replace("\n", parameters[1].ToString())
						: parameters[0]
			);

			result.RegisterHelper(
				"escapeverbatimstring",
				(context, parameters) =>
					parameters[0] is string s
						? s.ToString().Replace("\"", "\"\"")
						: parameters[0]
			);

			foreach (var resourceName in typeof(HandlebarsTemplateProcess).Assembly.GetManifestResourceNames().Where(n => n.EndsWith(".handlebars")))
				result.AddTemplate(typeof(HandlebarsTemplateProcess).Assembly, resourceName);

			return result;
		}

		public static void AddTemplate(this IHandlebars result, System.Reflection.Assembly assembly, string resourceName)
		{
			using var stream = assembly.GetManifestResourceStream(resourceName)!;
			using var reader = new StreamReader(stream);
			var templateName = Path.GetFileNameWithoutExtension(resourceName).Split('.').Last();
			result.RegisterTemplate(templateName: templateName, template: reader.ReadToEnd());
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
				return ToDictionary<ModelTemplate<TModel>>(new(Header: header, PackageName: packageName, Model: m));
			}
		}

		public static IDictionary<string, object?> ToDictionary<T>(T model)
		{
			var result = model == null ? null : System.Text.Json.JsonSerializer.SerializeToNode(model);

			return (IDictionary<string, object?>)FromElement(result)!;
		}

		private static object? FromElement(JsonNode? result)
		{
			return result switch
			{
				null => null,
				JsonValue n when n.TryGetValue<bool>(out var v) => v,
				JsonValue n when n.TryGetValue<decimal>(out var v) => v,
				JsonValue n when n.TryGetValue<string>(out var v) => v,
				JsonValue n when n.TryGetValue<int>(out var v) => v,
				JsonArray array => (from item in array
									select FromElement(item)).ToArray(),
				JsonObject obj => (from prop in obj
								   let Value = FromElement(prop.Value)
								   where Value != null
								   select (prop.Key, Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
				_ => throw new InvalidOperationException(),
			};
		}
	}
}
