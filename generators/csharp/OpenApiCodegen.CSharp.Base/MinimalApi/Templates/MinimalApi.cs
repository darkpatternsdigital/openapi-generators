using System.Linq;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApiCodegen.CSharp.MinimalApi.Templates;

/// <summary>One path group: its handler interface and the method that maps its routes</summary>
public record MinimalApiTemplate(
	PartialHeader Header,

	string PackageName,
	string InterfaceName,
	string EndpointsClassName,
	string MapMethodName,

	string? Summary,
	string? Description,

	// The same operations as the MVC output
	ControllerOperation[] Operations
)
{
	public bool HasBase64Params => Operations.Any(o => o.RequestBodies.Any(b => b.AllParams.Any(p => p.IsBase64)));
	public bool HasEnumParams => Operations.Any(o => o.RequestBodies.Any(b => b.AllParams.Any(p => p.EnumType != null && !p.UsesRequestValue)));
	public bool HasRequestValueParams => Operations.Any(o => o.RequestBodies.Any(b => b.AllParams.Any(p => p.UsesRequestValue)));
}
