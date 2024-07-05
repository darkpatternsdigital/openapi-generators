using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public static class OpenApiDocumentVisitorExtensions
{
	// Intentionally not an extension method, so that the overridden functionality is more likely to be used
	public static void VisitAny<TArgument>(IOpenApiDocumentVisitor<TArgument> visitor, IReferenceableDocumentNode? node, TArgument argument)
	{
		switch (node)
		{
			case null: break;
			case OpenApiContact contact: visitor.Visit(contact, argument); break;
			case OpenApiDocument document: visitor.Visit(document, argument); break;
			case OpenApiInfo info: visitor.Visit(info, argument); break;
			case OpenApiLicense license: visitor.Visit(license, argument); break;
			case OpenApiMediaTypeObject mediaTypeObject: visitor.Visit(mediaTypeObject, argument); break;
			case OpenApiOperation operation: visitor.Visit(operation, argument); break;
			case OpenApiParameter parameter: visitor.Visit(parameter, argument); break;
			case OpenApiPath path: visitor.Visit(path, argument); break;
			case OpenApiRequestBody requestBody: visitor.Visit(requestBody, argument); break;
			case OpenApiResponse response: visitor.Visit(response, argument); break;
			case OpenApiResponses responses: visitor.Visit(responses, argument); break;
			default: visitor.VisitUnknown(node, argument); break;
		};
	}

	public static void VisitHelper<TArgument, T>(this IOpenApiDocumentVisitor<TArgument> visitor, IEnumerable<T> entries, TArgument argument)
		where T : IReferenceableDocumentNode
	{
		foreach (var e in entries)
			VisitAny(visitor, e, argument);
	}
}
