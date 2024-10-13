using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Abstractions;
using System.Linq;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApi.CSharp;

public record TransformSettings(SchemaRegistry SchemaRegistry, PartialHeader Header)
{
	// TODO: This doesn't seem like the right spot, but it is the correct namespace for the current level of abstraction... which may need to change.
	public static CompositeOpenApiSourceProvider BuildComposite(
		OpenApiDocument document,
		DocumentRegistry documentRegistry,
		string versionInfo,
		System.Func<TransformSettings, ISourceProvider>[] factories)
	{
		var header = new PartialHeader(
			AppName: document.Info.Title,
			AppDescription: document.Info.Description,
			Version: document.Info.Version,
			InfoEmail: document.Info.Contact?.Email,
			CodeGeneratorVersionInfo: versionInfo
		);
		var schemaRegistry = new SchemaRegistry(documentRegistry);
		var settings = new TransformSettings(schemaRegistry, header);

		return new CompositeOpenApiSourceProvider(
			factories.Select(factory => factory(settings)).ToArray()
		);
	}

}
