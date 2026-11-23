using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;

public class FormYamlShould
{
	[Fact]
	public Task Handle_FormUrlEncodedContent_requests() =>
		TestSingleRequest<Form.BasicControllerBase.PostBasicFormActionResult, (string Name, IOptional<string> Tag, bool HasIdTag)>(new(
			Form.BasicControllerBase.PostBasicFormActionResult.Ok(17),
			client => client.PostAsync("/form/basic", new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["name"] = "Fido",
				["tag"] = "dog",
				["hasIdTag"] = "true",
			}))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("Fido", request.Name);
				Assert.Equal<IOptional<string>?>(Optional.Create("dog"), request.Tag);
				Assert.True(request.HasIdTag);
			},
			AssertResponseMessage = VerifyResponse(200, 17),
		});

	[Fact]
	public Task Handle_requests_without_the_optional_tag() =>
		TestSingleRequest<Form.BasicControllerBase.PostBasicFormActionResult, (string Name, IOptional<string>? Tag, bool HasIdTag)>(new(
			Form.BasicControllerBase.PostBasicFormActionResult.Ok(17),
			client => client.PostAsync("/form/basic", new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["name"] = "Fido",
				["hasIdTag"] = "false",
			}))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("Fido", request.Name);
				Assert.Null(request.Tag);
				Assert.False(request.HasIdTag);
			},
			AssertResponseMessage = VerifyResponse(200, 17),
		});
}
