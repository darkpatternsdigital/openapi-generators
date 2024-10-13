using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
namespace DarkPatterns.OpenApi.CSharp;

public class ClientTransformerFactory(TransformSettings settings)
{
	public CSharpClientTransformer Build(OpenApiDocument document, CSharpSchemaOptions options)
	{
		var handlebarsFactory = new HandlebarsFactory(ControllerHandlebarsTemplateProcess.CreateHandlebars);
		return new CSharpClientTransformer(settings, document, options, handlebarsFactory);
	}

	public static CompositeOpenApiSourceProvider BuildComposite(OpenApiDocument document, DocumentRegistry documentRegistry, string versionInfo, CSharpSchemaOptions options)
	{
		return TransformSettings.BuildComposite(document, documentRegistry, versionInfo, [
			(s) => new ClientTransformerFactory(s).Build(document, options),
			(s) => new CSharpSchemaSourceProvider(s, options)
		]);
	}

}
