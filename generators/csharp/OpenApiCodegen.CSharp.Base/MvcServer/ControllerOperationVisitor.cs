using Json.Pointer;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Specifications.Keywords.Draft04;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.OpenApi.CSharp;
using DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

class ControllerOperationVisitor : OpenApiDocumentVisitor<ControllerOperationVisitor.Argument>
{
	private readonly ISchemaRegistry schemaRegistry;
	private readonly CSharpServerSchemaOptions options;
	private readonly CSharpInlineSchemas inlineSchemas;
	private readonly string controllerClassName;
	private readonly IReadOnlyList<OpenApiSecurityRequirement> documentSecurityRequirements;
	private readonly IReadOnlyDictionary<string, OpenApiSecurityScheme> securitySchemes;
	private readonly Func<Uri, string> toSourceReference;

	public record Argument(
		OpenApiTransformDiagnostic Diagnostic,
		RegisterControllerOperation RegisterControllerOperation,
		OperationBuilder? Builder = null,
		OpenApiPath? CurrentPath = null
	);
	public delegate void RegisterControllerOperation(Templates.ControllerOperation operation);

	public class OperationBuilder
	{
		public OperationBuilder(OpenApiOperation operation, string operationId)
		{
			Operation = operation;
			OperationId = operationId;
		}

		public List<Func<OperationParameter[], OperationRequestBody>> RequestBodies { get; } = [];

		public OperationResponse? DefaultResponse { get; set; }
		public Dictionary<int, OperationResponse> StatusResponses { get; } = [];
		public Dictionary<int, OperationResponse> RangeResponses { get; } = [];
		public List<OperationSecurityRequirement> SecurityRequirements { get; } = [];
		public List<Dictionary<string, string[]>> RawSecurityRequirements { get; } = [];
		public List<OperationParameter> SharedParameters { get; } = [];
		public OpenApiOperation Operation { get; }
		/// <summary>The operationId, or a name from the method and path when there is none</summary>
		public string OperationId { get; }
	}

	public ControllerOperationVisitor(ISchemaRegistry schemaRegistry, CSharpServerSchemaOptions options, string controllerClassName, IReadOnlyList<OpenApiSecurityRequirement> documentSecurityRequirements, IReadOnlyDictionary<string, OpenApiSecurityScheme> securitySchemes, Func<Uri, string> toSourceReference)
	{
		this.securitySchemes = securitySchemes;
		this.schemaRegistry = schemaRegistry;
		this.options = options;
		this.inlineSchemas = new CSharpInlineSchemas(options, schemaRegistry.DocumentRegistry);
		this.controllerClassName = controllerClassName;
		this.documentSecurityRequirements = documentSecurityRequirements;
		this.toSourceReference = toSourceReference;
	}

	public override void Visit(OpenApiPath path, Argument argument)
	{
		base.Visit(path, argument with { CurrentPath = path });
	}

	public override void VisitWebhook(OpenApiPath value, string key, Argument? argument)
	{
	}

