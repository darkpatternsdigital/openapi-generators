﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DarkPatterns.Extensions.Configuration.Yaml
{
	/// <summary>
	/// A YAML file based <see cref="FileConfigurationSource"/>.
	/// </summary>
	public class YamlFileConfigurationSource : FileConfigurationSource
	{
		public override IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			EnsureDefaults(builder);
			return new YamlFileConfigurationProvider(this);
		}
	}
}
