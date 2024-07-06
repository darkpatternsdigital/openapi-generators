using System;
using System.Collections.Generic;
using System.Linq;
using PrincipleStudios.OpenApi.Transformations.Abstractions;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public abstract class OpenApiDocumentVisitor<TArgument> : IOpenApiDocumentVisitor<TArgument>
{
	public virtual void VisitAny(IReferenceableDocumentNode? openApiElement, TArgument argument) => OpenApiDocumentVisitorExtensions.VisitAny(this, openApiElement, argument);

	public virtual void Visit(OpenApiContact contact, TArgument? argument) { }

	public virtual void Visit(OpenApiDocument document, TArgument argument)
	{
		// this.VisitHelper(document.Servers, argument);
		this.VisitHelper(document.Paths.Values, argument);
		this.Visit(document.Info, argument);
		// this.VisitHelper(document.SecurityRequirements, argument);
		// this.VisitHelper(document.Tags, argument);
	}

	public virtual void Visit(OpenApiInfo info, TArgument argument)
	{
		this.VisitAny(info.Contact, argument);
		this.VisitAny(info.License, argument);
	}

	public virtual void Visit(OpenApiLicense license, TArgument argument) { }
	public virtual void Visit(OpenApiOperation operation, TArgument argument)
	{
		// this.VisitHelper(operation.Security, argument);
		// this.VisitHelper(operation.Callbacks, argument);
		this.VisitAny(operation.Responses, argument);
		this.VisitAny(operation.RequestBody, argument);
		this.VisitHelper(operation.Parameters, argument);
		// this.VisitHelper(operation.ExternalDocs, argument);
		// this.VisitHelper(operation.Servers, argument);
	}
	public virtual void Visit(OpenApiParameter parameter, TArgument argument) { }
	public virtual void Visit(OpenApiPath path, TArgument argument)
	{
		this.VisitHelper(path.Operations.Values, argument);
	}

	public virtual void Visit(OpenApiMediaTypeObject mediaTypeObject, TArgument argument) { }

	public virtual void Visit(OpenApiRequestBody requestBody, TArgument argument)
	{
		if (requestBody.Content != null)
			this.VisitHelper(requestBody.Content.Values, argument);
	}
	public virtual void Visit(OpenApiResponse response, TArgument argument)
	{
		this.VisitHelper(response.Headers, argument);
		if (response.Content != null)
			this.VisitHelper(response.Content.Values, argument);
	}
	public virtual void Visit(OpenApiResponses responses, TArgument argument)
	{
		this.VisitHelper(responses.StatusCodeResponses.Values, argument);
	}


	public virtual void VisitUnknown(IReferenceableDocumentNode node, TArgument argument)
	{
		throw new NotSupportedException();
	}
}
