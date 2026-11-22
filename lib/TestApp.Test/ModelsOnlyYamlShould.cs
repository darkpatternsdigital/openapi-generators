using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

/// <summary>
/// `OpenApiSchemaModels` generates only the models of an OpenAPI document, named as they would be
/// for a server document (planning #9). Before it, projects registered such documents as
/// `OpenApiSchemaMvcServer` with a dummy PathPrefix, or as `JsonSchemaDocument` and got names
/// like `ComponentsSchemasGadget`.
/// </summary>
public class ModelsOnlyYamlShould
{
	[Fact]
	public void Name_models_like_a_server_document()
	{
		var gadget = new ModelsOnly.Gadget(Id: "g-1", Kind: ModelsOnly.GadgetKind.Small);

		Assert.Equal("g-1", gadget.Id);
	}

	[Fact]
	public void Generate_no_controllers_or_service_registration()
	{
		var generatedNamespace = typeof(ModelsOnly.Gadget).Namespace;
		var types = typeof(ModelsOnly.Gadget).Assembly.GetTypes().Where(t => t.Namespace == generatedNamespace).ToArray();

		Assert.Contains(types, t => t == typeof(ModelsOnly.GadgetKind));
		Assert.DoesNotContain(types, t => typeof(ControllerBase).IsAssignableFrom(t));
		Assert.DoesNotContain(types, t => t.Name.EndsWith("Extensions"));
	}
}