	public override void Visit(OpenApiOperation operation, string httpMethod, Argument argument)
	{
		if (argument.CurrentPath is not OpenApiPath pathObj)
			throw new ArgumentException("Could not find path in argument; be sure to visit a whole OpenAPI doc", nameof(argument));
		var pathSuffix = JsonPointer.Parse(pathObj.Id.Fragment).Segments.Last().Value;
		var path = "/"
			+ (options.PathPrefix is { Length: > 1 }
				? options.PathPrefix.Trim('/') + "/"
				: "")
			+ pathSuffix.TrimStart('/');
		var operationId = operation.OperationId ?? $"{httpMethod} {path}";
		var builder = new OperationBuilder(operation, operationId);

		base.Visit(operation, httpMethod, argument with { Builder = builder });
		// Without its own `security`, the operation uses the document's requirements
		if (operation.SecurityRequirements == null)
			foreach (var securityRequirement in documentSecurityRequirements)
				Visit(securityRequirement, argument with { Builder = builder });

		// Requirements are alternatives, but separate [Authorize] attributes must all pass, so [Authorize] is only used
		// for schemes every requirement needs. The generated filter is added when that doesn't cover the
		// requirements: when there are alternatives, or OAuth2/OpenID Connect scopes to check.
		var securityRequirements = builder.SecurityRequirements
			.GroupBy(r => string.Join("\n", r.Schemes
				.Select(s => s.SchemeName + " " + string.Join(" ", s.ScopeNames.OrderBy(scope => scope, StringComparer.Ordinal)))
				.OrderBy(scheme => scheme, StringComparer.Ordinal)))
			.Select(duplicates => duplicates.First())
			.ToArray();
		var allowAnonymous = securityRequirements.Any(r => r.Schemes.Length == 0);
		var requiredSchemes = securityRequirements
			.Select(r => r.Schemes.Select(s => s.SchemeName))
			.Aggregate((IEnumerable<string>?)null, (common, schemes) => common?.Intersect(schemes) ?? schemes.Distinct())
			?.ToArray() ?? [];
		var useAuthorizeAttribute = securityRequirements is [var onlyRequirement] && onlyRequirement.Schemes.All(s => s.ScopeNames.Length == 0);
		string[] authorizePolicies =
			// With an empty requirement, the policies still authenticate the user when they can
			allowAnonymous ? [.. securityRequirements.SelectMany(r => r.Schemes).Select(s => s.SchemeName).Distinct()]
			: requiredSchemes;

		var sharedParameters = builder.SharedParameters.ToArray();
		argument.RegisterControllerOperation(
			new Templates.ControllerOperation(
				HttpMethod: CSharpNaming.ToTitleCaseIdentifier(httpMethod, []),
				Summary: operation.Summary,
				Description: operation.Description,
				Name: CSharpNaming.ToTitleCaseIdentifier(operationId, options.ReservedIdentifiers("ControllerBase", controllerClassName)),
				Path: path,
				RequestBodies: builder.RequestBodies.DefaultIfEmpty(OperationRequestBodyFactory(operationId, null, Enumerable.Empty<OperationParameter>())).Select(transform => transform(sharedParameters)).ToArray(),
				Responses: new Templates.OperationResponses(
					DefaultResponse: builder.DefaultResponse,
					StatusResponse: new(builder.StatusResponses),
					RangeResponse: new(builder.RangeResponses)
				),
				SecurityRequirements: securityRequirements,
				RawSecurityRequirements: [.. builder.RawSecurityRequirements],
				AllowAnonymous: allowAnonymous,
				AuthorizePolicies: authorizePolicies,
				UseSecurityRequirementsAttribute: securityRequirements.Length > 0 && !allowAnonymous && !useAuthorizeAttribute,
				SourceReference: toSourceReference(operation.Id)
			));
	}

