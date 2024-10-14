using System;
using System.Xml.Linq;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.OpenApiCodegen.Handlebars.Templates;

public record PartialHeader(
	string? AppTitle,
	string? AppDescription,
	string CodeGeneratorVersionInfo
);
