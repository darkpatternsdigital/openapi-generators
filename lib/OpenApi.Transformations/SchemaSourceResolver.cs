using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen;
using System.Collections.Generic;
using System.Linq;

namespace DarkPatterns.OpenApi.Transformations
{
	public abstract class SchemaSourceProvider(ISchemaRegistry schemaRegistry) : ISourceProvider
	{
		public IEnumerable<SourceEntry> GetSources(OpenApiTransformDiagnostic diagnostic) =>
			(from entry in schemaRegistry.GetSchemas()
			 let sourceEntry = GetSourceEntry(entry, diagnostic)
			 where sourceEntry != null
			 select sourceEntry)
			.Concat(GetAdditionalSources(diagnostic));

		protected virtual IEnumerable<SourceEntry> GetAdditionalSources(OpenApiTransformDiagnostic diagnostic) =>
			Enumerable.Empty<SourceEntry>();

		protected abstract SourceEntry? GetSourceEntry(JsonSchema entry, OpenApiTransformDiagnostic diagnostic);

	}
}
