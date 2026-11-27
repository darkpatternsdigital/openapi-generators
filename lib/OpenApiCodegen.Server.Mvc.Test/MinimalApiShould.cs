using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// The Minimal API output (planning #30): an interface per path group, implemented by the user and resolved
/// from DI, a result type per operation, and a generated Map method that wires every route of the group.
/// </summary>
public class MinimalApiShould
{
	// The same documents as ComprehensiveTransformsShould
	[Theory]
	[InlineData("all-of.yaml")]
	[InlineData("enum.yaml")]
	[InlineData("controller-extension.yaml")]
	[InlineData("regex-escape.yaml")]
	[InlineData("validation-min-max.yaml")]
	[InlineData("path-level-parameters.yaml")]
	[InlineData("headers.yaml")]
	[InlineData("one-of.yaml")]
	[InlineData("nullable-vs-optional.yaml")]
	[InlineData("annotations.yaml")]
	[InlineData("request-ref.yaml")]
	[InlineData("response-ref.yaml")]
	[InlineData("oas-server-http-methods.yml")]
	[InlineData("oas-server-http-trace.yml")]
	[InlineData("oas-server-paths.yml")]
	[InlineData("oas-server-responses.yml")]
	[InlineData("oas-server-validation.yml")]
	[InlineData("oas-server-no-operation-id-body.yml")]
	[InlineData("form.yaml")]
	[InlineData("validation-optional.yaml")]
	[InlineData("nullable-vs-optional-legacy.yaml")]
	[InlineData("oas-server-cookie-parameters.yml")]
	[InlineData("oas-server-request-bodies.yml")]
	[InlineData("form-and-cookie-types.yaml")]
	[InlineData("oas-server-multipart.yml")]
	[InlineData("oas-server-parameter-styles.yml")]
	[InlineData("oas-server-documentation.yml")]
	[InlineData("oas-server-integer-enum.yml")]
	[InlineData("oas-server-models.yml")]
	[InlineData("oas-server-security.yml")]
	[InlineData("oas-server-servers.yml")]
	[InlineData("oas-server-xml.yml")]
	[InlineData("oauth.yaml")]
	[InlineData("openapi-3.0-callbacks.yaml")]
	public void Compile(string name)
	{
		DynamicCompilation.GetGeneratedLibrary(name, minimalApi: true);
	}

	[Fact]
	public void Generate_an_interface_and_a_map_method_per_group()
	{
		var sources = DynamicCompilation.GetGeneratedSources("one-of.yaml", minimalApi: true);

		var source = sources["DPD.Controller.IPetApi.cs"];
		Assert.Contains("public interface IPetApi", source);
		Assert.Contains("public static global::Microsoft.AspNetCore.Routing.RouteGroupBuilder MapPetApi(this global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)", source);
		Assert.DoesNotContain(sources.Keys, k => k.EndsWith("ControllerBase.cs"));
	}

	[Fact]
	public void Not_reserve_the_names_of_ControllerBase_members()
	{
		var sources = DynamicCompilation.GetGeneratedSources("headers.yaml", minimalApi: true);

		// Redirect is a ControllerBase method, so the MVC output names the operation _Redirect
		Assert.Contains("Task<RedirectResult> Redirect(", sources["DPD.Controller.IRedirectApi.cs"]);
	}

	[Fact]
	public void Compile_operations_named_like_the_result_types_helpers()
	{
		// header and body would be the HeaderResult and BodyResult structs, which contain the result's helper classes;
		// the enum array covers binding each query value as an enum
		var result = GenerateMinimalApi("""
			openapi: 3.0.3
			info: { title: Names, version: 1.0.0 }
			paths:
			  /header:
			    get:
			      operationId: header
			      parameters:
			        - name: options
			          in: query
			          schema: { type: array, items: { type: string, enum: [a, b] } }
			      responses:
			        '200':
			          description: OK
			          content: { application/json: { schema: { type: string } } }
			  /body:
			    get:
			      operationId: body
			      responses:
			        '200':
			          description: OK
			          content: { application/json: { schema: { type: string } } }
			""");

		Assert.Empty(result.Diagnostics);
		DynamicCompilation.Compile(result.Sources.Select(s => (s.Key, s.SourceText)));
	}

	private static GenerationResult GenerateMinimalApi(string yaml) =>
		new CSharpGenerator().Generate([
			new AdditionalTextInfo(
				Path: Path.Combine(Path.GetTempPath(), "minimal-api.yaml"),
				Contents: yaml,
				Types: ["MinimalApi"],
				Metadata: new Dictionary<string, string?> { ["Namespace"] = "DPD.Controller" }
			),
		]);

	[Fact]
	public void Generate_for_files_listed_as_OpenApiSchemaMinimalApi()
	{
		const string schemaName = "simple-types.yaml";
		using var reader = new StreamReader(DocumentLoader.GetEmbeddedDocumentStream(new Uri(DocumentLoader.Embedded, schemaName)));
		var document = new AdditionalTextInfo(
			Path: Path.Combine(Path.GetTempPath(), schemaName),
			Contents: reader.ReadToEnd(),
			Types: ["MinimalApi"],
			Metadata: new Dictionary<string, string?> { ["Namespace"] = "Shared", ["DPDMinimalApiNamespace"] = "Endpoints" }
		);

		var result = new CSharpGenerator().Generate([document]);

		Assert.Empty(result.Diagnostics);
		var keys = result.Sources.Select(s => s.Key).ToArray();
		Assert.Contains("Endpoints.IDataApi.cs", keys);
		Assert.Contains("Shared.GetDataResponse.cs", keys);
		Assert.DoesNotContain(keys, k => k.EndsWith("ControllerBase.cs"));
	}
}
