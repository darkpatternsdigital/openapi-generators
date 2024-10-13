using HandlebarsDotNet;
using System;

namespace DarkPatterns.OpenApi.CSharp;

public class HandlebarsFactory(Func<IHandlebars> innerFactory)
{
	private readonly Lazy<IHandlebars> handlebars = new Lazy<IHandlebars>(innerFactory);

	public static HandlebarsFactory Default { get; } = new(HandlebarsTemplateProcess.CreateHandlebars);

	public IHandlebars Handlebars => handlebars.Value;
}