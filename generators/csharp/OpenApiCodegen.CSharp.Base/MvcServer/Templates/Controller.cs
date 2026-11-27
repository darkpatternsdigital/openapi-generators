using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;

public record ControllerTemplate(
	PartialHeader Header,
	string? SecurityAttribute,
	string SecurityRequirementsAttribute,

	string PackageName,
	string ClassName,

	bool HasDescriptionOrSummary,
	string? Summary,
	string? Description,

	ControllerOperation[] Operations
)
{
	public bool HasCookieParams => Operations.Any(o => o.RequestBodies.Any(b => b.AllParams.Any(p => p.IsCookieParam)));
}

public record ControllerOperation(
	string HttpMethod,
	string? Summary,
	string? Description,
	string Name,
	string Path,
	OperationRequestBody[] RequestBodies,
	OperationResponses Responses,
	OperationSecurityRequirement[] SecurityRequirements,
	Dictionary<string, string[]>[] RawSecurityRequirements,
	bool AllowAnonymous,
	string[] AuthorizePolicies,
	bool UseSecurityRequirementsAttribute,
	string SourceReference
)
{
	// ASP.NET Core MVC has no HttpTraceAttribute
	public string HttpMethodAttribute => HttpMethod == "Trace" ? "AcceptVerbs(\"TRACE\")" : $"Http{HttpMethod}";
	// For Minimal API's MapMethods
	public string HttpMethodName => HttpMethod.ToUpperInvariant();

	/// <summary>The requirements the generated filter enforces: scopes only for OAuth2 and OpenID Connect schemes</summary>
	public Dictionary<string, string[]>[] EnforcedSecurityRequirements =>
		SecurityRequirements.Select(r => r.Schemes.ToDictionary(s => s.SchemeName, s => s.ScopeNames)).ToArray();
}

public record OperationParameter(
	string? RawName,
	string ParamName,
	string? Description,
	string DataType,
	bool DataTypeNullable,
	bool DataTypeEnumerable,
	bool IsPathParam,
	bool IsQueryParam,
	bool IsHeaderParam,
	bool IsCookieParam,
	bool IsBodyParam,
	bool IsFormParam,
	bool Required,
	bool Optional,
	string? Pattern,
	int? MinLength,
	int? MaxLength,
	decimal? Minimum,
	decimal? Maximum,
	bool ExclusiveMinimum,
	bool ExclusiveMaximum,
	decimal? MultipleOf,
	bool UniqueItems,
	int? MinProperties,
	int? MaxProperties,
	bool IsTextBodyParam = false,
	bool IsStreamBodyParam = false
)
{
	/// <summary>Text and binary bodies are read from the request rather than bound as an action parameter</summary>
	public bool IsReadFromRequest => IsTextBodyParam || IsStreamBodyParam;
	// Base64 (`format: byte`) parameters outside the body, which Minimal API can't bind as byte[]
	public bool IsBase64 => !IsBodyParam && DataType is "byte[]" or "byte[]?";

	public bool HasMinLength => MinLength.HasValue;
	public bool HasMaxLength => MaxLength.HasValue;

	public bool HasMinimum => Minimum.HasValue;
	public bool HasMaximum => Maximum.HasValue;
	// Inclusive ranges with both bounds use [Range]; others use the Json.Extensions [Minimum] and [Maximum]
	public bool HasRange => HasMinimum && HasMaximum && !ExclusiveMinimum && !ExclusiveMaximum;
	public string? MinimumText => HasRange ? null : Minimum?.ToString(CultureInfo.InvariantCulture);
	public string? MaximumText => HasRange ? null : Maximum?.ToString(CultureInfo.InvariantCulture);

	public string? MultipleOfText => MultipleOf?.ToString(CultureInfo.InvariantCulture);
	public bool HasMinProperties => MinProperties.HasValue;
	public bool HasMaxProperties => MaxProperties.HasValue;
}

public record OperationResponses(
	OperationResponse? DefaultResponse,
	Dictionary<int, OperationResponse> StatusResponse,
	// Keyed by the first digit of the status code range, such as 4 for `4XX`
	Dictionary<int, OperationResponse> RangeResponse
);

public record OperationResponse(
	string Description,
	OperationResponseContentOption[] Content,
	OperationResponseHeader[] Headers
);

public record OperationResponseContentOption(
	string MediaType,
	string ResponseMethodName,
	string? DataType
);

public record OperationRequestBody(string Name, string? RequestBodyType, IEnumerable<OperationParameter> AllParams)
{
	/// <summary>The parameters MVC binds for the action</summary>
	public IEnumerable<OperationParameter> EntryParams => AllParams.Where(p => !p.IsReadFromRequest);
	/// <summary>A required body read from the request: a request without one is rejected, as MVC does for bound bodies</summary>
	public bool RequiresBodyFromRequest => AllParams.Any(p => p.IsReadFromRequest && p.Required);
}

public record OperationSecurityRequirement(
	OperationSecuritySchemeRequirement[] Schemes
);
public record OperationSecuritySchemeRequirement(
	string SchemeName,
	string[] ScopeNames
);

public record OperationResponseHeader(
	string? RawName,
	string ParamName,
	string? Description,
	string DataType,
	bool DataTypeNullable,
	bool Required,
	string? Pattern,
	int? MinLength,
	int? MaxLength,
	decimal? Minimum,
	decimal? Maximum
);
