using DarkPatterns.OpenApi.CSharp;
using System;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp
{
	public class HandlebarsTemplateProcessShould
	{
		[Fact]
		public void RegisterAllHandlebarsTemplates()
		{
			var handlebars = ControllerHandlebarsTemplateProcess.CreateHandlebars();

			Assert.True(handlebars.Configuration.RegisteredTemplates.Count >= 4);
		}
	}
}