	public override void Visit(OpenApiParameter param, Argument argument)
	{
		if (param.Schema != null)
			schemaRegistry.EnsureSchemaRegistered(param.Schema);
		var dataType = inlineSchemas.ToInlineDataType(param.Schema) ?? CSharpInlineSchemas.AnyObject;
		if (!param.Required)
		{
			// Path/Query/Header/Cookie parameters can't really be nullable, but rather than using custom ModelBinding on Optional<T>, we use nullability.
			dataType = dataType.MakeNullable();
		}
		var info = param.Schema?.ResolveSchemaInfo();
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
			Optional: false,
			Required: param.Required,
			Pattern: info?.TryGetAnnotation<PatternKeyword>()?.Pattern,
			MinLength: info?.TryGetAnnotation<MinLengthKeyword>()?.Value ?? info?.TryGetAnnotation<MinItemsKeyword>()?.Value,
			MaxLength: info?.TryGetAnnotation<MaxLengthKeyword>()?.Value ?? info?.TryGetAnnotation<MaxItemsKeyword>()?.Value,
			Minimum: info?.TryGetAnnotation<MinimumKeyword>()?.Value,
			Maximum: info?.TryGetAnnotation<MaximumKeyword>()?.Value,
			ExclusiveMinimum: info?.TryGetAnnotation<ExclusiveMinimumKeyword>()?.IsExclusive ?? false,
			ExclusiveMaximum: info?.TryGetAnnotation<ExclusiveMaximumKeyword>()?.IsExclusive ?? false,
			MultipleOf: info?.TryGetAnnotation<MultipleOfKeyword>()?.Value,
			UniqueItems: info?.TryGetAnnotation<UniqueItemsKeyword>()?.MustBeUnique ?? false,
			MinProperties: info?.TryGetAnnotation<MinPropertiesKeyword>()?.Value,
			MaxProperties: info?.TryGetAnnotation<MaxPropertiesKeyword>()?.Value
		));
	}

	public override void Visit(OpenApiResponse response, int? statusCode, Argument argument) =>
		VisitResponse(response, statusCode, null, argument);
	public override void VisitStatusCodeRange(OpenApiResponse response, int statusCodeClass, Argument argument) =>
		VisitResponse(response, null, statusCodeClass, argument);
	private void VisitResponse(OpenApiResponse response, int? statusCode, int? statusCodeClass, Argument argument)
	{
		string statusCodeName;
		if (statusCodeClass.HasValue)
			statusCodeName = HttpStatusCodes.StatusCodeRangeNames[statusCodeClass.Value];
		else if (!statusCode.HasValue)
			statusCodeName = "other status code";
		else if (!HttpStatusCodes.StatusCodeNames.TryGetValue(statusCode.Value, out statusCodeName))
			statusCodeName = $"status code {statusCode}";
		if (argument.Builder == null)
			throw new ArgumentException("Argument is not ready", nameof(argument));
		if (argument.Builder?.Operation is not OpenApiOperation op)
			throw new ArgumentException("Could not find operation in argument; be sure to visit a whole OpenAPI doc", nameof(argument));
		var content = response.Content;
		var contentCount = content?.Count ?? 0;

		var result = new OperationResponse(
			Description: response.Description,
			Content: (from entry in (from c in content ?? Enumerable.Empty<KeyValuePair<string, OpenApiMediaTypeObject>>()
									 select (c.Key, c.Value.Schema)).DefaultIfEmpty((Key: "", Schema: null))
					  let dataType = inlineSchemas.ToInlineDataType(entry.Schema)
					  select new OperationResponseContentOption(
						  MediaType: entry.Key,
						  ResponseMethodName: CSharpNaming.ToMethodName($"{(contentCount > 1 ? entry.Key : "")} {statusCodeName}", options.ReservedIdentifiers()),
						  DataType: dataType?.Text
					  )).ToArray(),
			Headers: (from entry in response.Headers
					  let required = entry.Required
					  let dataType = inlineSchemas.ToInlineDataType(entry.Schema) ?? CSharpInlineSchemas.AnyObject
					  let info = entry.Schema?.ResolveSchemaInfo()
					  select new Templates.OperationResponseHeader(
						RawName: entry.Name,
						ParamName: CSharpNaming.ToParameterName("header " + entry.Name, options.ReservedIdentifiers()),
						Description: entry.Description,
						DataType: dataType.Text,
						DataTypeNullable: dataType.Nullable,
						Required: entry.Required,
						Pattern: info?.TryGetAnnotation<PatternKeyword>()?.Pattern,
						MinLength: info?.TryGetAnnotation<MinLengthKeyword>()?.Value,
						MaxLength: info?.TryGetAnnotation<MaxLengthKeyword>()?.Value,
						Minimum: info?.TryGetAnnotation<MinimumKeyword>()?.Value,
						Maximum: info?.TryGetAnnotation<MaximumKeyword>()?.Value
					  )).ToArray()
		);

		if (statusCodeClass.HasValue)
			argument.Builder.RangeResponses.Add(statusCodeClass.Value, result);
		else if (statusCode.HasValue)
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
		var kind = MediaTypes.Classify(mimeType);
		var bodyRequired = argument.Builder?.Operation.RequestBody?.Required ?? false;

		var singleContentType = argument.Builder?.Operation.RequestBody?.Content?.Count is not > 1;

		var mediaTypeSchemaInfo = mediaType.Schema?.ResolveSchemaInfo();
		argument.Builder?.RequestBodies.Add(OperationRequestBodyFactory(argument.Builder?.OperationId + (singleContentType ? "" : mimeType), mimeType, isForm ? GetFormParams() : GetStandardParams()));

		IEnumerable<OperationParameter> GetFormParams() =>
			from param in mediaTypeSchemaInfo?.TryGetAnnotation<PropertiesKeyword>()?.Properties
			let required = mediaTypeSchemaInfo?.TryGetAnnotation<RequiredKeyword>()?.RequiredProperties.Contains(param.Key) ?? false
			let info = param.Value?.ResolveSchemaInfo()
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
				Optional: !required,
				Pattern: info?.TryGetAnnotation<PatternKeyword>()?.Pattern,
				MinLength: info?.TryGetAnnotation<MinLengthKeyword>()?.Value ?? info?.TryGetAnnotation<MinItemsKeyword>()?.Value,
				MaxLength: info?.TryGetAnnotation<MaxLengthKeyword>()?.Value ?? info?.TryGetAnnotation<MaxItemsKeyword>()?.Value,
				Minimum: info?.TryGetAnnotation<MinimumKeyword>()?.Value,
				Maximum: info?.TryGetAnnotation<MaximumKeyword>()?.Value,
				ExclusiveMinimum: info?.TryGetAnnotation<ExclusiveMinimumKeyword>()?.IsExclusive ?? false,
				ExclusiveMaximum: info?.TryGetAnnotation<ExclusiveMaximumKeyword>()?.IsExclusive ?? false,
				MultipleOf: info?.TryGetAnnotation<MultipleOfKeyword>()?.Value,
				UniqueItems: info?.TryGetAnnotation<UniqueItemsKeyword>()?.MustBeUnique ?? false,
				MinProperties: info?.TryGetAnnotation<MinPropertiesKeyword>()?.Value,
				MaxProperties: info?.TryGetAnnotation<MaxPropertiesKeyword>()?.Value
			);
		IEnumerable<OperationParameter> GetStandardParams() =>
			from ct in new[] { mediaType }
			let info = ct.Schema?.ResolveSchemaInfo()
			let dataType = inlineSchemas.ToInlineDataType(ct.Schema)
			// Text and binary bodies are read from the request directly, so they need no input formatter
			let isTextBody = kind == MediaTypeKind.Text && dataType.Text is "string" or "string?"
			let isStreamBody = kind != MediaTypeKind.Json && dataType.Text is "global::System.IO.Stream" or "global::System.IO.Stream?"
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
				IsBodyParam: !isTextBody && !isStreamBody,
				IsFormParam: false,
				Required: bodyRequired,
				Optional: !bodyRequired,
				Pattern: info?.TryGetAnnotation<PatternKeyword>()?.Pattern,
				MinLength: info?.TryGetAnnotation<MinLengthKeyword>()?.Value ?? info?.TryGetAnnotation<MinItemsKeyword>()?.Value,
				MaxLength: info?.TryGetAnnotation<MaxLengthKeyword>()?.Value ?? info?.TryGetAnnotation<MaxItemsKeyword>()?.Value,
				Minimum: info?.TryGetAnnotation<MinimumKeyword>()?.Value,
				Maximum: info?.TryGetAnnotation<MaximumKeyword>()?.Value,
				ExclusiveMinimum: info?.TryGetAnnotation<ExclusiveMinimumKeyword>()?.IsExclusive ?? false,
				ExclusiveMaximum: info?.TryGetAnnotation<ExclusiveMaximumKeyword>()?.IsExclusive ?? false,
				MultipleOf: info?.TryGetAnnotation<MultipleOfKeyword>()?.Value,
				UniqueItems: info?.TryGetAnnotation<UniqueItemsKeyword>()?.MustBeUnique ?? false,
				MinProperties: info?.TryGetAnnotation<MinPropertiesKeyword>()?.Value,
				MaxProperties: info?.TryGetAnnotation<MaxPropertiesKeyword>()?.Value,
				IsTextBodyParam: isTextBody,
				IsStreamBodyParam: isStreamBody
			);
	}

	private Func<OperationParameter[], OperationRequestBody> OperationRequestBodyFactory(string operationName, string? requestBodyMimeType, IEnumerable<OperationParameter> parameters)
	{
		return sharedParams => new Templates.OperationRequestBody(
				Name: CSharpNaming.ToTitleCaseIdentifier(operationName, options.ReservedIdentifiers()),
				RequestBodyType: requestBodyMimeType,
				AllParams: sharedParams.Concat(parameters)
			);
	}

	public override void Visit(OpenApiSecurityRequirement securityRequirement, Argument argument)
	{
		argument.Builder?.SecurityRequirements.Add(new OperationSecurityRequirement(
							 (from scheme in securityRequirement.SchemeRequirements
								  // OpenAPI 3.0 defines scopes only for OAuth2 and OpenID Connect; on other schemes they're left to the securityAttribute
							  let enforceScopes = securitySchemes.TryGetValue(scheme.SchemeName, out var definition) && definition.Type is SecuritySchemeType.OAuth2 or SecuritySchemeType.OpenIdConnect
							  select new Templates.OperationSecuritySchemeRequirement(scheme.SchemeName, enforceScopes ? scheme.ScopeNames.ToArray() : [])).ToArray())
						 );
		argument.Builder?.RawSecurityRequirements.Add(
			securityRequirement.SchemeRequirements.ToDictionary(req => req.SchemeName, req => req.ScopeNames.ToArray())
		);
	}
}
