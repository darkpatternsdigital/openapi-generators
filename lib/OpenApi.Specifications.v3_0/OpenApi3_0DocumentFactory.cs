using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications.Keywords;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Draft04 = DarkPatterns.Json.Specifications.Keywords.Draft04;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using Keywords = DarkPatterns.Json.Specifications.Keywords;
using YamlDotNet.Core.Tokens;

namespace DarkPatterns.OpenApi.Specifications.v3_0;

#pragma warning disable CA1822 // Mark members as static

/// <summary>
/// See https://spec.openapis.org/oas/v3.0.3
/// </summary>
public class OpenApi3_0DocumentFactory : IOpenApiDocumentFactory
{
	private static string[] validMethods = new[]
	{
		"get",
		"put",
		"post",
		"delete",
		"options",
		"head",
		"patch",
		"trace",
	};


	public static readonly Uri jsonSchemaMeta = new Uri("https://spec.openapis.example.org/oas/3.0/meta/base");
	public static readonly Uri jsonSchemaDialect = new Uri("https://spec.openapis.example.org/oas/3.0/dialect/base");
	private readonly SchemaRegistry schemaRegistry;
	private readonly List<DiagnosticBase> diagnostics;

	public ICollection<DiagnosticBase> Diagnostics => diagnostics;
	public static IJsonSchemaVocabulary Vocabulary { get; }
	public static IJsonSchemaDialect OpenApiDialect { get; }
	public static DialectMatcher JsonSchemaDialectMatcher { get; }

	static OpenApi3_0DocumentFactory()
	{
		//Vocabularies.Core201909.Keywords
		Vocabulary = new JsonSchemaVocabulary(
			// https://github.com/OAI/OpenAPI-Specification/blob/d4fdc6cae9043dfc1abcad3c1a55282c49b3a7eb/schemas/v3.0/schema.yaml#L203
			jsonSchemaMeta,
			[
				("$ref", RefKeyword.Instance),

				// Most of `Vocabularies.Validation202012Id` works, but the exclusiveMinimum / exclusiveMaximum work differently
				("title", Keywords.Draft2020_12Metadata.TitleKeyword.Instance),
				("multipleOf", Keywords.Draft2020_12Validation.MultipleOfKeyword.Instance),
				("maximum", Keywords.Draft2020_12Validation.MaximumKeyword.Instance),
				("exclusiveMaimum", Draft04.ExclusiveMaximumKeyword.Instance),
				("minimum", Keywords.Draft2020_12Validation.MinimumKeyword.Instance),
				("exclusiveMinimum", Draft04.ExclusiveMinimumKeyword.Instance),
				("maxLength", Keywords.Draft2020_12Validation.MaxLengthKeyword.Instance),
				("minLength", Keywords.Draft2020_12Validation.MinLengthKeyword.Instance),
				("pattern", Keywords.Draft2020_12Validation.PatternKeyword.Instance),
				("maxItems", Keywords.Draft2020_12Validation.MaxItemsKeyword.Instance),
				("minItems", Keywords.Draft2020_12Validation.MinItemsKeyword.Instance),
				("uniqueItems", Keywords.Draft2020_12Validation.UniqueItemsKeyword.Instance),
				("maxProperties", Keywords.Draft2020_12Validation.MaxPropertiesKeyword.Instance),
				("minProperties", Keywords.Draft2020_12Validation.MinPropertiesKeyword.Instance),
				("required", Keywords.Draft2020_12Validation.RequiredKeyword.Instance),
				("enum", Keywords.Draft2020_12Validation.EnumKeyword.Instance),

				// OpenAPI 3.0 is not truly JsonSchema compliant, which is why
				// this has its own Uri with "example" in it "type" must also be
				// included. See
				// https://swagger.io/docs/specification/data-models/keywords/
				("type", TypeKeyword.Instance),

				("not", Keywords.Draft2020_12Applicator.NotKeyword.Instance),
				("allOf", Keywords.Draft2020_12Applicator.AllOfKeyword.Instance),
				("oneOf", Keywords.Draft2020_12Applicator.OneOfKeyword.Instance),
				("anyOf", Keywords.Draft2020_12Applicator.AnyOfKeyword.Instance),
				("items", ItemsKeyword.Instance),
				("properties", Keywords.Draft2020_12Applicator.PropertiesKeyword.Instance),
				("additionalProperties", Keywords.Draft2020_12Applicator.AdditionalPropertiesKeyword.Instance),
				("description", Keywords.Draft2020_12Metadata.DescriptionKeyword.Instance),
				("format", Draft04.FormatKeyword.Instance),
				("default", Keywords.Draft2020_12Metadata.DefaultKeyword.Instance),
				("nullable", NullableKeyword.Instance),
				("discriminator", DiscriminatorKeyword.Instance),
				("readOnly", Keywords.Draft2020_12Metadata.ReadOnlyKeyword.Instance),
				("writeOnly", Keywords.Draft2020_12Metadata.WriteOnlyKeyword.Instance),
				("example", ExampleKeyword.Instance),
				// externalDocs
				("deprecated", Keywords.Draft2020_12Metadata.DeprecatedKeyword.Instance),
				// xml
			]
		);
		OpenApiDialect =
			new JsonSchemaDialect(
				jsonSchemaDialect,
				null,
				[
					Vocabulary,
					// should be all of "https://spec.openapis.org/oas/3.0/schema/2021-09-28"
				],
				UnknownKeyword.Instance,
				GetInfoDefinition: OpenApi3_0DocumentInfo
			);
		JsonSchemaDialectMatcher = new DialectMatcher(
			n => n?["openapi"]?.GetValue<string>().StartsWith("3.0.") ?? false,
			OpenApiDialect
		);
	}

