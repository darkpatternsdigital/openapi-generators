using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.TypeKeywordMismatch")]
	public static readonly DiagnosticDescriptor JsonSchemaTypeKeywordMismatch =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_004",
								title: "Type validation failed",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_TypeKeywordMismatch,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
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
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.EnumKeywordMismatch")]
	public static readonly DiagnosticDescriptor JsonSchemaEnumKeywordMismatchDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_005",
								title: "Value is not one of the allowed values",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_EnumKeywordMismatch,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.MinimumKeywordNotMet")]
	public static readonly DiagnosticDescriptor JsonSchemaMinimumKeywordNotMetDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_006",
								title: "Value is below the minimum",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_MinimumKeywordNotMet,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.MinItemsKeywordNotMet")]
	public static readonly DiagnosticDescriptor JsonSchemaMinItemsKeywordNotMetDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_007",
								title: "Array has too few items",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_MinItemsKeywordNotMet,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.MinPropertiesKeywordNotMet")]
	public static readonly DiagnosticDescriptor JsonSchemaMinPropertiesKeywordNotMetDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_008",
								title: "Object has too few properties",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_MinPropertiesKeywordNotMet,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.MaxPropertiesKeywordExceeded")]
	public static readonly DiagnosticDescriptor JsonSchemaMaxPropertiesKeywordExceededDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_VAL_009",
								title: "Object has too many properties",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Validation_MaxPropertiesKeywordExceeded,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}
