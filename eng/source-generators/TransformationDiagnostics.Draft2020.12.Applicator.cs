using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator.MustNotMatch")]
	public static readonly DiagnosticDescriptor MustNotMatchDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_APP_001",
								title: "Value matched 'not'-constrained schema",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Applicator_MustNotMatch,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator.OnlyOneMustMatch")]
	public static readonly DiagnosticDescriptor OnlyOneMustMatchDiagnostic =
		new DiagnosticDescriptor(id: "DPD_JSON_2020_12_APP_002",
								title: "Value matched multiple 'oneOf'-constrained schemas",
								messageFormat: DarkPatterns_Json_Specifications_Keywords_Draft2020_12Applicator_OnlyOneMustMatch,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);

}
