using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Validation;
using DarkPatterns.OpenApi.TypeScript;
using DarkPatterns.OpenApiCodegen.Client.TypeScript.Templates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript
{

	class OperationBuilderVisitor : OpenApiDocumentVisitor<OperationBuilderVisitor.Argument>
	{
		private const string formMimeType = "application/x-www-form-urlencoded";
		private readonly TypeScriptInlineSchemas inlineSchemas;
		private readonly TypeScriptSchemaOptions options;

		public record Argument(
			OpenApiTransformDiagnostic Diagnostic,
			OperationBuilder Builder,
			OpenApiPath? CurrentPath = null
		);

		public class OperationBuilder
		{
			public OperationBuilder(OpenApiOperation operation)
			{
				Operation = operation;
			}

			public List<OperationRequestBody> RequestBodies { get; } = new();

			public OperationResponse? DefaultResponse { get; set; }
			public Dictionary<int, OperationResponse> StatusResponses { get; } = new();
			public List<OperationSecurityRequirement> SecurityRequirements { get; } = new();
			public List<OperationParameter> SharedParameters { get; } = new();
			public OpenApiOperation Operation { get; }
		}

		public OperationBuilderVisitor(DocumentRegistry registry, TypeScriptSchemaOptions options)
		{
			this.inlineSchemas = new TypeScriptInlineSchemas(options, registry);
			this.options = options;
		}

		public override void Visit(OpenApiPath path, Argument argument)
		{
			base.Visit(path, argument with { CurrentPath = path });
		}

		public override void Visit(OpenApiOperation operation, string httpMethod, Argument argument)
		{
			if (argument.CurrentPath is not OpenApiPath)
				throw new ArgumentException("Could not find path in argument; be sure to visit a whole OpenAPI doc", nameof(argument));

			base.Visit(operation, httpMethod, argument);
		}

		public override void Visit(OpenApiParameter param, Argument argument)
		{
			var dataType = inlineSchemas.ToInlineDataType(param.Schema);
			argument.Builder?.SharedParameters.Add(new Templates.OperationParameter(
				RawName: param.Name,
				RawNameWithCurly: $"{{{param.Name}}}",
				ParamName: TypeScriptNaming.ToParameterName(param.Name, options.ReservedIdentifiers()),
				Description: param.Description,
				DataType: dataType.Text,
				DataTypeEnumerable: dataType.IsEnumerable,
				DataTypeNullable: dataType.Nullable,
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

		internal Operation ToOperationTemplate(OpenApiOperation operation, string httpMethod, string path, OperationBuilder builder)
		{
			var sharedParameters = builder.SharedParameters.ToArray();
			var operationId = operation.OperationId ?? $"{httpMethod} {path}";
			return (
				new Templates.Operation(
					HttpMethod: httpMethod,
					Summary: operation.Summary,
					Description: operation.Description,
					Name: TypeScriptNaming.ToMethodName(operationId, options.ReservedIdentifiers()),
					Path: path,
					AllowNoBody: operation.RequestBody is not { Required: true, Content.Count: > 1, },
					HasFormRequest: operation.RequestBody?.Content?.Any(kvp => kvp.Key == formMimeType) ?? false,
					Imports: inlineSchemas.GetImportStatements(GetSchemas(), Enumerable.Empty<JsonSchema>(), "./operation/").ToArray(),
					SharedParams: sharedParameters,
					RequestBodies: builder.RequestBodies.ToArray(),
					Responses: new Templates.OperationResponses(
						DefaultResponse: builder.DefaultResponse,
						StatusResponse: new(builder.StatusResponses)
					),
					SecurityRequirements: builder.SecurityRequirements.ToArray(),
					HasQueryParams: operation.Parameters.Any(p => p.In == ParameterLocation.Query)
				));

			IEnumerable<JsonSchema> GetSchemas()
			{
				return from set in new[]
						{
						from resp in GetResponses()
						from body in resp.Content?.Values ?? Enumerable.Empty<OpenApiMediaTypeObject>()
						select body.Schema,
						from p in operation.Parameters
						select p.Schema,
						from mediaType in operation.RequestBody?.Content?.Values ?? Enumerable.Empty<OpenApiMediaTypeObject>()
						select mediaType.Schema
					}
					   from schema in set
					   select schema;

			}
			IEnumerable<OpenApiResponse> GetResponses()
			{
				if (operation.Responses == null) return Enumerable.Empty<OpenApiResponse>();
				var result = operation.Responses.StatusCodeResponses.Values;
				if (operation.Responses.Default != null)
					result = result.Append(operation.Responses.Default);
				return result;
			}
		}

		public override void Visit(OpenApiResponse response, int? statusCode, Argument argument)
		{
			string? statusCodeName;
			if (!statusCode.HasValue)
				statusCodeName = "other status code";
			else if (!HttpStatusCodes.StatusCodeNames.TryGetValue(statusCode.Value, out statusCodeName))
				statusCodeName = $"status code {statusCode}";
			var content = response.Content;
			var contentCount = content?.Count ?? 0;

			var result = new OperationResponse(
				Description: response.Description,
				Content: (from entry in (from c in content ?? Enumerable.Empty<KeyValuePair<string, OpenApiMediaTypeObject>>()
										 select (c.Key, c.Value.Schema)).DefaultIfEmpty((Key: "", Schema: null))
						  where entry.Key is { Length: 0 } || options.AllowedMimeTypes.Contains(entry.Key)
						  let dataType = inlineSchemas.ToInlineDataType(entry.Schema)
						  select new OperationResponseContentOption(
							  MediaType: entry.Key,
							  ResponseMethodName: TypeScriptNaming.ToTitleCaseIdentifier($"{(contentCount > 1 ? entry.Key : "")} {statusCodeName}", options.ReservedIdentifiers()),
							  DataType: dataType.Text
						  )).ToArray(),
				Headers: (from entry in response.Headers
						  let required = entry.Required
						  let dataType = inlineSchemas.ToInlineDataType(entry.Schema) ?? TypeScriptInlineSchemas.AnyObject
						  select new Templates.OperationResponseHeader(
							  RawName: entry.Name,
							  ParamName: TypeScriptNaming.ToParameterName("header " + entry.Name, options.ReservedIdentifiers()),
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
				argument.Builder?.StatusResponses.Add(statusCode.Value, result);
			else
				argument.Builder.DefaultResponse = result;
		}

		public override void Visit(OpenApiMediaTypeObject mediaType, Argument argument)
		{
			// All media type visitations should be for request bodies
			var mimeType = mediaType.GetLastContextPart();
			if (!options.AllowedMimeTypes.Contains(mimeType))
				return;

			var isForm = mimeType == formMimeType;

			var singleContentType = argument.Builder?.Operation.RequestBody?.Content?.Count is not > 1;

			argument.Builder?.RequestBodies.Add(OperationRequestBody(mimeType, isForm, isForm ? GetFormParams() : GetStandardParams()));

			IEnumerable<OperationParameter> GetFormParams() =>
				from param in mediaType.Schema?.TryGetAnnotation<PropertiesKeyword>()?.Properties
				let required = mediaType.Schema?.TryGetAnnotation<RequiredKeyword>()?.RequiredProperties.Contains(param.Key) ?? false
				let dataType = inlineSchemas.ToInlineDataType(param.Value)
				select new Templates.OperationParameter(
					RawName: param.Key,
					RawNameWithCurly: $"{{{param.Key}}}",
					ParamName: TypeScriptNaming.ToParameterName(param.Key, options.ReservedIdentifiers()),
					Description: null,
					DataType: dataType.Text,
					DataTypeEnumerable: dataType.IsEnumerable,
					DataTypeNullable: dataType.Nullable,
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
				   RawNameWithCurly: null,
				   ParamName: TypeScriptNaming.ToParameterName("body", options.ReservedIdentifiers()),
				   Description: null,
				   DataType: dataType.Text,
				   DataTypeEnumerable: dataType.IsEnumerable,
				   DataTypeNullable: dataType.Nullable,
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

		public static OperationRequestBody OperationRequestBody(string requestBodyMimeType, bool isForm, IEnumerable<OperationParameter> parameters)
		{
			return new Templates.OperationRequestBody(
				 RequestBodyType: requestBodyMimeType,
				 IsForm: isForm,
				 AllParams: parameters
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
}