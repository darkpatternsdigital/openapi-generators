using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Metadata;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Specifications.v3_0;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Xunit;
using static DarkPatterns.OpenApiCodegen.TestUtils.DocumentHelpers;
using Draft04 = DarkPatterns.Json.Specifications.Keywords.Draft04;
using ItemsKeyword = DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator.ItemsKeyword;

namespace DarkPatterns.OpenApi.Transformations;

/// <summary>
/// Covers the OpenAPI 3.0.3 specification (https://spec.openapis.org/oas/v3.0.3) at the parser / document-model layer.
/// </summary>
/// <remarks>
/// Fixtures are <c>schemas/oas-parser-*.yml</c>. The <c>.yml</c> extension keeps them out of the TypeScript client
/// tests, which generate and compile code for every <c>schemas/*.yaml</c>.
/// </remarks>
public class OpenApi3_0SpecificationShould
{
	private const string DocumentYaml = "oas-parser-document.yml";
	private const string PathsYaml = "oas-parser-paths.yml";
	private const string ParametersYaml = "oas-parser-parameters.yml";
	private const string ResponsesYaml = "oas-parser-responses.yml";
	private const string SchemasYaml = "oas-parser-schemas.yml";

	#region Helpers

	private static ParseResult<OpenApiDocument> Parse(string yamlName)
	{
		var registry = DocumentLoader.CreateRegistry();
		return GetOpenApiDocument(yamlName, registry);
	}

	private static OpenApiDocument ParseValid(string yamlName)
	{
		var result = Parse(yamlName);
		AssertNoDiagnostics(result);
		Assert.NotNull(result.Result);
		return result.Result;
	}

	private static void AssertNoDiagnostics(ParseResult<OpenApiDocument> result) =>
		Assert.True(result.Diagnostics.Count == 0, "Unexpected diagnostics:\n" + string.Join("\n", result.Diagnostics));

	private static void AssertProperDiagnostic(ParseResult<OpenApiDocument> result)
	{
		Assert.True(result.Diagnostics.Count > 0, "Expected a diagnostic, but none were reported");
		Assert.True(
			!result.Diagnostics.OfType<UnhandledExceptionDiagnostic>().Any(),
			"Expected a specific diagnostic, but got:\n" + string.Join("\n", result.Diagnostics));
	}

	private static string IdOf(string yamlName, string pointer) => $"proj://embedded/{yamlName}#{pointer}";

	/// <summary>
	/// Every node in the document model, by original Id
	/// </summary>
	private static IReadOnlyDictionary<string, IJsonDocumentNode> AllNodes(IJsonDocumentNode root)
	{
		var result = new Dictionary<string, IJsonDocumentNode>();
		var stack = new Stack<IJsonDocumentNode>([root]);
		while (stack.Count > 0)
		{
			var next = stack.Pop();
			if (!result.TryAdd(next.Metadata.Id.OriginalString, next))
				continue;
			foreach (var nested in next.GetNestedNodes())
				stack.Push(nested);
		}
		return result;
	}

	private static IJsonDocumentNode AssertModelsNode(OpenApiDocument document, string yamlName, string pointer)
	{
		var nodes = AllNodes(document);
		Assert.True(nodes.TryGetValue(IdOf(yamlName, pointer), out var node), $"The document model does not include {pointer}");
		return node;
	}

	/// <summary>
	/// Reads a property that the document model does not (yet) have, by name
	/// </summary>
	private static object? ModelProperty(object target, string propertyName)
	{
		var property = target.GetType().GetProperty(propertyName);
		Assert.True(property != null, $"{target.GetType().Name} does not model `{propertyName}`");
		return property.GetValue(target);
	}

	private static OpenApiOperation Operation(OpenApiDocument document, string path, string method) =>
		document.Paths[path].Operations[method];

	private static OpenApiParameter Parameter(OpenApiDocument document, string path, string method, string name) =>
		Assert.Single(Operation(document, path, method).Parameters, p => p.Name == name);

