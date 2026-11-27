using System.Collections.Generic;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApiCodegen.CSharp.Client.Templates;

public record FullTemplate(
	PartialHeader Header,

	string PackageName,
	string ClassName,

	Operation[] Operations
);

public record Operation(
	string HttpMethod,
	string? Summary,
	string? Description,
	string Name,
	string Path,
	bool HasQueryStringEmbedded,
	OperationRequestBody[] RequestBodies,
	OperationResponses Responses,
	OperationSecurityRequirement[] SecurityRequirements,
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
	string? Pattern,
	int? MinLength,
	int? MaxLength,
	decimal? Minimum,
	decimal? Maximum
)
{
	public bool IsFile => DataType is "global::System.IO.Stream" or "global::System.IO.Stream?";
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
	string? DataType,
	bool IsJson,
	bool IsText,
	bool IsRaw,
	string MediaTypeCondition
);

public record OperationRequestBody(string Name, bool IsForm, bool IsText, bool IsFile, bool HasQueryParam, bool HasCookieParam, string? RequestBodyType, IEnumerable<OperationParameter> AllParams);

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
