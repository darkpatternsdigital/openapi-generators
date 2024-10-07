using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.OpenApi3_0.TypeKeywordMismatch")]
	public static readonly DiagnosticDescriptor OpenApi30TypeKeywordMismatch =
		new DiagnosticDescriptor(id: "DPD_OPENAPI_3_0_001",
								title: "Type validation failed",
								messageFormat: DarkPatterns_Json_Specifications_OpenApi3_0_TypeKeywordMismatch,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.OpenApi3_0.InvalidNode")]
	public static readonly DiagnosticDescriptor OpenApi30InvalidNode =
		new DiagnosticDescriptor(id: "DPD_OPENAPI_3_0_002",
								title: "Unable to parse node in OpenAPI 3.0 document",
								messageFormat: DarkPatterns_Json_Specifications_OpenApi3_0_InvalidNode,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}
