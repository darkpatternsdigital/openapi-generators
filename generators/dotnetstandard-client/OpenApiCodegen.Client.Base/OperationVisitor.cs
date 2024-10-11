using Json.Pointer;
using DarkPatterns.OpenApi.CSharp.Templates;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.OpenApi.CSharp;

class OperationVisitor : OpenApiDocumentVisitor<OperationVisitor.Argument>
{
	private readonly DocumentRegistry documentRegistry;
	private readonly ISchemaRegistry schemaRegistry;
	private readonly CSharpSchemaOptions options;
	private readonly CSharpInlineSchemas inlineSchemas;
	private readonly string controllerClassName;

	public record Argument(
		OpenApiTransformDiagnostic Diagnostic,
		RegisterControllerOperation RegisterControllerOperation,
		OperationBuilder? Builder = null,
		OpenApiPath? CurrentPath = null,
		OpenApiOperation? CurrentOperation = null
	);
	public delegate void RegisterControllerOperation(Templates.Operation operation);

	public class OperationBuilder
	{
		public OperationBuilder(OpenApiOperation operation)
		{
			Operation = operation;
		}

		public List<Func<OperationParameter[], OperationRequestBody>> RequestBodies { get; } = new();

		public OperationResponse? DefaultResponse { get; set; }
		public Dictionary<int, OperationResponse> StatusResponses { get; } = new();
		public List<OperationSecurityRequirement> SecurityRequirements { get; } = new();
		public List<OperationParameter> SharedParameters { get; } = new();
		public OpenApiOperation Operation { get; }
	}

	public OperationVisitor(DocumentRegistry documentRegistry, ISchemaRegistry schemaRegistry, CSharpSchemaOptions options, string controllerClassName)
	{
		this.documentRegistry = documentRegistry;
		this.schemaRegistry = schemaRegistry;
		this.options = options;
		this.inlineSchemas = new(options, documentRegistry);
		this.controllerClassName = controllerClassName;
	}

	public override void Visit(OpenApiPath path, Argument argument)
	{
		base.Visit(path, argument with { CurrentPath = path });
	}

	public override void Visit(OpenApiOperation operation, string httpMethod, Argument argument)
	{
		if (argument.CurrentPath is not OpenApiPath pathObj)
			throw new ArgumentException("Could not find path in argument; be sure to visit a whole OpenAPI doc", nameof(argument));
		var path = JsonPointer.Parse(pathObj.Id.Fragment).Segments.Last().Value;
		if (path == null)
			throw new ArgumentException("Context is not initialized properly - key expected for path items", nameof(argument));

		var builder = new OperationBuilder(operation);

		var operationId = operation.OperationId ?? (httpMethod + path);
		var noBody = OperationRequestBodyFactory(operationId, null, Enumerable.Empty<OperationParameter>(), false);
		if (operation.RequestBody == null || !operation.RequestBody.Required)
			builder.RequestBodies.Add(noBody);

		base.Visit(operation, httpMethod, argument with { Builder = builder, CurrentOperation = operation });

		var sharedParameters = builder.SharedParameters.ToArray();
		var requestBodies = builder.RequestBodies.DefaultIfEmpty(noBody).Select(transform => transform(sharedParameters))
			.Where(body => body.RequestBodyType != "application/xml") // exclude xml, since we don't support it
			.ToArray();
		if (requestBodies.Length > 0)
		{
			argument.RegisterControllerOperation(
				new Templates.Operation(
				 HttpMethod: CSharpNaming.ToTitleCaseIdentifier(httpMethod, []),
				 Summary: operation.Summary,
				 Description: operation.Description,
				 Name: CSharpNaming.ToTitleCaseIdentifier(operationId, options.ReservedIdentifiers("ControllerBase", controllerClassName)),
				 Path: path,
				 RequestBodies: requestBodies,
				 HasQueryStringEmbedded: path.Contains("?"),
				 Responses: new Templates.OperationResponses(
					 DefaultResponse: builder.DefaultResponse,
					 StatusResponse: new(builder.StatusResponses)
				 ),
				 SecurityRequirements: builder.SecurityRequirements.ToArray()
			 ));
		}
	}

