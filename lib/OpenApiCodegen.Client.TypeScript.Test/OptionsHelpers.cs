using Microsoft.Extensions.Configuration;
using PrincipleStudios.OpenApi.TypeScript;
using System;

namespace PrincipleStudios.OpenApiCodegen.Client.TypeScript
{
	public static class OptionsHelpers
	{
		public static TypeScriptSchemaOptions LoadOptions(Action<IConfigurationBuilder>? configureBuilder = null)
		{
			using var defaultJsonStream = TypeScriptSchemaOptions.GetDefaultOptionsJson();
			var builder = new ConfigurationBuilder();
			builder.AddYamlStream(defaultJsonStream);
			configureBuilder?.Invoke(builder);
			var result = builder.Build().Get<TypeScriptSchemaOptions>()
					?? throw new InvalidOperationException("Could not construct options");
			return result;
		}

	}
}
