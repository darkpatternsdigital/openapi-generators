﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DarkPatterns.Extensions.Configuration.Yaml
{
	/// <summary>
	/// A YAML stream based <see cref="StreamConfigurationSource"/>.
	/// </summary>
	public class YamlStreamConfigurationSource : StreamConfigurationSource
	{
		public override IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			return new YamlStreamConfigurationProvider(this);
		}
	}
}
