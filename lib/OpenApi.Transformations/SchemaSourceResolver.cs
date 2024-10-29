using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApiCodegen;
using System;
using System.Diagnostics;
using System.Linq;

namespace DarkPatterns.OpenApi.Transformations;

public abstract class SchemaSourceProvider(ISchemaRegistry schemaRegistry) : ISourceProvider
{
	public SourcesResult GetSources()
	{
		var diagnostic = new OpenApiTransformDiagnostic();
		SourceEntry[] sources = [
				.. from entry in schemaRegistry.GetSchemas()
				   let e = SafeGetSourceEntry(entry, diagnostic)
				   where e != null
				   select e,
		];
		var groups = sources.GroupBy(s => s.Key).ToArray();
		var duplicateSources = groups.Where(g => g.Count() > 1).ToArray();
		if (duplicateSources.Length != 0)
			Debugger.Break();

		return SourcesResult.Combine([
			new SourcesResult(sources, [.. diagnostic.Diagnostics]),
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