	private static DocumentInfo OpenApi3_0DocumentInfo(IDocumentReference doc)
	{
		var title = doc.RootNode?["info"]?["title"]?.GetValue<string>();
		var version = doc.RootNode?["info"]?["version"]?.GetValue<string>();
		var description = doc.RootNode?["info"]?["description"]?.GetValue<string>();
		var emailLine = doc.RootNode?["info"]?["contact"]?["email"]?.GetValue<string>() switch
		{
			string email => $"API Contact: {email}",
			_ => null
		};

		return new(
			version is { Length: > 0 } ? $"{title} v{version}" : title,
			string.Join(
				"\n\n",
				new[] { description, emailLine }
					.Where(v => v is { Length: > 0 })
			)
		);
	}

	public OpenApi3_0DocumentFactory(SchemaRegistry schemaRegistry, IEnumerable<DiagnosticBase> initialDiagnostics)
	{
		this.schemaRegistry = schemaRegistry;
		this.diagnostics = initialDiagnostics.ToList();
	}


	public OpenApiDocument ConstructDocument(IDocumentReference documentReference)
	{
		documentReference.Dialect = OpenApiDialect;
		return ConstructDocument(ResolvableNode.FromRoot(schemaRegistry.DocumentRegistry, documentReference));
	}

	private OpenApiDocument ConstructDocument(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new InvalidOperationException(Errors.InvalidOpenApiRootNode);
		return new OpenApiDocument(key.Id,
			OpenApiSpecVersion: new OpenApiSpecVersion("openapi", obj["openapi"]?.GetValue<string>() ?? "3.0.3"),
			Info: ConstructInfo(key.Navigate("info")),
			Dialect: OpenApiDialect,
			Paths: ConstructPaths(key.Navigate("paths")),
			SecurityRequirements: ReadArray(key.Navigate("security"), ConstructSecurityRequirement),
			Servers: ReadArray(key.Navigate("servers"), ConstructServer),
			Webhooks: ConstructWebhooksFromCallbacks(key)
		);
	}

	private Dictionary<string, OpenApiPath> ConstructWebhooksFromCallbacks(ResolvableNode key)
	{
		var result = new Dictionary<string, OpenApiPath>();
		var callbackNodes = (from path in GetChildrenOf(key.Navigate("paths"))
							 from operation in GetChildrenOf(path, validMethods.Contains).Select(AllowReference(n => n))
							 from callback in GetChildrenOf(operation.Navigate("callbacks")).Select(AllowReference(n => n))
							 from withExpression in GetChildrenOf(callback).Select(AllowReference(n => n))
							 group withExpression by withExpression.Id.OriginalString into matchingIds
							 select matchingIds.First()).ToArray();

		return callbackNodes.ToDictionary(n => n.Id.OriginalString, n => ConstructPath(n));
	}

