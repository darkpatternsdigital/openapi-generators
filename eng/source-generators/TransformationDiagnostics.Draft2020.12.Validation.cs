using Microsoft.CodeAnalysis;
using static PrincipleStudios.OpenApiCodegen.CommonDiagnostics;

namespace PrincipleStudios.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Validation.JsonSchemaPatternMismatchDiagnostic")]
	public static readonly DiagnosticDescriptor JsonSchemaPatternMismatchDiagnostic =
		new DiagnosticDescriptor(id: "PS_JSON_2020_12_VAL_001",
								title: "Value did not match pattern",
								messageFormat: PrincipleStudios_OpenApi_Transformations_Specifications_Keywords_Draft2020_12Validation_JsonSchemaPatternMismatchDiagnostic,
								category: "PrincipleStudios.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Validation.MissingRequiredProperties")]
	public static readonly DiagnosticDescriptor JsonSchemaMissingRequiredPropertiesDiagnostic =
		new DiagnosticDescriptor(id: "PS_JSON_2020_12_VAL_002",
								title: "Required properties are missing from object",
								messageFormat: PrincipleStudios_OpenApi_Transformations_Specifications_Keywords_Draft2020_12Validation_MissingRequiredProperties,
								category: "PrincipleStudios.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Validation.UniqueItemsKeywordNotUnique")]
	public static readonly DiagnosticDescriptor JsonSchemaUniqueItemsKeywordNotUniqueDiagnostic =
		new DiagnosticDescriptor(id: "PS_JSON_2020_12_VAL_003",
								title: "Array items must be unique",
								messageFormat: PrincipleStudios_OpenApi_Transformations_Specifications_Keywords_Draft2020_12Validation_UniqueItemsKeywordNotUnique,
								category: "PrincipleStudios.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}