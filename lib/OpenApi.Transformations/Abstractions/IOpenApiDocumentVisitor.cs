namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public interface IOpenApiDocumentVisitor<TArgument>
{
	void Visit(OpenApiContact contact, TArgument argument);

	void Visit(OpenApiDocument document, TArgument argument);
	void Visit(OpenApiInfo info, TArgument argument);
	void Visit(OpenApiLicense license, TArgument argument);
	void Visit(OpenApiMediaTypeObject mediaTypeObject, TArgument argument);
	void Visit(OpenApiOperation operation, TArgument argument);
	void Visit(OpenApiParameter parameter, TArgument argument);
	void Visit(OpenApiPath path, TArgument argument);
	void Visit(OpenApiRequestBody requestBody, TArgument argument);
	void Visit(OpenApiResponse response, TArgument argument);
	void Visit(OpenApiResponses responses, TArgument argument);
	void VisitUnknown(IReferenceableDocumentNode node, TArgument argument);
}
