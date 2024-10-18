using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.Client;
using DarkPatterns.OpenApiCodegen.CSharp.WebhookClient;
using System;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp
{
	public class HandlebarsTemplateProcessShould
	{
		[Fact]
		public void RegisterAllHandlebarsTemplates()
		{
			var handlebars = ClientHandlebarsTemplateProcess.CreateHandlebars();

			Assert.True(handlebars.Configuration.RegisteredTemplates.Count >= 4);
		}
	}
}
