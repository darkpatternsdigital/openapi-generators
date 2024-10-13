using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Abstractions;

namespace DarkPatterns.OpenApi.Transformations;

public class PathControllerSourceTransformer(
	ISchemaRegistry schemaRegistry,
	OpenApiDocument document,
	IOpenApiOperationControllerTransformer operationControllerTransformer,
	PathControllerSourceTransformer.OperationToGroupOverride? operationToGroupOverride = null
) : OperationGroupingSourceTransformer(schemaRegistry, document, GetGroup(operationToGroupOverride), operationControllerTransformer)
{
	public delegate string? OperationToGroupOverride(OpenApiOperation operation, OpenApiPath path);

	private static OperationToGroup GetGroup(OperationToGroupOverride? operationToGroupOverride)
	{
		return (operation, pathEntry) =>
		{
			var group = operationToGroupOverride?.Invoke(operation, pathEntry);
			if (group != null)
				return (group, null, null);

			var path = pathEntry.GetLastContextPart();
			return (path, pathEntry.Summary, pathEntry.Description);
		};
	}
}