	private static JsonSchema ComponentSchema(OpenApiDocument document, string propertyName)
	{
		var responseSchema = Operation(document, "/schemas", "get").Responses?.StatusCodeResponses[200].Content?["application/json"].Schema;
		Assert.NotNull(responseSchema);
		var properties = Assert.Single(responseSchema.Annotations.OfType<PropertiesKeyword>());
		return Dereference(properties.Properties[propertyName]);
	}

	private static JsonSchema Property(JsonSchema schema, string propertyName) =>
		Dereference(Assert.Single(schema.Annotations.OfType<PropertiesKeyword>()).Properties[propertyName]);

	private static JsonSchema Dereference(JsonSchema schema)
	{
		while (schema.Annotations.OfType<IJsonSchemaRefAnnotation>().SingleOrDefault()?.ReferencedSchema is JsonSchema target)
			schema = target;
		return schema;
	}

	#endregion

	#region OpenAPI Object / versions

	[InlineData("oas-parser-version-3.0.0.yml", "3.0.0")]
	[InlineData("oas-parser-version-3.0.1.yml", "3.0.1")]
	[InlineData(DocumentYaml, "3.0.3")]
	[Theory]
	public void Accept_openapi_3_0_versions(string yamlName, string version)
	{
		var document = ParseValid(yamlName);

		Assert.Equal(("openapi", version), (document.OpenApiSpecVersion.DocumentType, document.OpenApiSpecVersion.Version));
	}

	[InlineData("oas-parser-version-3.1.0.yml")]
	[InlineData("oas-parser-swagger-2.0.yml")]
	[Theory]
	public void Reject_non_3_0_documents(string yamlName)
	{
		var result = Parse(yamlName);

		Assert.Null(result.Result);
		Assert.IsType<UnableToParseDiagnostic>(Assert.Single(result.Diagnostics));
	}

