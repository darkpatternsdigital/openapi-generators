using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.OpenApi.CSharp.UnableToCreateInlineSchemaDiagnostic")]
	public static readonly DiagnosticDescriptor OpenApiCSharpUnableToCreateInlineSchemaDiagnostic =
		new DiagnosticDescriptor(id: "DPD_OPENAPI_CS_001",
								title: "Could not convert schema into a C# type",
								messageFormat: DarkPatterns_OpenApi_CSharp_UnableToCreateInlineSchemaDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);

	[TransformationDiagnostic("DarkPatterns.OpenApiCodegen.CSharp.ConflictingOutputMetadataDiagnostic")]
	public static readonly DiagnosticDescriptor OpenApiCSharpConflictingOutputMetadataDiagnostic =
		new DiagnosticDescriptor(id: "DPD_OPENAPI_CS_002",
								title: "A file is listed more than once as the same output with different metadata",
								messageFormat: DarkPatterns_OpenApiCodegen_CSharp_ConflictingOutputMetadataDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}
