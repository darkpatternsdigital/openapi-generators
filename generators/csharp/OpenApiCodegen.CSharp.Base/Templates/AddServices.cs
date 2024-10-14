using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApi.CSharp.Templates;

public record AddServicesModel(PartialHeader Header, string MethodName, string PackageName, ControllerReference[] Controllers);

public record ControllerReference(string GenericTypeName, string ClassName);
