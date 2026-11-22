using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using static DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils.GenerationUtilities;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Integration;

[Collection(CommonDirectoryFixture.CollectionName)]
public class PathLevelParametersYamlShould
{
	private readonly CommonDirectoryFixture commonDirectory;

	public PathLevelParametersYamlShould(CommonDirectoryFixture commonDirectory)
	{
		this.commonDirectory = commonDirectory;
	}

	[Fact]
	public async Task Include_path_level_parameters()
	{
		var result = await commonDirectory.ConvertRequest("path-level-parameters.yaml", "getWidget", new { widgetId = "w-1", verbose = true });

		AssertRequestSuccess(result, "GET", "/widgets/w-1?verbose=true");
	}

	[Fact]
	public async Task Include_operation_parameters_that_override_path_level_ones()
	{
		var result = await commonDirectory.ConvertRequest("path-level-parameters.yaml", "deleteWidget", new { widgetId = "w-1", verbose = false, reason = "old" });

		AssertRequestSuccess(result, "DELETE", "/widgets/w-1?verbose=false&reason=old");
	}
}
