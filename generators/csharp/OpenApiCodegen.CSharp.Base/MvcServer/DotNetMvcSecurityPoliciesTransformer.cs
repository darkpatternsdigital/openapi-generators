using System.Linq;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer;

internal class DotNetMvcSecurityPoliciesTransformer(CSharpControllerTransformer schemaTransformer, OpenApiDocument document) : ISourceProvider
{
	public SourcesResult GetSources()
	{
		OpenApiTransformDiagnostic diagnostic = new();

		return new(
			schemaTransformer.TransformSecurityPoliciesHelper([
				.. from paths in document.Paths
				   from operation in paths.Value.Operations
				   from securityRequirement in operation.Value.SecurityRequirements
				   from schemeRequirement in securityRequirement.SchemeRequirements
				   select schemeRequirement.SchemeName,
				.. from securityRequirement in document.SecurityRequirements
				   from schemeRequirement in securityRequirement.SchemeRequirements
				   select schemeRequirement.SchemeName,
			], diagnostic).ToArray(),
			[.. diagnostic.Diagnostics]
		);
	}
}