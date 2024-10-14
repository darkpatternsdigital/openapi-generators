using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApiCodegen;
using System.Linq;

namespace DarkPatterns.OpenApi.Transformations
{
	public abstract class SchemaSourceProvider(ISchemaRegistry schemaRegistry) : ISourceProvider
	{
		public SourcesResult GetSources()
		{
			var diagnostic = new OpenApiTransformDiagnostic();
			return SourcesResult.Combine([
				new SourcesResult([
					.. from entry in schemaRegistry.GetSchemas()
					   let e = GetSourceEntry(entry, diagnostic)
					   where e != null
					   select e,
				], [.. diagnostic.Diagnostics]),
				GetAdditionalSources()
			]);
		}

		protected virtual SourcesResult GetAdditionalSources() =>
			SourcesResult.Empty;

		protected abstract SourceEntry? GetSourceEntry(JsonSchema entry, OpenApiTransformDiagnostic diagnostic);

	}
}
