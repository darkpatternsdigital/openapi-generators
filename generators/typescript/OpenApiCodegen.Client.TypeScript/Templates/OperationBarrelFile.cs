using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Templates
{
	public record OperationBarrelFileModel(PartialHeader Header, OperationReference[] Operations);

	public record OperationReference(string Path, string MethodName);
}
