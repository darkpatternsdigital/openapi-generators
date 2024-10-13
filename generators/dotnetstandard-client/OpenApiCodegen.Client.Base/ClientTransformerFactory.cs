using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApi.CSharp;

public class ClientTransformerFactory(TransformSettings settings)
{
	public CSharpClientTransformer Build(OpenApiDocument document, CSharpSchemaOptions options)
	{
		var handlebarsFactory = new HandlebarsFactory(ControllerHandlebarsTemplateProcess.CreateHandlebars);
		return new CSharpClientTransformer(settings, document, options, handlebarsFactory);
	}
}
