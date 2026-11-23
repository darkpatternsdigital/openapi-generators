using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using static DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils.GenerationUtilities;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Integration;

[Collection(CommonDirectoryFixture.CollectionName)]
public class FormYamlShould
{
	private readonly CommonDirectoryFixture commonDirectory;

	public FormYamlShould(CommonDirectoryFixture commonDirectory)
	{
		this.commonDirectory = commonDirectory;
	}

	[Fact]
	public async Task Be_able_to_generate_the_request()
	{
		var body = new { name = "Fido", tag = "dog", hasIdTag = true };
		// Form fields are sent as strings
		var expectedBody = new { name = "Fido", tag = "dog", hasIdTag = "true" };

		var result = await commonDirectory.ConvertRequest("form.yaml", "postBasicForm", new { }, body, contentType: "application/x-www-form-urlencoded");

		AssertRequestSuccess(result, "POST", "/basic", expectedBody, contentType: "application/x-www-form-urlencoded");
	}

	[Fact]
	public async Task Leave_out_the_optional_tag_when_not_set()
	{
		var body = new { name = "Fido", hasIdTag = false };
		var expectedBody = new { name = "Fido", hasIdTag = "false" };

		var result = await commonDirectory.ConvertRequest("form.yaml", "postBasicForm", new { }, body, contentType: "application/x-www-form-urlencoded");

		AssertRequestSuccess(result, "POST", "/basic", expectedBody, contentType: "application/x-www-form-urlencoded");
	}

}
