using DarkPatterns.OpenApi.Transformations.Abstractions;

namespace DarkPatterns.OpenApi.Transformations
{
	public class PathControllerSourceTransformer : OperationGroupingSourceTransformer
	{
		public delegate string? OperationToGroupOverride(OpenApiOperation operation, OpenApiPath path);


		public PathControllerSourceTransformer(DocumentRegistry registry, ISchemaRegistry schemaRegistry, OpenApiDocument document, IOpenApiOperationControllerTransformer operationControllerTransformer, OperationToGroupOverride? operationToGroupOverride = null)
			: base(registry, schemaRegistry, document, GetGroup(operationToGroupOverride), operationControllerTransformer)
		{
		}

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
}
