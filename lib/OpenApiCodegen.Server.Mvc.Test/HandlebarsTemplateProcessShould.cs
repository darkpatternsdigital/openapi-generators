using DarkPatterns.OpenApiCodegen.CSharp.MvcServer;
using System;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.Test
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
