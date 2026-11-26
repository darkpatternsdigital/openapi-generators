using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.CSharp;

public class GeneratedDocumentationShould
{
	[Fact]
	public void DocumentOperationSummariesAndDescriptions()
	{
		var operations = DynamicCompilation.GetGeneratedSources("oas-client-methods.yaml")["DPD.Controller.Operations.cs"];

		Assert.Contains("/// Gets the legacy value", operations);
		Assert.Contains("/// Use replaceItem instead.", operations);
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): deprecated operations are not marked [Obsolete]")]
	public void MarkDeprecatedOperationsObsolete()
	{
		var operations = LoadType("oas-client-methods.yaml", "DPD.Controller.Operations");

		var methods = operations.GetMethods().Where(m => m.Name == "GetLegacy").ToArray();
		Assert.NotEmpty(methods);
		Assert.All(methods, m => Assert.NotNull(m.GetCustomAttribute<ObsoleteAttribute>()));
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): deprecated properties are not marked [Obsolete]")]
	public void MarkDeprecatedPropertiesObsolete()
	{
		var account = LoadType("oas-client-models.yaml", "DPD.Controller.Account");

		Assert.NotNull(account.GetProperty("LegacyCode")?.GetCustomAttribute<ObsoleteAttribute>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): property descriptions are not emitted as doc comments")]
	public void DocumentPropertyDescriptions()
	{
		var account = DynamicCompilation.GetGeneratedSources("oas-client-models.yaml")["DPD.Controller.Account.cs"];

		Assert.Contains("The display name of the account", account);
	}

	private static Type LoadType(string documentName, string typeName)
	{
		var assembly = Assembly.Load(DynamicCompilation.GetGeneratedLibrary(documentName));
		return assembly.GetType(typeName, throwOnError: true)!;
	}
}