	[Fact]
	public void Tolerate_specification_extensions()
	{
		var document = ParseValid(DocumentYaml);

		Assert.Equal("path", document.Paths["/ping"].Extensions["x-path-extension"]?.GetValue<string>());
		Assert.Equal("operation", Operation(document, "/ping", "get").Extensions["x-operation-extension"]?.GetValue<string>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): root `tags` and Tag objects are not in the document model")]
	public void Model_root_tags()
	{
		var document = ParseValid(DocumentYaml);

		AssertModelsNode(document, DocumentYaml, "/tags/0");
		AssertModelsNode(document, DocumentYaml, "/tags/1");
		AssertModelsNode(document, DocumentYaml, "/tags/0/externalDocs");
		Assert.Equal("widgets", ModelProperty(AssertModelsNode(document, DocumentYaml, "/tags/0"), "Name"));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): root `externalDocs` is not in the document model")]
	public void Model_root_external_docs()
	{
		var document = ParseValid(DocumentYaml);

		var externalDocs = AssertModelsNode(document, DocumentYaml, "/externalDocs");
		Assert.Equal("https://example.com/docs", ModelProperty(externalDocs, "Url")?.ToString());
		Assert.Equal("Root docs", ModelProperty(externalDocs, "Description"));
	}

	#endregion

	#region Info Object

	[Fact]
	public void Read_info_fields()
	{
		var info = ParseValid(DocumentYaml).Info;

		Assert.Equal("Document fixture", info.Title);
		Assert.Equal("1.2.3", info.Version);
		Assert.Equal("Info description", info.Description);
		Assert.Equal("https://example.com/terms", info.TermsOfService?.OriginalString);
		Assert.Equal(("API team", "https://example.com/contact", "api@example.com"), (info.Contact?.Name, info.Contact?.Url?.OriginalString, info.Contact?.Email));
		Assert.Equal(("MIT", "https://opensource.org/licenses/MIT"), (info.License?.Name, info.License?.Url?.OriginalString));
	}

	[Fact]
	public void Report_missing_info_title()
	{
		var result = Parse("oas-parser-invalid-missing-info-title.yml");

		Assert.Contains(result.Diagnostics, d => d is MissingRequiredProperties m && m.Missing.Contains("title"));
	}

	[Fact]
	public void Report_missing_info_version()
	{
		var result = Parse("oas-parser-invalid-missing-info-version.yml");

		Assert.Contains(result.Diagnostics, d => d is MissingRequiredProperties m && m.Missing.Contains("version"));
	}

	[Fact]
	public void Report_wrong_scalar_types_without_unhandled_exceptions()
	{
		var result = Parse("oas-parser-invalid-scalar-type.yml");

		AssertProperDiagnostic(result);
	}

	#endregion

	#region Server Object

	[Fact]
	public void Read_servers()
	{
		var document = ParseValid(DocumentYaml);

		Assert.Collection(document.Servers,
			server =>
			{
				Assert.Equal(IdOf(DocumentYaml, "/servers/0"), server.Id.OriginalString);
				Assert.Equal("https://api.example.com/v1", server.Url.OriginalString);
				Assert.Equal("Production", server.Description);
				Assert.Empty(server.Variables);
			},
			server =>
			{
				Assert.Equal("Templated", server.Description);
				var variable = Assert.Single(server.Variables);
				Assert.Equal("env", variable.Key);
				Assert.Equal(["dev", "staging"], variable.Value.AllowedValues);
				Assert.Equal("dev", variable.Value.DefaultValue);
				Assert.Equal("Environment", variable.Value.Description);
			});
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Servers): a templated server url such as `https://{env}.example.com/v1` becomes `#`")]
	public void Keep_templated_server_urls()
	{
		var document = ParseValid(DocumentYaml);

		Assert.Equal("https://{env}.example.com/v1", document.Servers[1].Url.OriginalString);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Servers): path-level `servers` are not in the document model")]
	public void Model_path_level_servers()
	{
		var document = ParseValid(PathsYaml);

		var server = AssertModelsNode(document, PathsYaml, "/paths/~1all-methods/servers/0");
		Assert.Equal("https://path.example.com", (server as OpenApiServer)?.Url.OriginalString);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Servers): operation-level `servers` are not in the document model")]
	public void Model_operation_level_servers()
	{
		var document = ParseValid(PathsYaml);

		var server = AssertModelsNode(document, PathsYaml, "/paths/~1operation-details/get/servers/0");
		Assert.Equal("https://operation.example.com", (server as OpenApiServer)?.Url.OriginalString);
	}

	#endregion

	#region Paths / Path Item Object

	[Fact]
	public void Read_all_eight_operations()
	{
		var path = ParseValid(PathsYaml).Paths["/all-methods"];

		Assert.Equal(["get", "put", "post", "delete", "options", "head", "patch", "trace"], path.Operations.Keys);
		Assert.All(path.Operations, op => Assert.Equal(op.Key + "AllMethods", op.Value.OperationId));
	}

	[Fact]
	public void Read_path_item_summary_and_description()
	{
		var path = ParseValid(PathsYaml).Paths["/all-methods"];

		Assert.Equal("Path summary", path.Summary);
		Assert.Equal("Path description", path.Description);
	}

	[Fact]
	public void Resolve_path_item_refs()
	{
		var path = ParseValid(PathsYaml).Paths["/aliased"];

		Assert.Equal(IdOf(PathsYaml, "/x-path-items/Shared"), path.Id.OriginalString);
		Assert.Equal("Shared path item", path.Summary);
		Assert.Equal("getShared", Assert.Single(path.Operations).Value.OperationId);
	}

	#endregion

	#region Operation Object

	[Fact]
	public void Read_operation_documentation_fields()
	{
		var operation = Operation(ParseValid(PathsYaml), "/operation-details", "get");

		Assert.Equal(["widgets", "gadgets"], operation.Tags);
		Assert.Equal("Operation summary", operation.Summary);
		Assert.Equal("Operation description", operation.Description);
		Assert.True(operation.Deprecated);
		Assert.False(Operation(ParseValid(PathsYaml), "/operation-details", "post").Deprecated);
	}

	[Fact]
	public void Allow_operations_without_operation_id()
	{
		var operation = Operation(ParseValid(PathsYaml), "/no-operation-id", "get");

		Assert.Null(operation.OperationId);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): operation `externalDocs` is not in the document model")]
	public void Model_operation_external_docs()
	{
		var document = ParseValid(PathsYaml);

		var externalDocs = AssertModelsNode(document, PathsYaml, "/paths/~1operation-details/get/externalDocs");
		Assert.Equal("https://example.com/docs/operation", ModelProperty(externalDocs, "Url")?.ToString());
	}

	[Fact]
	public void Report_missing_responses()
	{
		var result = Parse("oas-parser-invalid-missing-responses.yml");

		Assert.Contains(result.Diagnostics, d => d is MissingRequiredProperties m && m.Missing.Contains("responses"));
	}

	#endregion

	#region Security Requirement Object

	[Fact]
	public void Read_document_security_requirements_as_or_of_ands()
	{
		var document = ParseValid(DocumentYaml);

		Assert.Collection(document.SecurityRequirements,
			requirement => Assert.Collection(requirement.SchemeRequirements,
				scheme => Assert.Equal(("apiKeyHeader", 0), (scheme.SchemeName, scheme.ScopeNames.Count)),
				scheme =>
				{
					Assert.Equal("oauth", scheme.SchemeName);
					Assert.Equal(["read", "write"], scheme.ScopeNames);
				}),
			requirement => Assert.Equal("bearer", Assert.Single(requirement.SchemeRequirements).SchemeName));
	}

	[Fact]
	public void Read_operation_security_requirements_as_or_of_ands()
	{
		var operation = Operation(ParseValid(PathsYaml), "/operation-details", "post");

		Assert.Collection(operation.SecurityRequirements,
			requirement => Assert.Equal(["basic", "apiKey"], requirement.SchemeRequirements.Select(s => s.SchemeName)),
			requirement =>
			{
				var scheme = Assert.Single(requirement.SchemeRequirements);
				Assert.Equal("oauth", scheme.SchemeName);
				Assert.Equal(["read", "write"], scheme.ScopeNames);
			});
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Security): operation `security: []` (explicitly none) is indistinguishable from omitted `security` (inherit)")]
	public void Distinguish_explicitly_empty_operation_security_from_inherited()
	{
		var document = ParseValid(PathsYaml);
		var explicitlyNone = Operation(document, "/operation-details", "get");
		var inherited = Operation(document, "/no-operation-id", "get");

		Assert.NotNull(explicitlyNone.SecurityRequirements);
		Assert.Empty(explicitlyNone.SecurityRequirements);
		// Omitted security inherits the document-level requirements; it must not look like `security: []`
		Assert.Null(inherited.SecurityRequirements);
	}

	#endregion

	#region Security Scheme Object

	[InlineData("apiKeyQuery")]
	[InlineData("apiKeyHeader")]
	[InlineData("apiKeyCookie")]
	[InlineData("basic")]
	[InlineData("bearer")]
	[InlineData("oauth")]
	[InlineData("oidc")]
	[Theory(Skip = "OpenAPI 3.0 gap (Security): `components.securitySchemes` are not in the document model")]
	public void Model_security_schemes(string schemeName)
	{
		var document = ParseValid(DocumentYaml);

		AssertModelsNode(document, DocumentYaml, $"/components/securitySchemes/{schemeName}");
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Security): `components.securitySchemes` are not in the document model")]
	public void Model_security_scheme_details()
	{
		var document = ParseValid(DocumentYaml);

		var apiKey = AssertModelsNode(document, DocumentYaml, "/components/securitySchemes/apiKeyHeader");
		Assert.Equal(("X-API-Key", "header"), (ModelProperty(apiKey, "Name"), ModelProperty(apiKey, "In")?.ToString()?.ToLowerInvariant()));
		var bearer = AssertModelsNode(document, DocumentYaml, "/components/securitySchemes/bearer");
		Assert.Equal(("bearer", "JWT"), (ModelProperty(bearer, "Scheme"), ModelProperty(bearer, "BearerFormat")));
		var oidc = AssertModelsNode(document, DocumentYaml, "/components/securitySchemes/oidc");
		Assert.Equal("https://example.com/.well-known/openid-configuration", ModelProperty(oidc, "OpenIdConnectUrl")?.ToString());
		foreach (var flow in new[] { "implicit", "password", "clientCredentials", "authorizationCode" })
			AssertModelsNode(document, DocumentYaml, $"/components/securitySchemes/oauth/flows/{flow}");
		var authorizationCode = AssertModelsNode(document, DocumentYaml, "/components/securitySchemes/oauth/flows/authorizationCode");
		Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable>(ModelProperty(authorizationCode, "Scopes")).Cast<object>().Count());
		var password = AssertModelsNode(document, DocumentYaml, "/components/securitySchemes/oauth/flows/password");
		Assert.Equal("https://example.com/oauth/refresh", ModelProperty(password, "RefreshUrl")?.ToString());
	}

