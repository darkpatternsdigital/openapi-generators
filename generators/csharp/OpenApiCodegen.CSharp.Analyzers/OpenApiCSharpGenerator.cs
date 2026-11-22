using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

#if NETSTANDARD2_0
#nullable disable warnings
#endif

namespace DarkPatterns.OpenApiCodegen.CSharp;

[Generator]
public sealed class OpenApiCSharpGenerator() : BaseGenerator("DarkPatterns.OpenApiCodegen.CSharp.CSharpGenerator", "DarkPatterns.OpenApiCodegen.CSharp.Base")
{
	private const string includeKey = "DPDInclude";
	private const string jsonSchemaType = "DPDJsonSchema";
	private const string mvcServerType = "DPDGenerateMvcServer";
	private const string clientType = "DPDGenerateClient";
	private const string webhookClientType = "DPDGenerateWebhookClient";
	private const string modelsType = "DPDGenerateModels";
	private const string configType = "DPDSchemaOptions";
	private static readonly DiagnosticDescriptor IncludeDependentDll = new DiagnosticDescriptor(id: "DPDAPI001",
																								title: "Include a reference to DarkPatterns.OpenApiCodegen.Json.Extensions",
																								messageFormat: "Include a reference to DarkPatterns.OpenApiCodegen.Json.Extensions",
																								category: "DarkPatterns.OpenApiCodegen.CSharp",
																								DiagnosticSeverity.Warning,
																								isEnabledByDefault: true);

	// Generated code uses Json.Extensions APIs added in this version. Raise it whenever templates
	// start using newer Json.Extensions APIs.
	private static readonly Version MinimumJsonExtensionsVersion = new Version(0, 19, 0, 0);
	private static readonly DiagnosticDescriptor UpdateDependentDll = new DiagnosticDescriptor(id: "DPDAPI002",
																								title: "Update the reference to DarkPatterns.OpenApiCodegen.Json.Extensions",
																								messageFormat: "Generated code requires DarkPatterns.OpenApiCodegen.Json.Extensions {0} or later, but version {1} is referenced",
																								category: "DarkPatterns.OpenApiCodegen.CSharp",
																								DiagnosticSeverity.Warning,
																								isEnabledByDefault: true);

	protected override void ReportCompilationDiagnostics(Compilation compilation, CompilerApis apis)
	{
		// check that the users compilation references the expected library
		var jsonExtensions = compilation.ReferencedAssemblyNames.FirstOrDefault(static ai => ai.Name.Equals("DarkPatterns.OpenApiCodegen.Json.Extensions", StringComparison.OrdinalIgnoreCase));
		if (jsonExtensions == null)
		{
			apis.ReportDiagnostic(Diagnostic.Create(IncludeDependentDll, Location.None));
		}
		else if (jsonExtensions.Version < MinimumJsonExtensionsVersion)
		{
			apis.ReportDiagnostic(Diagnostic.Create(UpdateDependentDll, Location.None, MinimumJsonExtensionsVersion.ToString(3), jsonExtensions.Version.ToString(3)));
		}
	}

	protected override string[] GetFileTypes(AdditionalTextWithOptions additionalText)
	{
		return [
			.. IfHasProperty(includeKey, "Other"),
			.. IfHasProperty(jsonSchemaType, "JsonSchema"),
			.. IfHasProperty(mvcServerType, "MvcServer"),
			.. IfHasProperty(webhookClientType, "WebhookClient"),
			.. IfHasProperty(clientType, "Client"),
			.. IfHasProperty(modelsType, "Models"),
			.. IfHasProperty(configType, "Config"),
		];

		IEnumerable<string> IfHasProperty(string property, string result)
		{
			var v = additionalText.ConfigOptions.GetAdditionalFilesMetadata(property);
			return v == "true" ? [result] : [];
		}
	}
}
