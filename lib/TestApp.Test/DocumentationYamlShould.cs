using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using Documentation;

public class DocumentationYamlShould
{
	[Fact]
	public Task Route_operations_with_documentation() =>
		TestSingleRequest<GadgetsControllerBase.ListGadgetsActionResult, (string? Color, string? LegacyFilter)>(new(
			GadgetsControllerBase.ListGadgetsActionResult.Ok([new Gadget("sprocket", null)]),
			client => client.GetAsync("/documentation/gadgets?color=red&legacyFilter=old")
		)
		{
			AssertRequest = (controller, request) => Assert.Equal(("red", "old"), request),
			AssertResponseMessage = VerifyResponse(200, new[] { new { name = "sprocket" } }),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): deprecated operations are not marked [Obsolete]")]
	public void Mark_deprecated_operations_obsolete()
	{
		var method = typeof(OldGadgetsControllerBase).GetMethod("ListOldGadgets", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		Assert.NotNull(method.GetCustomAttribute<ObsoleteAttribute>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): deprecated parameters are not marked [Obsolete]")]
	public void Mark_deprecated_parameters_obsolete()
	{
		var method = typeof(GadgetsControllerBase).GetMethod("ListGadgets", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		var parameter = method.GetParameters().Single(p => p.Name == "legacyFilter");
		Assert.NotNull(parameter.GetCustomAttribute<ObsoleteAttribute>());
	}

	[Fact(Skip = "OpenAPI 3.0 gap (Documentation): deprecated properties are not marked [Obsolete]")]
	public void Mark_deprecated_properties_obsolete()
	{
		var property = typeof(Gadget).GetProperty(nameof(Gadget.OldName));
		Assert.NotNull(property);
		Assert.NotNull(property.GetCustomAttribute<ObsoleteAttribute>());
	}
}