	[Fact]
	public void Report_invalid_security_scheme_type()
	{
		var result = Parse("oas-parser-invalid-security-scheme-type.yml");

		AssertProperDiagnostic(result);
	}

	#endregion

	#region Parameter Object

	[Fact]
	public void Read_cookie_parameters()
	{
		var parameter = Parameter(ParseValid(ParametersYaml), "/items/{itemId}", "get", "session");

		Assert.Equal((ParameterLocation.Cookie, "form", false), (parameter.In, parameter.Style, parameter.Required));
	}

	[Fact]
	public void Read_parameter_deprecated_and_allow_empty_value()
	{
		var document = ParseValid(ParametersYaml);

		Assert.True(Parameter(document, "/items/{itemId}", "get", "legacy").Deprecated);
		Assert.False(Parameter(document, "/items/{itemId}", "get", "session").Deprecated);
		Assert.True(Parameter(document, "/items/{itemId}", "get", "emptyAllowed").AllowEmptyValue);
		Assert.False(Parameter(document, "/items/{itemId}", "get", "emptyNotAllowed").AllowEmptyValue);
	}

	[InlineData("/items/{itemId}", "session", "form", true)]
	[InlineData("/items/{itemId}", "spaced", "spaceDelimited", false)]
	[InlineData("/items/{itemId}", "piped", "pipeDelimited", false)]
	[InlineData("/matrix/{m}/label/{l}/simple/{s}", "m", "matrix", false)]
	[InlineData("/matrix/{m}/label/{l}/simple/{s}", "l", "label", false)]
	[InlineData("/matrix/{m}/label/{l}/simple/{s}", "s", "simple", false)]
	[InlineData("/matrix/{m}/label/{l}/simple/{s}", "X-Simple", "simple", false)]
	[Theory]
	public void Read_parameter_styles(string path, string name, string style, bool explode)
	{
		var parameter = Parameter(ParseValid(ParametersYaml), path, "get", name);

		Assert.Equal((style, explode), (parameter.Style, parameter.Explode));
	}

