using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using Multipart;

public class MultipartYamlShould
{
	[Fact(Skip = "OpenAPI 3.0 gap (Forms and multipart): multipart/form-data is bound [FromBody], so it returns 415")]
	public Task Accept_multipart_form_fields() =>
		TestSingleRequest<FieldsControllerBase.PostFieldsActionResult, PostFieldsRequest?>(new(
			FieldsControllerBase.PostFieldsActionResult.Ok("Fido"),
			client => client.PostAsync("/multipart/fields", new MultipartFormDataContent
			{
				{ new StringContent("Fido"), "name" },
				{ new StringContent("3"), "count" },
			})
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("Fido", body?.Name);
				Assert.Equal(3, body?.Count?.Value);
			},
			AssertResponseMessage = VerifyResponse(200, "Fido"),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Forms and multipart): multipart/form-data is bound [FromBody], so it returns 415")]
	public Task Accept_multipart_files_and_encoded_parts() =>
		TestSingleRequest<UploadControllerBase.UploadActionResult, UploadRequest?>(new(
			UploadControllerBase.UploadActionResult.Ok("logo"),
			client =>
			{
				var file = new ByteArrayContent([137, 80, 78, 71]);
				file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
				return client.PostAsync("/multipart/upload", new MultipartFormDataContent
				{
					{ new StringContent("logo"), "name" },
					{ file, "file", "logo.png" },
					{ new StringContent("""{"author":"me"}""", Encoding.UTF8, "application/json"), "metadata" },
				});
			}
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("logo", body?.Name);
				Assert.NotNull(body?.File);
				Assert.Equal("me", body?.Metadata?.Value.Author?.Value);
			},
			AssertResponseMessage = VerifyResponse(200, "logo"),
		});
}
