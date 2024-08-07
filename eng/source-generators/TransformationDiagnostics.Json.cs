using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.DocumentTypes.UnableToParseSchema")]
	public static readonly DiagnosticDescriptor UnableToParseSchemaDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_001",
								title: "Unable to parse schema; it must be either an object or a boolean value",
								messageFormat: DarkPatterns_OpenApi_Transformations_DocumentTypes_UnableToParseSchema,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.Specifications.FalseJsonSchemasFailDiagnostic")]
	public static readonly DiagnosticDescriptor FalseJsonSchemasFailDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_002",
								title: "Unable to match against a 'false' schema",
								messageFormat: DarkPatterns_OpenApi_Transformations_Specifications_FalseJsonSchemasFailDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.DocumentTypes.YamlLoadDiagnostic")]
	public static readonly DiagnosticDescriptor YamlLoadDiagnosticDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_003",
								title: "Unable to parse document",
								messageFormat: DarkPatterns_OpenApi_Transformations_DocumentTypes_YamlLoadDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.Specifications.Keywords.UnableToParseKeyword")]
	public static readonly DiagnosticDescriptor UnableToParseKeywordDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_004",
								title: "Could not parse the keyword",
								messageFormat: DarkPatterns_OpenApi_Transformations_Specifications_Keywords_UnableToParseKeyword,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}
