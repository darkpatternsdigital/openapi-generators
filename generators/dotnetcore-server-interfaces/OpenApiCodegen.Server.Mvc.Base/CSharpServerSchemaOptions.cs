using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DarkPatterns.OpenApi.CSharp;

public class CSharpServerSchemaOptions : CSharpSchemaOptions
{
	public CSharpServerSchemaOptions()
	{
		Extensions = new();
	}

	public new CSharpSchemaServerExtensionsOptions Extensions
	{
		get { return (base.Extensions as CSharpSchemaServerExtensionsOptions)!; }
		set { base.Extensions = value; }
	}
	public string PathPrefix { get; set; } = "";


	public static System.IO.Stream GetServerDefaultOptionsJson() =>
		typeof(CSharpServerSchemaOptions).Assembly.GetManifestResourceStream($"{typeof(CSharpServerSchemaOptions).Namespace}.csharp.config.yaml");
}
