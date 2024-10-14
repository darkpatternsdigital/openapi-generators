using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Abstractions;
using System.Linq;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;
using System;

namespace DarkPatterns.OpenApiCodegen.Handlebars;

public record TransformSettings(SchemaRegistry SchemaRegistry, string CodeGeneratorVersionInfo)
{
	public static CompositeOpenApiSourceProvider BuildComposite(
		DocumentRegistry documentRegistry,
		string versionInfo,
		System.Func<TransformSettings, ISourceProvider>[] factories)
	{
		return BuildComposite(new SchemaRegistry(documentRegistry), versionInfo, factories);
	}

	public static CompositeOpenApiSourceProvider BuildComposite(
		SchemaRegistry schemaRegistry,
		string versionInfo,
		System.Func<TransformSettings, ISourceProvider>[] factories)
	{
		var settings = new TransformSettings(schemaRegistry, versionInfo);

		return new CompositeOpenApiSourceProvider(
			factories.Select(factory => factory(settings)).ToArray()
		);
	}

	public PartialHeader Header(Uri id)
	{
		if (!SchemaRegistry.DocumentRegistry.TryGetDocument(id, out var doc))
		{
			return new PartialHeader(null, null, CodeGeneratorVersionInfo);
		}
		var info = doc.Dialect.GetInfo(doc);
		return Header(info.Title, info.Description);
	}

	public PartialHeader Header(string? title, string? description = null)
	{
		return new PartialHeader(
			AppTitle: title,
			AppDescription: description,
			CodeGeneratorVersionInfo: CodeGeneratorVersionInfo
		);
	}
}
