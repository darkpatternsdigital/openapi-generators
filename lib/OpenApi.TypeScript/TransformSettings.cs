using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApi.TypeScript;

public record TransformSettings(ISchemaRegistry SchemaRegistry, Templates.PartialHeader Header);
