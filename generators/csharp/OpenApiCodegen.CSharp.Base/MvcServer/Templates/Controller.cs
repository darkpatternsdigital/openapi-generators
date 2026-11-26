using System.Collections.Generic;
using System.Globalization;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;

public record ControllerTemplate(
	PartialHeader Header,
	string? SecurityAttribute,

	string PackageName,
	string ClassName,

	bool HasDescriptionOrSummary,
	string? Summary,
	string? Description,

	ControllerOperation[] Operations
);

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
	string SourceReference
);

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
	int? MaxProperties
)
{
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

public record OperationRequestBody(string Name, string? RequestBodyType, IEnumerable<OperationParameter> AllParams);

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
