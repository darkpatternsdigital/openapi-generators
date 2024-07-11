using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using YamlDotNet.Core;

namespace PrincipleStudios.OpenApi.Transformations.Configuration
{
	/// <summary>
	/// A YAML file based <see cref="FileConfigurationProvider"/>.
	/// </summary>
	public class YamlStreamConfigurationProvider : StreamConfigurationProvider
	{
		public YamlStreamConfigurationProvider(YamlStreamConfigurationSource source) : base(source) { }

		public override void Load(Stream stream)
		{
			var parser = new YamlConfigurationFileParser();
			try
			{
				Data = parser.Parse(stream);
			}
			catch (YamlException e)
			{
				throw new FormatException(string.Format(Resources.Error_YamlParseError, e.Message), e);
			}
		}
	}
}