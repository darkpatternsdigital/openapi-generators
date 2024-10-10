using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.JsonSchemaPatternMismatchDiagnostic")]
	public static readonly DiagnosticDescriptor JsonSchemaPatternMismatchDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_001",
								title: "Value did not match pattern",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_JsonSchemaPatternMismatchDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.MissingRequiredProperties")]
	public static readonly DiagnosticDescriptor JsonSchemaMissingRequiredPropertiesDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_002",
								title: "Required properties are missing from object",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_MissingRequiredProperties,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.UniqueItemsKeywordNotUnique")]
	public static readonly DiagnosticDescriptor JsonSchemaUniqueItemsKeywordNotUniqueDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_003",
								title: "Array items must be unique",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_UniqueItemsKeywordNotUnique,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}
