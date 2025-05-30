﻿using System.Collections.Generic;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApiCodegen.CSharp.WebhookClient.Templates;

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
	bool HasQueryStringEmbedded,
	OperationRequestBody[] RequestBodies,
	OperationResponses Responses,
	OperationSecurityRequirement[] SecurityRequirements
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

public record OperationRequestBody(string Name, bool IsForm, bool IsFile, bool HasQueryParam, string? RequestBodyType, IEnumerable<OperationParameter> AllParams);

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
