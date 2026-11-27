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

	[TransformationDiagnostic("DarkPatterns.OpenApiCodegen.CSharp.JsonUnionsRequireSystemTextJson11Diagnostic")]
	public static readonly DiagnosticDescriptor OpenApiCSharpJsonUnionsRequireSystemTextJson11Diagnostic =
		new DiagnosticDescriptor(id: "DPDAPI004",
								title: "jsonUnions requires System.Text.Json 11",
								messageFormat: DarkPatterns_OpenApiCodegen_CSharp_JsonUnionsRequireSystemTextJson11Diagnostic,
								category: "DarkPatterns.OpenApiCodegen.CSharp",
								DiagnosticSeverity.Warning,
								isEnabledByDefault: true);

	[TransformationDiagnostic("DarkPatterns.OpenApiCodegen.CSharp.JsonUnionsRequireUnionTypesDiagnostic")]
	public static readonly DiagnosticDescriptor OpenApiCSharpJsonUnionsRequireUnionTypesDiagnostic =
		new DiagnosticDescriptor(id: "DPDAPI005",
								title: "jsonUnions requires UnionAttribute and IUnion",
								messageFormat: DarkPatterns_OpenApiCodegen_CSharp_JsonUnionsRequireUnionTypesDiagnostic,
								category: "DarkPatterns.OpenApiCodegen.CSharp",
								DiagnosticSeverity.Warning,
								isEnabledByDefault: true);
}
