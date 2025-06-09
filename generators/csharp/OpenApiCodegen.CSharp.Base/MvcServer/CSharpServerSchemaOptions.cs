using System;
using System.Text.Json.Nodes;
using DarkPatterns.OpenApi.CSharp;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

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
	public string? SecurityAttribute { get; set; }


	public static readonly Lazy<JsonNode> ServerDefaultOptionsJson = new Lazy<JsonNode>(() =>
		typeof(CSharpServerSchemaOptions).Assembly.GetManifestResourceStream($"{typeof(CSharpServerSchemaOptions).Namespace}.csharp.config.yaml").LoadYaml()
	);
}