	[Fact]
	public void Read_explode_from_the_explode_field()
	{
		var document = ParseValid(ParametersYaml);

		Assert.False(Parameter(document, "/items/{itemId}", "get", "formNoExplode").Explode);
		// allowEmptyValue must not affect explode
		Assert.False(Parameter(document, "/items/{itemId}", "get", "emptyAllowed").Explode);
		Assert.True(Parameter(document, "/items/{itemId}", "get", "emptyNotAllowed").Explode);
		Assert.True(Parameter(document, "/items/{itemId}", "get", "deep").Explode);
	}

	[Fact]
	public void Resolve_parameter_refs_to_components()
	{
		var document = ParseValid(ParametersYaml);

		var parameter = Operation(document, "/items/{itemId}", "get").Parameters[0];
		Assert.Equal(IdOf(ParametersYaml, "/components/parameters/ItemId"), parameter.Id.OriginalString);
		Assert.Equal(("itemId", ParameterLocation.Path, true), (parameter.Name, parameter.In, parameter.Required));
	}

	[Fact]
	public void Unescape_tilde_in_json_pointers()
	{
		var parameter = Parameter(ParseValid(ParametersYaml), "/items/{itemId}", "get", "X-Tilde");

		Assert.Equal(IdOf(ParametersYaml, "/components/parameters/a~0b"), parameter.Id.OriginalString);
		Assert.Equal(ParameterLocation.Header, parameter.In);
	}

