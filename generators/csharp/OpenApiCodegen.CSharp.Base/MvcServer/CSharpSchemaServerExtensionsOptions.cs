using DarkPatterns.OpenApi.CSharp;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

public class CSharpSchemaServerExtensionsOptions : CSharpSchemaExtensionsOptions
{
	public string ControllerName { get; set; } = "dotnet-mvc-server-controller";
}