	private IEnumerable<ResolvableNode> GetChildrenOf(ResolvableNode target, Func<string, bool>? propertyNameFilter = null)
	{
		switch (target.Node)
		{
			case JsonObject obj:
				foreach (var n in obj)
					if (propertyNameFilter?.Invoke(n.Key) ?? true)
						yield return target.Navigate(n.Key);
				yield break;
			case JsonArray arr:
				foreach (var index in arr.Select((n, i) => i))
					yield return target.Navigate(index);
				yield break;
			case null: yield break;
			case JsonValue:
				yield break;
		}
	}

	private OpenApiServer ConstructServer(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructServer, MissingRequiredFieldDefaults.ConstructPlaceholderServerRequirement)(key);
	private OpenApiServer InternalConstructServer(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiServer)));
		return new OpenApiServer(key.Id,
			Url: obj["url"]?.GetValue<string>() is string url
				&& Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var result)
					? result
					: new Uri("#", UriKind.Relative),
			Description: obj["description"]?.GetValue<string>(),
			Variables: ReadDictionary(key.Navigate("variables"), v => true, (prop, key) => (prop, ConstructServerVariable(key)))
		);
	}

	private OpenApiServerVariable ConstructServerVariable(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructServerVariable, MissingRequiredFieldDefaults.ConstructPlaceholderServerVariable)(key);
	private OpenApiServerVariable InternalConstructServerVariable(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiServerVariable)));
		return new OpenApiServerVariable(
			key.Id,
			AllowedValues: ReadArray(key.Navigate("enum"), (n) => n.Node?.GetValue<string>() ?? MissingRequiredFieldDefaults.ServerVariableAllowedValue),
			DefaultValue: obj["default"]?.GetValue<string>() ?? MissingRequiredFieldDefaults.ServerVariableAllowedValue,
			Description: obj["description"]?.GetValue<string>()
		);
	}

	private OpenApiContact? ConstructContact(ResolvableNode key) =>
		CatchDiagnostic(AllowNull(InternalConstructContact), (_) => null)(key);
	private OpenApiContact InternalConstructContact(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiContact)));
		return new OpenApiContact(key.Id,
			Name: obj["name"]?.GetValue<string>(),
			Url: obj["url"]?.GetValue<string>() is string url
				&& Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var result)
					? result
					: null,
			Email: obj["email"]?.GetValue<string>()
		);
	}

	private OpenApiInfo ConstructInfo(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructInfo, MissingRequiredFieldDefaults.ConstructPlaceholderInfo)(key);
	private OpenApiInfo InternalConstructInfo(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiInfo)));
		return new OpenApiInfo(key.Id,
			Title: obj["title"]?.GetValue<string>() ?? MissingRequiredFieldDefaults.InfoTitle,
			Summary: null,
			Description: obj["description"]?.GetValue<string>(),
			TermsOfService: obj["termsOfService"]?.GetValue<string>() is string tos
				&& Uri.TryCreate(tos, UriKind.RelativeOrAbsolute, out var result)
					? result
					: null,
			Contact: obj["contact"] is JsonObject
				? ConstructContact(key.Navigate("contact"))
				: null,
			License: ConstructLicense(key.Navigate("license")),
			Version: obj["version"]?.GetValue<string>() ?? MissingRequiredFieldDefaults.InfoVersion
		);
	}

	private OpenApiLicense? ConstructLicense(ResolvableNode key) =>
		CatchDiagnostic(AllowNull(InternalConstructLicense), (_) => null)(key);
	private OpenApiLicense InternalConstructLicense(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiLicense)));
		return new OpenApiLicense(key.Id,
			Name: obj["name"]?.GetValue<string>() ?? MissingRequiredFieldDefaults.LicenseName,
			Url: obj["url"]?.GetValue<string>() is string url
					&& Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var result)
						? result
						: null,
			Identifier: null
		);
	}

	private OpenApiMediaTypeObject ConstructMediaTypeObject(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructMediaTypeObject, MissingRequiredFieldDefaults.ConstructPlaceholderMediaTypeObject)(key);
	// https://spec.openapis.org/oas/v3.0.0#media-type-object
	private OpenApiMediaTypeObject InternalConstructMediaTypeObject(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiMediaTypeObject)));
		var schema = ConstructSchema(key.Navigate("schema"));
		return new OpenApiMediaTypeObject(key.Id, Schema: schema);
	}

	private OpenApiOperation ConstructOperation(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructOperation, MissingRequiredFieldDefaults.ConstructPlaceholderOperation)(key);
	// https://spec.openapis.org/oas/v3.0.0#operationObject
	private OpenApiOperation InternalConstructOperation(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiOperation)));
		return new OpenApiOperation(key.Id,
			Tags: ReadArray(key.Navigate("tags"), k => k.Node?.GetValue<string>() ?? MissingRequiredFieldDefaults.OperationTag),
			Summary: obj["summary"]?.GetValue<string>(),
			Description: obj["description"]?.GetValue<string>(),
			OperationId: obj["operationId"]?.GetValue<string>(),
			Parameters: ReadArray(key.Navigate("parameters"), ConstructParameter),
			SecurityRequirements: ReadArray(key.Navigate("security"), ConstructSecurityRequirement),
			RequestBody: ConstructRequestBody(key.Navigate("requestBody")),
			Responses: ConstructResponses(key.Navigate("responses")),
			Deprecated: obj["deprecated"]?.GetValue<bool>() ?? false,
			Extensions: GetExtensions(obj)
		);
	}

	private Dictionary<string, JsonNode?> GetExtensions(JsonObject obj)
	{
		return obj.Where(kvp => kvp.Key.StartsWith("x-")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	}

	private OpenApiParameter ConstructParameter(ResolvableNode key) =>
		CatchDiagnostic(AllowReference(InternalConstructParameter), MissingRequiredFieldDefaults.ConstructPlaceholderParameter)(key);
	// https://spec.openapis.org/oas/v3.0.0#parameterObject
	private OpenApiParameter InternalConstructParameter(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiParameter)));

		var location = ToParameterLocation(obj["in"]);
		return LoadParameter(key, obj, location);
	}

	private OpenApiParameter LoadParameter(ResolvableNode key, JsonObject obj, ParameterLocation location)
	{
		var style = obj["style"]?.GetValue<string>() ?? (location switch
		{
			ParameterLocation.Header => "simple",
			ParameterLocation.Query => "form",
			ParameterLocation.Cookie => "form",
			ParameterLocation.Path => "simple",
			_ => "form",
		});
		return new OpenApiParameter(
					Id: key.Id,
					Name: obj["name"]?.GetValue<string>() ?? MissingRequiredFieldDefaults.ParameterName,
					In: location,
					Description: obj["description"]?.GetValue<string>(),
					Required: location == ParameterLocation.Path ? true : obj["required"]?.GetValue<bool>() ?? false,
					Deprecated: obj["deprecated"]?.GetValue<bool>() ?? false,
					AllowEmptyValue: obj["allowEmptyValue"]?.GetValue<bool>() ?? false,
					Style: style,
					Explode: obj["allowEmptyValue"]?.GetValue<bool>() ?? (style == "form"),
					Schema: ConstructSchema(key.Navigate("schema"))
				);
	}

	private OpenApiParameter ConstructHeaderParameter(ResolvableNode key, string name) =>
		CatchDiagnostic(AllowReference(InternalConstructHeaderParameter), MissingRequiredFieldDefaults.ConstructPlaceholderParameter)(key) with { Name = name };
	private OpenApiParameter InternalConstructHeaderParameter(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiParameter)));

		return LoadParameter(key, obj, ParameterLocation.Header);
	}

	private OpenApiSecurityRequirement ConstructSecurityRequirement(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructSecurityRequirement, MissingRequiredFieldDefaults.ConstructPlaceholderSecurityRequirement)(key);
	private OpenApiSecurityRequirement InternalConstructSecurityRequirement(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiSecurityRequirement)));

		var schemeRequirements = (from kvp in obj
								  let arr = kvp.Value as JsonArray ?? throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiSecurityRequirement)))
								  select new OpenApiSecuritySchemeRequirement(
									 SchemeName: kvp.Key,
									 ScopeNames: arr.Deserialize<string[]>() ?? throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiSecurityRequirement)))
								  )).ToArray();

		return new OpenApiSecurityRequirement(key.Id, schemeRequirements);
	}

	private JsonSchema? ConstructSchema(ResolvableNode key) =>
		CatchDiagnostic(AllowReference(AllowNull(InternalConstructSchema)), (_) => null)(key);
	private JsonSchema InternalConstructSchema(ResolvableNode key)
	{
		var resolved = JsonSchemaParser.Deserialize(key, new JsonSchemaParserOptions(schemaRegistry, OpenApiDialect));
		return resolved.Fold(
			schema => schema,
			diagnostics => throw new MultipleDiagnosticException(diagnostics)
		);
	}

	private ParameterLocation ToParameterLocation(JsonNode? jsonNode)
	{
		switch (jsonNode?.GetValue<string>())
		{
			case "path": return ParameterLocation.Path;
			case "query": return ParameterLocation.Query;
			case "header": return ParameterLocation.Header;
			case "cookie": return ParameterLocation.Cookie;
			case null:
			default:
				throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiParameter)));
		}
	}

	private IReadOnlyDictionary<string, OpenApiPath> ConstructPaths(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructPaths, (_) => new Dictionary<string, OpenApiPath>())(key);
	private IReadOnlyDictionary<string, OpenApiPath> InternalConstructPaths(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiPath)));

		return ReadDictionary(key, filter: _ => true, toKeyValuePair: (prop, key) => (prop, ConstructPath(key)));
	}

	private OpenApiPath ConstructPath(ResolvableNode key) =>
		CatchDiagnostic(AllowReference(InternalConstructPath), MissingRequiredFieldDefaults.ConstructPlaceholderPath)(key);
	private OpenApiPath InternalConstructPath(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiPath)));
		return new OpenApiPath(key.Id,
			Summary: obj["summary"]?.GetValue<string>(),
			Description: obj["description"]?.GetValue<string>(),
			Operations: ReadDictionary(key, validMethods.Contains, toKeyValuePair: (method, key) => (Key: method, Value: ConstructOperation(key))),
			Extensions: GetExtensions(obj)
		);
	}

	private OpenApiRequestBody? ConstructRequestBody(ResolvableNode key) =>
		CatchDiagnostic(AllowReference(AllowNull(InternalConstructRequestBody)), (_) => null)(key);
	// https://spec.openapis.org/oas/v3.0.0#request-body-object
	private OpenApiRequestBody InternalConstructRequestBody(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiRequestBody)));
		return new OpenApiRequestBody(
			Id: key.Id,
			Description: obj["description"]?.GetValue<string>(),
			Content: ConstructMediaContentDictionary(key.Navigate("content")),
			Required: obj["required"]?.GetValue<bool>() ?? false
		);
	}

	private IReadOnlyDictionary<string, OpenApiMediaTypeObject>? ConstructMediaContentDictionary(ResolvableNode key) =>
		CatchDiagnostic(AllowNull(InternalConstructMediaContentDictionary), (_) => null)(key);
	private IReadOnlyDictionary<string, OpenApiMediaTypeObject> InternalConstructMediaContentDictionary(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder("OpenApiMediaTypeContent"));
		return ReadDictionary(key,
			filter: media => media.Contains('/'),
			toKeyValuePair: (media, key) => (Key: media, Value: ConstructMediaTypeObject(key))
		);
	}

	// https://spec.openapis.org/oas/v3.0.0#response-object
	private OpenApiResponse ConstructResponse(ResolvableNode key) =>
		CatchDiagnostic(AllowReference(InternalConstructResponse), MissingRequiredFieldDefaults.ConstructPlaceholderResponse)(key);
	private OpenApiResponse InternalConstructResponse(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiResponse)));
		return new OpenApiResponse(key.Id,
			Description: obj["description"]?.GetValue<string>() ?? MissingRequiredFieldDefaults.ResponseDescription,
			Headers: ConstructHeaders(key.Navigate("headers")),
			Content: ConstructMediaContentDictionary(key.Navigate("content"))
		);
	}

	private OpenApiParameter[] ConstructHeaders(ResolvableNode key) =>
		CatchDiagnostic(InternalConstructHeaders, _ => Array.Empty<OpenApiParameter>())(key);
	private OpenApiParameter[] InternalConstructHeaders(ResolvableNode key)
	{
		if (key.Node == null) return Array.Empty<OpenApiParameter>();
		return ReadDictionary(
				key,
				_ => true,
				(name, key) => (name, ConstructHeaderParameter(key, name))
			).Values.ToArray();
	}

	// https://spec.openapis.org/oas/v3.0.0#responses-object
	private OpenApiResponses? ConstructResponses(ResolvableNode key) =>
		CatchDiagnostic(AllowNull(InternalConstructResponses), (_) => null)(key);
	private OpenApiResponses InternalConstructResponses(ResolvableNode key)
	{
		if (key.Node is not JsonObject obj) throw new DiagnosticException(InvalidNode.Builder(nameof(OpenApiResponses)));
		return new OpenApiResponses(Id: key.Id,
			Default: AllowNull(ConstructResponse)(key.Navigate("default")),
			StatusCodeResponses: ReadDictionary(key,
				filter: statusCode => statusCode.Length == 3 && int.TryParse(statusCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
				toKeyValuePair: (statusCode, key) => (Key: int.Parse(statusCode, NumberStyles.Integer, CultureInfo.InvariantCulture), Value: ConstructResponse(key))
			)
		);
	}

	private Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(ResolvableNode key, Func<string, bool> filter, Func<string, ResolvableNode, (TKey Key, TValue Value)> toKeyValuePair, Action? ifNotObject = null)
	{
		if (key.Node is not JsonObject obj)
		{
			ifNotObject?.Invoke();
			return new Dictionary<TKey, TValue>();
		}
		return obj
			.Select(kvp => kvp.Key)
			.Where(filter)
			.Select(prop => toKeyValuePair(prop, key.Navigate(prop)))
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	}

	private T[] ReadArray<T>(ResolvableNode key, Func<ResolvableNode, T> toItem, Action? ifNotArray = null)
	{
		if (key.Node is not JsonArray array)
		{
			ifNotArray?.Invoke();
			return Array.Empty<T>();
		}
		return array.Select((node, index) => key.Navigate(index.ToString())).Select(toItem).ToArray();
	}

	private Func<ResolvableNode, T?> AllowNull<T>(Func<ResolvableNode, T> toItem)
	{
		return (key) =>
		{
			if (key.Node == null) return default;
			return toItem(key);
		};
	}

	private Func<ResolvableNode, T> AllowReference<T>(Func<ResolvableNode, T> toItem)
	{
		return (key) =>
		{
			if (key.Node is not JsonObject obj) return toItem(key);
			if (obj["$ref"]?.GetValue<string>() is not string refName) return toItem(key);

			if (!Uri.TryCreate(refName, UriKind.RelativeOrAbsolute, out var uri))
				throw new DiagnosticException(InvalidRefDiagnostic.Builder(refName));

			var newKey = schemaRegistry.DocumentRegistry.ResolveMetadataNode(new Uri(key.Metadata.Id, uri), key.Metadata);
			return toItem(newKey);
		};
	}

	private Func<ResolvableNode, T> CatchDiagnostic<T>(Func<ResolvableNode, T> toItem, Func<Uri, T> constructDefault)
	{
		return (key) =>
		{
			try
			{
				return toItem(key);
			}
#pragma warning disable CA1031 // Catching a general exception type here to turn it into a diagnostic for reporting
			catch (Exception ex)
			{
				diagnostics.AddRange(ex.ToDiagnostics(schemaRegistry.DocumentRegistry, key.Metadata));
			}
#pragma warning restore CA1031 // Do not catch general exception types
			return constructDefault(key.Id);
		};
	}
}

public record InvalidNode(string NodeType, Location Location) : DiagnosticBase(Location)
{
	public static DiagnosticException.ToDiagnostic Builder(string nodeType) => (Location) => new InvalidNode(nodeType, Location);
}
