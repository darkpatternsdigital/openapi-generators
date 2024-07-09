using PrincipleStudios.OpenApiCodegen;
using System.Collections.Generic;

namespace PrincipleStudios.OpenApi.Transformations
{
	public interface IOpenApiOperationControllerTransformer
	{
		SourceEntry TransformController(string groupName, OperationGroupData groupData, OpenApiTransformDiagnostic diagnostic);
		string SanitizeGroupName(string groupName);
	}

	public class OperationGroupData
	{
		public string? Summary { get; set; }
		public string? Description { get; set; }
		public List<(Abstractions.OpenApiOperation Operation, string Method, Abstractions.OpenApiPath Path)> Operations { get; } = new();

		public void Deconstruct(out string? summary, out string? description, out IEnumerable<(Abstractions.OpenApiOperation Operation, string Method, Abstractions.OpenApiPath Path)> operations)
		{
			summary = Summary;
			description = Description;
			operations = Operations;
		}
	}

}