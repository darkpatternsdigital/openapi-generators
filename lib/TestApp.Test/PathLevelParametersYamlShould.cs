using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using PathLevelParameters;

public class PathLevelParametersYamlShould
{
	[Fact]
	public Task Pass_path_level_parameters_to_each_operation() =>
		TestSingleRequest<WidgetsWidgetIdControllerBase.GetWidgetActionResult, (string WidgetId, bool? Verbose)>(new(
			WidgetsWidgetIdControllerBase.GetWidgetActionResult.Ok("widget"),
			client => client.GetAsync("/path-level-parameters/widgets/w-1?verbose=true")
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(("w-1", true), request);
			},
			AssertResponseMessage = VerifyResponse(200, "widget"),
		});

	[Fact]
	public Task Use_the_operation_parameter_that_overrides_a_path_level_one() =>
		TestSingleRequest<WidgetsWidgetIdControllerBase.DeleteWidgetActionResult, (string WidgetId, bool Verbose, string? Reason)>(new(
			WidgetsWidgetIdControllerBase.DeleteWidgetActionResult.NoContent(),
			client => client.DeleteAsync("/path-level-parameters/widgets/w-1?reason=old")
		)
		{
			AssertRequest = (controller, request) =>
			{
				// `verbose` is required on delete (the override), but optional at the path level
				Assert.False(controller.ModelState.IsValid);
			},
			AssertResponseMessage = VerifyResponse(204),
		});
}
