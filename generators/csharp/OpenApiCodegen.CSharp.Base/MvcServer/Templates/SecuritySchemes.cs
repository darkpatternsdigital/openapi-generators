namespace DarkPatterns.OpenApiCodegen.CSharp.MvcServer.Templates;

public record SecuritySchemesModel(Handlebars.Templates.PartialHeader Header, string ClassName, string PackageName, SecuritySchemeReference[] Schemes);

public record SecuritySchemeReference(string PropertyName, string Scheme);

