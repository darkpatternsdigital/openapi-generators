using System;
using System.Collections.Generic;
using System.Linq;
using PrincipleStudios.OpenApi.Transformations.Abstractions;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public abstract class OpenApiDocumentVisitor<TArgument> : IOpenApiDocumentVisitor<TArgument>
{
	public virtual void Visit(OpenApiContact contact, TArgument? argument) { }

	public virtual void Visit(OpenApiDocument document, TArgument argument)
	{
		// this.VisitHelper(document.Servers, argument);
		foreach (var e in document.Paths.Values)
			this.Visit(e, argument);
		this.Visit(document.Info, argument);
		// this.VisitHelper(document.SecurityRequirements, argument);
		// this.VisitHelper(document.Tags, argument);
	}

	public virtual void Visit(OpenApiInfo info, TArgument argument)
	{
		if (info.Contact != null)
			this.Visit(info.Contact, argument);
		if (info.License != null)
			this.Visit(info.License, argument);
	}

	public virtual void Visit(OpenApiLicense license, TArgument argument) { }
	public virtual void Visit(OpenApiOperation operation, TArgument argument)
	{
		// this.VisitHelper(operation.Security, argument);
		// this.VisitHelper(operation.Callbacks, argument);
		if (operation.Responses != null)
			this.Visit(operation.Responses, argument);
		if (operation.RequestBody != null)
			this.Visit(operation.RequestBody, argument);
		foreach (var e in operation.Parameters)
			this.Visit(e, argument);
		// this.VisitHelper(operation.ExternalDocs, argument);
		// this.VisitHelper(operation.Servers, argument);
	}
	public virtual void Visit(OpenApiParameter parameter, TArgument argument) { }
	public virtual void Visit(OpenApiPath path, TArgument argument)
	{
		foreach (var e in path.Operations.Values)
			this.Visit(e, argument);
	}

	public virtual void Visit(OpenApiMediaTypeObject mediaTypeObject, TArgument argument) { }

	public virtual void Visit(OpenApiRequestBody requestBody, TArgument argument)
	{
		if (requestBody.Content != null)
			foreach (var e in requestBody.Content.Values)
				this.Visit(e, argument);
	}
	public virtual void Visit(OpenApiResponse response, TArgument argument)
	{
		foreach (var e in response.Headers)
			this.Visit(e, argument);
		if (response.Content != null)
			foreach (var e in response.Content.Values)
				this.Visit(e, argument);
	}
	public virtual void Visit(OpenApiResponses responses, TArgument argument)
	{
		foreach (var e in responses.StatusCodeResponses.Values)
			this.Visit(e, argument);
		if (responses.Default != null)
			this.Visit(responses.Default, argument);
	}

	public virtual void VisitUnknown(IReferenceableDocumentNode node, TArgument argument)
	{
		throw new NotSupportedException();
	}
}
