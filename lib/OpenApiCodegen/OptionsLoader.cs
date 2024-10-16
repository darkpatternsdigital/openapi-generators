using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DarkPatterns.OpenApiCodegen;

public static class OptionsLoader
{
	public static T LoadOptions<T>(IEnumerable<Stream> streams, IEnumerable<string> files, Action<IConfigurationBuilder>? configureBuilder = null)
	{
		var builder = new ConfigurationBuilder();
		foreach (var stream in streams)
			builder.AddYamlStream(stream);
		foreach (var file in files)
			if (File.Exists(file))
				builder.AddYamlFile(file, optional: true);
		configureBuilder?.Invoke(builder);
		return builder.Build().Get<T>()
			// TODO - generate diagnostic instead of throwing exception
			?? throw new InvalidOperationException("Could not build schema options");
	}
}
