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
}
