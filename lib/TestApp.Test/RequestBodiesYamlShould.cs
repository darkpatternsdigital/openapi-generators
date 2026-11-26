using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using RequestBodies;

public class RequestBodiesYamlShould
{
	[Fact(Skip = "OpenAPI 3.0 gap (Request bodies): text/plain is bound [FromBody] with no text input formatter, so it returns 415")]
	public Task Accept_text_plain_bodies() =>
		TestSingleRequest<TextControllerBase.PostTextActionResult, string>(new(
			TextControllerBase.PostTextActionResult.Ok("hello"),
			client => client.PostAsync("/request-bodies/text", new StringContent("hello", Encoding.UTF8, "text/plain"))
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("hello", body);
			},
			AssertResponseMessage = VerifyResponse(200, "hello"),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Request bodies): application/octet-stream is bound [FromBody] Stream with no input formatter, so it returns 415")]
	public Task Accept_octet_stream_bodies() =>
		TestSingleRequest<BinaryControllerBase.PostBinaryActionResult, byte[]>(new(
			BinaryControllerBase.PostBinaryActionResult.Ok(3),
			client => client.PostAsync("/request-bodies/binary", new ByteArrayContent([1, 2, 3])
			{
				Headers = { { "Content-Type", "application/octet-stream" } },
			})
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal([1, 2, 3], body);
			},
			AssertResponseMessage = VerifyResponse(200, 3),
		});

	[Fact]
	public Task Accept_an_optional_body() =>
		TestSingleRequest<OptionalControllerBase.PostOptionalActionResult, Note?>(new(
			OptionalControllerBase.PostOptionalActionResult.NoContent(),
			client => client.PostAsync("/request-bodies/optional", JsonContent.Create(new { text = "note" }))
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("note", body?.Text);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Request bodies): required: false is ignored (body is [Required]); a request without a body returns 415")]
	public Task Accept_a_request_without_the_optional_body() =>
		TestSingleRequest<OptionalControllerBase.PostOptionalActionResult, Note?>(new(
			OptionalControllerBase.PostOptionalActionResult.NoContent(),
			client => client.PostAsync("/request-bodies/optional", null)
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Null(body);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Accept_the_json_media_type_of_several() =>
		TestSingleRequest<MultipleControllerBase.PostMultipleActionResult, (string MediaType, string? Text)>(new(
			MultipleControllerBase.PostMultipleActionResult.NoContent(),
			client => client.PostAsync("/request-bodies/multiple", JsonContent.Create(new { text = "json" }))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(("application/json", "json"), request);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Accept_the_form_media_type_of_several() =>
		TestSingleRequest<MultipleControllerBase.PostMultipleActionResult, (string MediaType, string? Text)>(new(
			MultipleControllerBase.PostMultipleActionResult.NoContent(),
			client => client.PostAsync("/request-bodies/multiple", new FormUrlEncodedContent(new Dictionary<string, string> { ["text"] = "form" }))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(("application/x-www-form-urlencoded", "form"), request);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Reject_media_types_not_listed() =>
		TestRequestBlockedByFramework(
			client => client.PostAsync("/request-bodies/multiple", new StringContent("text", Encoding.UTF8, "text/plain")),
			response =>
			{
				Assert.Equal(415, (int)response.StatusCode);
				return Task.CompletedTask;
			});

	[Fact]
	public Task Accept_request_bodies_and_responses_defined_in_components() =>
		TestSingleRequest<RefControllerBase.PostRefActionResult, Note?>(new(
			RefControllerBase.PostRefActionResult.Ok(new Note("saved")),
			client => client.PostAsync("/request-bodies/ref", JsonContent.Create(new { text = "note" }))
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("note", body?.Text);
			},
			AssertResponseMessage = VerifyResponse(200, new { text = "saved" }),
		});
}
