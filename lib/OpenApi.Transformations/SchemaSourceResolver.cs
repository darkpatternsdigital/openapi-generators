using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Linq;

namespace DarkPatterns.OpenApi.Transformations;

public abstract class SchemaSourceProvider(ISchemaRegistry schemaRegistry) : ISourceProvider
{
	public SourcesResult GetSources()
	{
		var diagnostic = new OpenApiTransformDiagnostic();
		return SourcesResult.Combine([
			new SourcesResult([
				.. from entry in schemaRegistry.GetSchemas()
				   let e = SafeGetSourceEntry(entry, diagnostic)
				   where e != null
				   select e,
			], [.. diagnostic.Diagnostics]),
			GetAdditionalSources()
		]);
	}

	protected virtual SourcesResult GetAdditionalSources() =>
		SourcesResult.Empty;

	protected abstract SourceEntry? GetSourceEntry(JsonSchema entry, OpenApiTransformDiagnostic diagnostic);

	private SourceEntry? SafeGetSourceEntry(JsonSchema entry, OpenApiTransformDiagnostic diagnostic)
	{
		try
		{
			return GetSourceEntry(entry, diagnostic);
		}
		catch (Exception ex)
		{
			diagnostic.Diagnostics.AddRange(ex.ToDiagnostics(schemaRegistry.DocumentRegistry, entry.Metadata));
			return null;
		}
	}

}
