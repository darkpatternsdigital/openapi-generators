using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.Json.Documents.UnableToParseSchema")]
	public static readonly DiagnosticDescriptor UnableToParseSchemaDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_001",
								title: "Unable to parse schema; it must be either an object or a boolean value",
								messageFormat: DarkPatterns_Json_Documents_UnableToParseSchema,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.FalseJsonSchemasFailDiagnostic")]
	public static readonly DiagnosticDescriptor FalseJsonSchemasFailDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_002",
								title: "Unable to match against a 'false' schema",
								messageFormat: DarkPatterns_Json_Specifications_FalseJsonSchemasFailDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Loaders.YamlLoadDiagnostic")]
	public static readonly DiagnosticDescriptor YamlLoadDiagnosticDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_003",
								title: "Unable to parse document",
								messageFormat: DarkPatterns_Json_Loaders_YamlLoadDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.UnableToParseKeyword")]
	public static readonly DiagnosticDescriptor UnableToParseKeywordDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_004",
								title: "Could not parse the keyword",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_UnableToParseKeyword,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}
