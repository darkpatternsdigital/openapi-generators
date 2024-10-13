using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApi.TypeScript;

public record TransformSettings(ISchemaRegistry SchemaRegistry, PartialHeader Header);
