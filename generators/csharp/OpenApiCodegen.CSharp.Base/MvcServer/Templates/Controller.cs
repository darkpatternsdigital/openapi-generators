using System.Collections.Generic;
using System.Linq;
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
	bool IsTextBodyParam = false,
	bool IsStreamBodyParam = false
)
{
	/// <summary>Text and binary bodies are read from the request rather than bound as an action parameter</summary>
	public bool IsReadFromRequest => IsTextBodyParam || IsStreamBodyParam;

	public bool HasMinLength => MinLength.HasValue;
	public bool HasMaxLength => MaxLength.HasValue;

	public bool HasMinimum => Minimum.HasValue;
	public bool HasMaximum => Maximum.HasValue;
}

public record OperationResponses(
	OperationResponse? DefaultResponse,
	Dictionary<int, OperationResponse> StatusResponse
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
