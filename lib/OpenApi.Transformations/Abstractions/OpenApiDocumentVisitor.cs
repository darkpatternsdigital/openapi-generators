using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

public abstract class OpenApiDocumentVisitor<TArgument> : IOpenApiDocumentVisitor<TArgument>
{
	public virtual void Visit(OpenApiContact contact, TArgument? argument) { }

	public virtual void Visit(OpenApiDocument document, TArgument argument)
	{
		// this.VisitHelper(document.Servers, argument);
		foreach (var e in document.Paths.Values)
			this.Visit(e, argument);
		this.Visit(document.Info, argument);
		foreach (var e in document.SecurityRequirements)
			this.Visit(e, argument);
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
	public virtual void Visit(OpenApiOperation operation, string method, TArgument argument)
	{
		foreach (var e in operation.SecurityRequirements)
			this.Visit(e, argument);
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
		foreach (var kvp in path.Operations)
			this.Visit(kvp.Value, kvp.Key, argument);
	}

	public virtual void Visit(OpenApiMediaTypeObject mediaTypeObject, TArgument argument) { }

	public virtual void Visit(OpenApiRequestBody requestBody, TArgument argument)
	{
		if (requestBody.Content != null)
			foreach (var e in requestBody.Content.Values)
				this.Visit(e, argument);
	}
	public virtual void Visit(OpenApiResponse response, int? statusCode, TArgument argument)
	{
		foreach (var e in response.Headers)
			this.Visit(e, argument);
		if (response.Content != null)
			foreach (var e in response.Content.Values)
				this.Visit(e, argument);
	}
	public virtual void Visit(OpenApiResponses responses, TArgument argument)
	{
		foreach (var kvp in responses.StatusCodeResponses)
			this.Visit(kvp.Value, kvp.Key, argument);
		if (responses.Default != null)
			this.Visit(responses.Default, null, argument);
	}

	public virtual void Visit(OpenApiSecurityRequirement securityRequirement, TArgument argument)
	{
	}

	public virtual void VisitUnknown(IReferenceableDocumentNode node, TArgument argument)
	{
		throw new NotSupportedException();
	}
}