	[Fact]
	public void Read_parameter_allow_reserved()
	{
		var parameter = Parameter(ParseValid(ParametersYaml), "/items/{itemId}", "get", "reserved");

		Assert.Equal(true, ModelProperty(parameter, "AllowReserved"));
	}

	[Fact]
	public void Read_content_based_parameters()
	{
		var document = ParseValid(ParametersYaml);

		var mediaType = AssertModelsNode(document, ParametersYaml, "/paths/~1items~1%7BitemId%7D/get/parameters/9/content/application~1json");
		Assert.NotNull((mediaType as OpenApiMediaTypeObject)?.Schema);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): parameter `example`/`examples` and Example objects are not in the document model")]
	public void Read_parameter_examples()
	{
		var document = ParseValid(ParametersYaml);

		var withExample = Parameter(document, "/items/{itemId}", "get", "withExample");
		Assert.Equal("abc", Assert.IsAssignableFrom<JsonNode>(ModelProperty(withExample, "Example")).GetValue<string>());
		AssertModelsNode(document, ParametersYaml, "/paths/~1items~1{itemId}/get/parameters/11/examples/first");
		AssertModelsNode(document, ParametersYaml, "/components/examples/Second");
	}

	[Fact]
	public void Report_path_parameters_that_are_not_required()
	{
		var result = Parse("oas-parser-invalid-path-param-not-required.yml");

		AssertProperDiagnostic(result);
	}

	[Fact]
	public void Report_unknown_parameter_location()
	{
		var result = Parse("oas-parser-invalid-parameter-in.yml");

		AssertProperDiagnostic(result);
	}

	[Fact]
	public void Report_invalid_refs()
	{
		var result = Parse("oas-parser-invalid-ref.yml");

		Assert.Contains(result.Diagnostics, d => d is InvalidRefDiagnostic r && r.RefValue == "https://[invalid");
	}

	#endregion

	#region Request Body / Media Type / Encoding

	[Fact]
	public void Read_request_body_description()
	{
		var requestBody = Operation(ParseValid(ResponsesYaml), "/uploads", "post").RequestBody;

		Assert.NotNull(requestBody);
		Assert.Equal("Upload body", requestBody.Description);
		Assert.True(requestBody.Required);
		Assert.NotNull(requestBody.Content?["multipart/form-data"].Schema);
	}

	[Fact]
	public void Read_media_type_encoding()
	{
		var document = ParseValid(ResponsesYaml);

		var encoding = AssertModelsNode(document, ResponsesYaml, "/paths/~1uploads/post/requestBody/content/multipart~1form-data/encoding/meta");
		Assert.Equal("application/json", ModelProperty(encoding, "ContentType"));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): MediaType `example`/`examples` are not in the document model")]
	public void Read_media_type_examples()
	{
		var document = ParseValid(ResponsesYaml);

		var mediaType = Operation(document, "/responses", "get").Responses?.StatusCodeResponses[200].Content?["application/json"];
		Assert.NotNull(mediaType);
		Assert.Equal(1, Assert.IsAssignableFrom<JsonNode>(ModelProperty(mediaType, "Example"))["id"]?.GetValue<int>());
		AssertModelsNode(document, ResponsesYaml, "/components/examples/Item");
	}

	#endregion

