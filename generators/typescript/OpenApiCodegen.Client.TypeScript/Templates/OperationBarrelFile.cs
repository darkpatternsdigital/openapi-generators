using DarkPatterns.OpenApi.TypeScript.Templates;
using System;
using System.Collections.Generic;
using System.Text;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Templates
{
	public record OperationBarrelFileModel(PartialHeader Header, OperationReference[] Operations);

	public record OperationReference(string Path, string MethodName);
}
