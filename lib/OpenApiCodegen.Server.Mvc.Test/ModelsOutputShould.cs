using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.CSharp;

/// <summary>
/// The Models output (`OpenApiSchemaModels`) generates only an OpenAPI document's models, named as
/// they would be for a server document (planning #9).
/// </summary>
public class ModelsOutputShould
{
	[Fact]
	public void Generate_only_the_models_of_a_models_document()
	{
		var result = Generate("models-only.yaml", "Models");

		Assert.Empty(result.Diagnostics);
		Assert.Equal(["Models.Gadget.cs", "Models.GadgetKind.cs"], result.Sources.Select(s => s.Key).Order());
		var gadget = result.Sources.Single(s => s.Key == "Models.Gadget.cs").SourceText;
		Assert.Contains("global::Models.GadgetKind Kind", gadget);
		Assert.Contains("string Id", gadget);
	}

	[Fact]
	public void Name_models_referenced_from_a_server_document()
	{
		var directory = Path.GetTempPath();
		var models = new AdditionalTextInfo(
			Path: Path.Combine(directory, "shared.yaml"),
			Contents: """
				openapi: '3.0.3'
				info: { title: Shared, version: 1.0.0 }
				paths: {}
				components:
				  schemas:
				    Gadget:
				      type: object
				      properties:
				        id: { type: string }
				""",
			Types: ["Models"],
			Metadata: new Dictionary<string, string?> { ["Namespace"] = "Shared" }
		);
		var server = new AdditionalTextInfo(
			Path: Path.Combine(directory, "server.yaml"),
			Contents: """
				openapi: '3.0.3'
				info: { title: Server, version: 1.0.0 }
				paths:
				  /gadget:
				    get:
				      operationId: getGadget
				      responses:
				        '200':
				          description: The gadget
				          content:
				            application/json:
				              schema:
				                $ref: 'shared.yaml#/components/schemas/Gadget'
				""",
			Types: ["MvcServer"],
			Metadata: new Dictionary<string, string?> { ["Namespace"] = "Server" }
		);

		var result = new CSharpGenerator().Generate([models, server]);

		Assert.Empty(result.Diagnostics);
		// Without the Models output, this would be `ComponentsSchemasGadget`
		Assert.Contains("Shared.Gadget.cs", result.Sources.Select(s => s.Key));
		Assert.Contains("global::Shared.Gadget", result.Sources.Single(s => s.Key == "Server.GadgetControllerBase.cs").SourceText);
	}

	private static GenerationResult Generate(string schemaName, string type)
	{
		using var reader = new StreamReader(DocumentLoader.GetEmbeddedDocumentStream(new Uri(DocumentLoader.Embedded, schemaName)));
		var document = new AdditionalTextInfo(
			Path: Path.Combine(Path.GetTempPath(), schemaName),
			Contents: reader.ReadToEnd(),
			Types: [type],
			Metadata: new Dictionary<string, string?> { ["Namespace"] = "Models" }
		);
		return new CSharpGenerator().Generate([document]);
	}
}