	#region Responses / Response / Header / Link

	[Fact]
	public void Read_default_response()
	{
		var responses = Operation(ParseValid(ResponsesYaml), "/responses", "get").Responses;

		Assert.Equal("Unexpected error", responses?.Default?.Description);
		Assert.Equal("OK", responses?.StatusCodeResponses[200].Description);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Response ranges): `2XX`/`4XX` response keys are dropped")]
	public void Read_response_status_code_ranges()
	{
		var document = ParseValid(ResponsesYaml);

		Assert.Equal("Other success", (AssertModelsNode(document, ResponsesYaml, "/paths/~1responses/get/responses/2XX") as OpenApiResponse)?.Description);
		Assert.Equal("Client error", (AssertModelsNode(document, ResponsesYaml, "/paths/~1responses/get/responses/4XX") as OpenApiResponse)?.Description);
	}

	[Fact]
	public void Report_missing_response_description()
	{
		var result = Parse("oas-parser-invalid-missing-response-description.yml");

		Assert.Contains(result.Diagnostics, d => d is MissingRequiredProperties m && m.Missing.Contains("description"));
	}

	[Fact]
	public void Resolve_header_refs_to_components()
	{
		var response = Operation(ParseValid(ResponsesYaml), "/responses", "get").Responses?.StatusCodeResponses[200];

		Assert.NotNull(response);
		Assert.Collection(response.Headers,
			header =>
			{
				Assert.Equal(IdOf(ResponsesYaml, "/components/headers/RateLimit"), header.Id.OriginalString);
				Assert.Equal(("X-Rate-Limit", ParameterLocation.Header, "Requests remaining"), (header.Name, header.In, header.Description));
				Assert.NotNull(header.Schema);
			},
			header => Assert.Equal(("X-Inline", "Inline header"), (header.Name, header.Description)));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Links): response `links` and Link objects are not in the document model")]
	public void Read_response_links()
	{
		var document = ParseValid(ResponsesYaml);

		var link = AssertModelsNode(document, ResponsesYaml, "/paths/~1responses/get/responses/200/links/GetItem");
		Assert.Equal("getItem", ModelProperty(link, "OperationId"));
		AssertModelsNode(document, ResponsesYaml, "/components/links/Self");
	}

	#endregion

	#region Callback Object

	[Fact]
	public void Read_inline_and_referenced_callbacks()
	{
		var document = ParseValid(ResponsesYaml);

		Assert.Contains(IdOf(ResponsesYaml, "/paths/~1uploads/post/callbacks/inline/{$request.query.callbackUrl}~1inline"), document.Webhooks.Keys);
		Assert.Contains(IdOf(ResponsesYaml, "/components/callbacks/Shared/{$request.query.callbackUrl}~1shared"), document.Webhooks.Keys);
		Assert.All(document.Webhooks.Values, webhook => Assert.Equal(["post"], webhook.Operations.Keys));
	}

	#endregion

	#region Schema Object

	[Fact]
	public void Read_read_only_write_only_and_deprecated()
	{
		var flags = ComponentSchema(ParseValid(SchemasYaml), "flags");

		Assert.True(Assert.Single(Property(flags, "id").Annotations.OfType<ReadOnlyKeyword>()).IsReadOnly);
		Assert.True(Assert.Single(Property(flags, "secret").Annotations.OfType<WriteOnlyKeyword>()).IsWriteOnly);
		Assert.True(Assert.Single(Property(flags, "old").Annotations.OfType<DeprecatedKeyword>()).IsDeprecated);
	}

