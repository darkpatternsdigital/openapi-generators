using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using static DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils.GenerationUtilities;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Integration;

[Collection(CommonDirectoryFixture.CollectionName)]
public class SimpleTypesShould
{
	private readonly CommonDirectoryFixture commonDirectory;

	public SimpleTypesShould(CommonDirectoryFixture commonDirectory)
	{
		this.commonDirectory = commonDirectory;
	}

	[Fact]
	public async Task Be_able_to_generate_the_request()
	{
		var result = await commonDirectory.ConvertRequest("simple-types.yaml", "getData", new { });

		AssertRequestSuccess(result, "POST", "/data");
	}

}
