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
	private static readonly Version MinimumJsonExtensionsVersion = new Version(0, 22, 0, 0);
	private static readonly DiagnosticDescriptor UpdateDependentDll = new DiagnosticDescriptor(id: "DPDAPI002",
																								title: "Update the reference to DarkPatterns.OpenApiCodegen.Json.Extensions",
																								messageFormat: "Generated code requires DarkPatterns.OpenApiCodegen.Json.Extensions {0} or later, but version {1} is referenced",
																								category: "DarkPatterns.OpenApiCodegen.CSharp",
																								DiagnosticSeverity.Warning,
																								isEnabledByDefault: true);

	// oneOf models use System.Text.Json 11's union support when the framework has it (.NET 11+), or when an options
	// file sets jsonUnions, so the generator needs to know what the compilation has
	private const string polySharpVersionProperty = "build_property._DPDPolySharpVersion";
	private static readonly Version MinimumPolySharpVersion = new Version(1, 17, 0);

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

	protected override IEnumerable<KeyValuePair<string, string>> GetCompilationMetadata(Compilation compilation, AnalyzerConfigOptions globalOptions)
	{
		var unionAttribute = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.UnionAttribute");
		var unionInterface = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.IUnion");
		// From a reference rather than the project's own source, as on .NET 11
		var frameworkUnionTypes = unionAttribute != null && unionInterface != null
			&& !SymbolEqualityComparer.Default.Equals(unionAttribute.ContainingAssembly, compilation.Assembly)
			&& !SymbolEqualityComparer.Default.Equals(unionInterface.ContainingAssembly, compilation.Assembly);
		// PolySharp generates the union types in the same compilation, which this generator can't see
		var unionTypes = (unionAttribute != null && unionInterface != null) || HasPolySharpUnionTypes(globalOptions);
		var jsonTypeClassifier = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonTypeClassifierFactory`1") != null;

		return [
			new("compilation.jsonTypeClassifier", ToMetadata(jsonTypeClassifier)),
			new("compilation.frameworkUnionTypes", ToMetadata(frameworkUnionTypes)),
			new("compilation.unionTypes", ToMetadata(unionTypes)),
		];

		static string ToMetadata(bool value) => value ? "true" : "false";
	}

	private static bool HasPolySharpUnionTypes(AnalyzerConfigOptions globalOptions) =>
		globalOptions.TryGetValue(polySharpVersionProperty, out var polySharpVersion)
		&& Version.TryParse(polySharpVersion.Split('-')[0], out var version)
		&& version >= MinimumPolySharpVersion;

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