	[Fact]
	public void Read_numeric_validation_keywords()
	{
		var range = ComponentSchema(ParseValid(SchemasYaml), "range");

		Assert.Equal(0, Assert.Single(range.Annotations.OfType<MinimumKeyword>()).Value);
		Assert.Equal(10, Assert.Single(range.Annotations.OfType<MaximumKeyword>()).Value);
		Assert.Single(range.Annotations.OfType<MultipleOfKeyword>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): `exclusiveMaximum` is registered as `exclusiveMaimum`, so it is an unknown keyword")]
	public void Read_boolean_exclusive_minimum_and_maximum()
	{
		var range = ComponentSchema(ParseValid(SchemasYaml), "range");

		Assert.True(Assert.Single(range.Annotations.OfType<Draft04.ExclusiveMinimumKeyword>()).IsExclusive);
		Assert.True(Assert.Single(range.Annotations.OfType<Draft04.ExclusiveMaximumKeyword>()).IsExclusive);
	}

	[Fact]
	public void Read_array_validation_keywords()
	{
		var list = ComponentSchema(ParseValid(SchemasYaml), "list");

		Assert.Single(list.Annotations.OfType<MinItemsKeyword>());
		Assert.Single(list.Annotations.OfType<MaxItemsKeyword>());
		Assert.True(Assert.Single(list.Annotations.OfType<UniqueItemsKeyword>()).MustBeUnique);
		Assert.Single(list.Annotations.OfType<ItemsKeyword>());
	}

	[Fact]
	public void Read_object_validation_keywords()
	{
		var document = ParseValid(SchemasYaml);
		var bag = ComponentSchema(document, "bag");

		Assert.Single(bag.Annotations.OfType<MinPropertiesKeyword>());
		Assert.Single(bag.Annotations.OfType<MaxPropertiesKeyword>());
		Assert.True(Assert.Single(bag.Annotations.OfType<AdditionalPropertiesKeyword>()).Schema.BoolValue);
		Assert.False(Assert.Single(ComponentSchema(document, "closed").Annotations.OfType<AdditionalPropertiesKeyword>()).Schema.BoolValue);
		var map = Assert.Single(ComponentSchema(document, "map").Annotations.OfType<AdditionalPropertiesKeyword>()).Schema;
		Assert.Null(map.BoolValue);
		Assert.Equal([TypeAnnotation.PrimitiveType.Integer], Assert.Single(map.Annotations.OfType<TypeAnnotation>()).AllowedTypes);
	}

	[Fact]
	public void Read_any_of_and_not()
	{
		var document = ParseValid(SchemasYaml);

		Assert.Equal(2, Assert.Single(ComponentSchema(document, "either").Annotations.OfType<AnyOfKeyword>()).Schemas.Count);
		var not = Assert.Single(ComponentSchema(document, "notString").Annotations.OfType<NotKeyword>());
		Assert.Equal([TypeAnnotation.PrimitiveType.String], Assert.Single(not.Schema.Annotations.OfType<TypeAnnotation>()).AllowedTypes);
	}

	[Fact]
	public void Read_discriminator_mapping()
	{
		var pet = ComponentSchema(ParseValid(SchemasYaml), "pet");

		var discriminator = Assert.Single(pet.Annotations.OfType<DiscriminatorKeyword>());
		Assert.Equal("petType", discriminator.PropertyName);
		Assert.NotNull(discriminator.Mapping);
		Assert.Equal("#/components/schemas/Cat", discriminator.Mapping["cat"].OriginalString);
		Assert.Equal("#/components/schemas/Dog", discriminator.Mapping["dog"].OriginalString);
		Assert.Equal(
			[IdOf(SchemasYaml, "/components/schemas/Cat"), IdOf(SchemasYaml, "/components/schemas/Dog")],
			Assert.Single(pet.Annotations.OfType<OneOfKeyword>()).Schemas.Select(s => Dereference(s).Metadata.Id.OriginalString));
	}

	[Fact]
	public void Resolve_circular_refs()
	{
		var node = ComponentSchema(ParseValid(SchemasYaml), "node");

		Assert.Equal(IdOf(SchemasYaml, "/components/schemas/Node"), node.Metadata.Id.OriginalString);
		var items = Assert.Single(Property(node, "children").Annotations.OfType<ItemsKeyword>());
		Assert.Same(node, Dereference(items.Schema));
	}

	#endregion
}
