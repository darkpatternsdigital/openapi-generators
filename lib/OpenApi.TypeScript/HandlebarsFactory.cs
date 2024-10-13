using HandlebarsDotNet;
using System;

namespace DarkPatterns.OpenApi.TypeScript;

public class HandlebarsFactory(Func<IHandlebars> innerFactory)
{
	private readonly Lazy<IHandlebars> handlebars = new(innerFactory);
	public static HandlebarsFactory Default { get; } = new(HandlebarsTemplateProcess.CreateHandlebars);

	public IHandlebars Handlebars => handlebars.Value;
}