	public override void Visit(OpenApiParameter param, Argument argument)
	{
		var dataType = inlineSchemas.ToInlineDataType(param.Schema) ?? CSharpInlineSchemas.AnyObject;
		argument.Builder?.SharedParameters.Add(new Templates.OperationParameter(
			RawName: param.Name,
			ParamName: CSharpNaming.ToParameterName(param.Name, options.ReservedIdentifiers()),
			Description: param.Description,
			DataType: dataType.Text,
			DataTypeNullable: dataType.Nullable,
			DataTypeEnumerable: dataType.IsEnumerable,
			IsPathParam: param.In == ParameterLocation.Path,
			IsQueryParam: param.In == ParameterLocation.Query,
			IsHeaderParam: param.In == ParameterLocation.Header,
			IsCookieParam: param.In == ParameterLocation.Cookie,
			IsBodyParam: false,
			IsFormParam: false,
			Required: param.Required,
			Pattern: param.Schema?.TryGetAnnotation<PatternKeyword>()?.Pattern,
			MinLength: param.Schema?.TryGetAnnotation<MinLengthKeyword>()?.Value,
			MaxLength: param.Schema?.TryGetAnnotation<MaxLengthKeyword>()?.Value,
			Minimum: param.Schema?.TryGetAnnotation<MinimumKeyword>()?.Value,
			Maximum: param.Schema?.TryGetAnnotation<MaximumKeyword>()?.Value
		));
	}
	public override void Visit(OpenApiResponse response, int? statusCode, Argument argument)
	{
		string statusCodeName;
		if (!statusCode.HasValue)
			statusCodeName = "other status code";
		else if (!HttpStatusCodes.StatusCodeNames.TryGetValue(statusCode.Value, out statusCodeName))
			statusCodeName = $"status code {statusCode}";
		if (argument.Builder?.Operation is not OpenApiOperation op)
			throw new ArgumentException("Could not find operation in argument; be sure to visit a whole OpenAPI doc", nameof(argument));
		var content = response.Content;
		var contentCount = content?.Count ?? 0;

		var result = new OperationResponse(
			Description: response.Description,
			Content: (from entry in (from c in content ?? Enumerable.Empty<KeyValuePair<string, OpenApiMediaTypeObject>>()
									 select (c.Key, c.Value.Schema)).DefaultIfEmpty((Key: "", Schema: null))
					  let dataType = entry.Schema != null ? inlineSchemas.ToInlineDataType(entry.Schema) : null
					  where entry.Key != "application/xml" // exclude xml, since we don't support it
					  select new OperationResponseContentOption(
						  MediaType: entry.Key,
						  ResponseMethodName: CSharpNaming.ToTitleCaseIdentifier($"{(contentCount > 1 ? entry.Key : "")} {statusCodeName}", options.ReservedIdentifiers()),
						  DataType: dataType?.Text
					  )).ToArray(),
			Headers: (from entry in response.Headers
					  let required = entry.Required
					  let dataType = inlineSchemas.ToInlineDataType(entry.Schema)
					  select new Templates.OperationResponseHeader(
						  RawName: entry.Name,
						  ParamName: CSharpNaming.ToParameterName("header " + entry.Name, options.ReservedIdentifiers()),
						  Description: entry.Description,
						  DataType: dataType.Text,
						  DataTypeNullable: dataType.Nullable,
						  Required: entry.Required,
						  Pattern: entry.Schema?.TryGetAnnotation<PatternKeyword>()?.Pattern,
						  MinLength: entry.Schema?.TryGetAnnotation<MinLengthKeyword>()?.Value,
						  MaxLength: entry.Schema?.TryGetAnnotation<MaxLengthKeyword>()?.Value,
						  Minimum: entry.Schema?.TryGetAnnotation<MinimumKeyword>()?.Value,
						  Maximum: entry.Schema?.TryGetAnnotation<MaximumKeyword>()?.Value
					  )).ToArray()
		);

		if (statusCode.HasValue)
			argument.Builder.StatusResponses.Add(statusCode.Value, result);
		else
			argument.Builder.DefaultResponse = result;
	}
	//public override void Visit(OpenApiRequestBody requestBody, Argument argument)
	//{
	//}
	public override void Visit(OpenApiMediaTypeObject mediaType, Argument argument)
	{
		// All media type visitations should be for request bodies
		var mimeType = mediaType.GetLastContextPart();

		var isForm = mimeType == "application/x-www-form-urlencoded";

		var singleContentType = argument.Builder?.Operation.RequestBody?.Content?.Count is not > 1;

		argument.Builder?.RequestBodies.Add(OperationRequestBodyFactory(argument.Builder?.Operation.OperationId + (singleContentType ? "" : mimeType), mimeType, isForm ? GetFormParams() : GetStandardParams(), isForm));

		IEnumerable<OperationParameter> GetFormParams() =>
			from param in mediaType.Schema?.TryGetAnnotation<PropertiesKeyword>()?.Properties
			let required = mediaType.Schema?.TryGetAnnotation<RequiredKeyword>()?.RequiredProperties.Contains(param.Key) ?? false
			let dataType = inlineSchemas.ToInlineDataType(param.Value)
			select new Templates.OperationParameter(
				RawName: param.Key,
				ParamName: CSharpNaming.ToParameterName(param.Key, options.ReservedIdentifiers()),
				Description: null,
				DataType: dataType.Text,
				DataTypeNullable: dataType.Nullable,
				DataTypeEnumerable: dataType.IsEnumerable,
				IsPathParam: false,
				IsQueryParam: false,
				IsHeaderParam: false,
				IsCookieParam: false,
				IsBodyParam: false,
				IsFormParam: true,
				Required: required,
				Pattern: param.Value?.TryGetAnnotation<PatternKeyword>()?.Pattern,
				MinLength: param.Value?.TryGetAnnotation<MinLengthKeyword>()?.Value,
				MaxLength: param.Value?.TryGetAnnotation<MaxLengthKeyword>()?.Value,
				Minimum: param.Value?.TryGetAnnotation<MinimumKeyword>()?.Value,
				Maximum: param.Value?.TryGetAnnotation<MaximumKeyword>()?.Value
			);
		IEnumerable<OperationParameter> GetStandardParams() =>
			from ct in new[] { mediaType }
			let dataType = inlineSchemas.ToInlineDataType(ct.Schema)
			select new Templates.OperationParameter(
				RawName: null,
				ParamName: CSharpNaming.ToParameterName(argument.Builder?.Operation.OperationId + " body", options.ReservedIdentifiers()),
				Description: null,
				DataType: dataType.Text,
				DataTypeNullable: dataType.Nullable,
				DataTypeEnumerable: dataType.IsEnumerable,
				IsPathParam: false,
				IsQueryParam: false,
				IsHeaderParam: false,
				IsCookieParam: false,
				IsBodyParam: true,
				IsFormParam: false,
				Required: true,
				Pattern: mediaType.Schema?.TryGetAnnotation<PatternKeyword>()?.Pattern,
				MinLength: mediaType.Schema?.TryGetAnnotation<MinLengthKeyword>()?.Value,
				MaxLength: mediaType.Schema?.TryGetAnnotation<MaxLengthKeyword>()?.Value,
				Minimum: mediaType.Schema?.TryGetAnnotation<MinimumKeyword>()?.Value,
				Maximum: mediaType.Schema?.TryGetAnnotation<MaximumKeyword>()?.Value
		   );
	}

	private Func<OperationParameter[], OperationRequestBody> OperationRequestBodyFactory(string operationName, string? requestBodyMimeType, IEnumerable<OperationParameter> parameters, bool isForm)
	{
		return sharedParams => new Templates.OperationRequestBody(
			 Name: CSharpNaming.ToTitleCaseIdentifier(operationName, options.ReservedIdentifiers()),
			 IsForm: isForm,
			 IsFile: parameters.Any(t => t.IsFile),
			 HasQueryParam: sharedParams.Concat(parameters).Any(p => p.IsQueryParam),
			 RequestBodyType: requestBodyMimeType,
			 AllParams: sharedParams.Concat(parameters)
		 );
	}

	public override void Visit(OpenApiSecurityRequirement securityRequirement, Argument argument)
	{
		argument.Builder?.SecurityRequirements.Add(new OperationSecurityRequirement(
							 (from scheme in securityRequirement.SchemeRequirements
							  select new Templates.OperationSecuritySchemeRequirement(scheme.SchemeName, scheme.ScopeNames.ToArray())).ToArray())
						 );
	}
}
