using Microsoft.CodeAnalysis;
using static DarkPatterns.OpenApiCodegen.CommonDiagnostics;

namespace DarkPatterns.OpenApiCodegen;

public partial class TransformationDiagnostics
{
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.InvalidRetrievalUri")]
	public static readonly DiagnosticDescriptor InvalidRetrievalUriDiagnostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_002",
								title: "An invalid URI was provided to retrieve a document",
								messageFormat: DarkPatterns_OpenApi_Transformations_InvalidRetrievalUri,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.Specifications.UnableToParseDiagnostic")]
	public static readonly DiagnosticDescriptor UnableToParseDiagnostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_003",
								title: "Could not identify the document type",
								messageFormat: DarkPatterns_OpenApi_Transformations_Specifications_UnableToParseDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.InvalidDocumentBaseUri")]
	public static readonly DiagnosticDescriptor InvalidDocumentBaseUriDiagostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_004",
								title: "An invalid base URI was provided by a document",
								messageFormat: DarkPatterns_OpenApi_Transformations_InvalidDocumentBaseUri,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.InvalidFragmentDiagnostic")]
	public static readonly DiagnosticDescriptor InvalidFragmentDiagostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_005",
								title: "The fragment provided was not a valid JSON pointer",
								messageFormat: DarkPatterns_OpenApi_Transformations_InvalidFragmentDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.InvalidRefDiagnostic")]
	public static readonly DiagnosticDescriptor InvalidRefDiagnostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_006",
								title: "Invalid URI provided for ref",
								messageFormat: DarkPatterns_OpenApi_Transformations_InvalidRefDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.CouldNotFindTargetNodeDiagnostic")]
	public static readonly DiagnosticDescriptor CouldNotFindTargetNodeDiagnostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_007",
								title: "Target node did not exist in given document",
								messageFormat: DarkPatterns_OpenApi_Transformations_CouldNotFindTargetNodeDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.UnknownAnchorDiagnostic")]
	public static readonly DiagnosticDescriptor UnknownAnchorDiagnostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_008",
								title: "Unknown anchor provided in ref",
								messageFormat: DarkPatterns_OpenApi_Transformations_UnknownAnchorDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.ResolveDocumentDiagnostic")]
	public static readonly DiagnosticDescriptor ResolveDocumentDiagnostic =
		new DiagnosticDescriptor(id: "DPD_PARSE_009",
								title: "Could not retrieve the specified document",
								messageFormat: DarkPatterns_OpenApi_Transformations_ResolveDocumentDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.Diagnostics.UnhandledExceptionDiagnostic")]
	public static readonly DiagnosticDescriptor UnhandledExceptionDiagnostic =
		new DiagnosticDescriptor(id: "DPD_UNK",
								title: "Unhandled exception",
								messageFormat: DarkPatterns_OpenApi_Transformations_Diagnostics_UnhandledExceptionDiagnostic,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
	[TransformationDiagnostic("DarkPatterns.OpenApi.Transformations.UnableToGroupOperation")]
	public static readonly DiagnosticDescriptor UnableToGroupOperationDiagnostic =
		new DiagnosticDescriptor(id: "DPD_TRANSFORM_001",
								title: "Unable to group operations: {0}",
								messageFormat: DarkPatterns_OpenApi_Transformations_UnableToGroupOperation,
								category: "DarkPatterns.OpenApiCodegen",
								DiagnosticSeverity.Error,
								isEnabledByDefault: true);
}
