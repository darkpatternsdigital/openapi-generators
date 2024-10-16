using HandlebarsDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApiCodegen.Handlebars;

public static class HandlebarsTemplateProcess
{
	public static IHandlebars CreateHandlebars()
	{
		var result = HandlebarsDotNet.Handlebars.Create();

		result.RegisterHelper(
			"linewrap",
			(context, parameters) =>
				parameters[0] is string s
					? s.ToString().Replace("\r", "").Replace("\n", parameters[1].ToString())
					: parameters[0]
		);

		result.RegisterHelper(
			"ifEquals",
			(BlockHelperOptions options, Context context, Arguments parameters) =>
			{
				return parameters[0] is string s1 && parameters[1] is string s2
					? s1 == s2 ? options.Template() : options.Inverse()
					: throw new InvalidOperationException($"Cannot compoare '{parameters[0]}' and '{parameters[1]}'");
			}
		);

		result.AddTemplatesFromAssembly(typeof(HandlebarsTemplateProcess).Assembly);

		return result;
	}

	public static void AddTemplatesAdjacentToType(this IHandlebars result, Type type)
	{
		foreach (var resourceName in type.Assembly.GetManifestResourceNames()
				.Where(n => n.StartsWith(type.Namespace + ".") && n.EndsWith(".handlebars")))
			result.AddTemplate(type.Assembly, resourceName);
	}

	public static void AddTemplatesFromAssembly(this IHandlebars result, System.Reflection.Assembly assembly)
	{
		foreach (var resourceName in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".handlebars")))
			result.AddTemplate(assembly, resourceName);
	}

	public static void AddTemplate(this IHandlebars result, System.Reflection.Assembly assembly, string resourceName)
	{
		using var stream = assembly.GetManifestResourceStream(resourceName)!;
		using var reader = new StreamReader(stream);
		var templateName = Path.GetFileNameWithoutExtension(resourceName).Split('.').Last();
		result.RegisterTemplate(templateName: templateName, template: reader.ReadToEnd());
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
