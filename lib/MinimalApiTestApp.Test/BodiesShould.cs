using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using Moq;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

using static Utilities;

/// <summary>Forms, cookies, and text, binary and optional bodies bind as they do in the MVC output</summary>
public class BodiesShould
{
	[Fact]
	public async Task Bind_form_fields()
	{
		(string Name, IOptional<string>? Tag, bool HasIdTag)? request = null;
		var api = new Mock<Form.IBasicApi>();
		api.Setup(a => a.PostBasicForm(It.IsAny<string>(), It.IsAny<IOptional<string>>(), It.IsAny<bool>()))
			.Callback((string name, IOptional<string> tag, bool hasIdTag) => request = (name, tag, hasIdTag))
			.ReturnsAsync(Form.IBasicApi.PostBasicFormResult.Ok(17));

		await SendAsync(api, client => client.PostAsync("/form/basic", new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["name"] = "Fido",
			["tag"] = "dog",
			["hasIdTag"] = "true",
		})), VerifyResponse(200, "application/json", "17"));

		Assert.Equal("Fido", request?.Name);
		Assert.Equal<IOptional<string>?>(Optional.Create("dog"), request?.Tag);
		Assert.True(request?.HasIdTag);
	}

	[Fact]
	public Task Bind_every_optional_form_field_that_is_sent() =>
		PostOptionalForm(new() { ["name"] = "abc", ["code"] = "ABC", ["count"] = "5", ["note"] = "hello" }, request =>
		{
			Assert.Equal<IOptional<string>?>(Optional.Create("abc"), request.Name);
			Assert.Equal<IOptional<string>?>(Optional.Create("ABC"), request.Code);
			Assert.Equal<IOptional<int>?>(Optional.Create(5), request.Count);
			Assert.Equal<IOptional<string?>?>(Optional.Create<string?>("hello"), request.Note);
		});

	[Fact]
	public Task Bind_optional_form_fields_that_are_not_sent_as_missing() =>
		// A field the operation doesn't read, since Minimal API rejects a form without a body
		PostOptionalForm(new() { ["other"] = "x" }, request =>
		{
			Assert.Null(request.Name);
			Assert.Null(request.Code);
			Assert.Null(request.Count);
			Assert.Null(request.Note);
		});

	[Fact]
	public Task Bind_an_empty_nullable_form_field_as_present_and_null() =>
		PostOptionalForm(new() { ["note"] = "" }, request =>
		{
			Assert.Equal<IOptional<string?>?>(Optional.Create<string?>(null), request.Note);
		});

	[Fact]
	public async Task Reject_a_form_without_a_body()
	{
		// MVC calls the action with every optional field missing; Minimal API rejects a [FromForm] request without a body
		var api = new Mock<ValidationOptional.IFormApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.PostAsync("/validation-optional/form", new FormUrlEncodedContent(new Dictionary<string, string>())),
			VerifyResponse(400));
	}

	[Fact]
	public async Task Reject_a_form_field_that_cannot_be_parsed()
	{
		// MVC calls the action with a model state error; Minimal API rejects the request
		var api = new Mock<ValidationOptional.IFormApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.PostAsync("/validation-optional/form", new FormUrlEncodedContent(new Dictionary<string, string> { ["count"] = "abc" })),
			VerifyResponse(400));
	}

	private static async Task PostOptionalForm(Dictionary<string, string> fields, System.Action<(IOptional<string>? Name, IOptional<string>? Code, IOptional<int>? Count, IOptional<string?>? Note)> assertRequest)
	{
		(IOptional<string>? Name, IOptional<string>? Code, IOptional<int>? Count, IOptional<string?>? Note)? request = null;
		var api = new Mock<ValidationOptional.IFormApi>();
		api.Setup(a => a.PostForm(It.IsAny<IOptional<string>>(), It.IsAny<IOptional<string>>(), It.IsAny<IOptional<int>>(), It.IsAny<IOptional<string?>>()))
			.Callback((IOptional<string> name, IOptional<string> code, IOptional<int> count, IOptional<string?> note) => request = (name, code, count, note))
			.ReturnsAsync(ValidationOptional.IFormApi.PostFormResult.NoContent());

		await SendAsync(api, client => client.PostAsync("/validation-optional/form", new FormUrlEncodedContent(fields)), VerifyResponse(204));

		Assert.NotNull(request);
		assertRequest(request.Value);
	}

	[Theory]
	[InlineData("sessionId=s-1; theme=dark%20mode", "dark mode")]
	[InlineData("sessionId=s-1", null)]
	public async Task Bind_cookie_parameters(string cookie, string? theme)
	{
		var api = new Mock<CookieParameters.ISessionApi>();
		api.Setup(a => a.GetSession("s-1", theme == null ? null! : Optional.Create(theme))).ReturnsAsync(CookieParameters.ISessionApi.GetSessionResult.Ok("s-1"));

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/cookie-parameters/session") { Headers = { { "Cookie", cookie } } }),
			VerifyResponse(200, "application/json", "\"s-1\""));
	}

	[Fact]
	public async Task Reject_a_missing_required_cookie_parameter()
	{
		var api = new Mock<CookieParameters.ISessionApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.GetAsync("/cookie-parameters/session"), VerifyResponse(400));
	}

	[Fact]
	public async Task Read_text_bodies()
	{
		var api = new Mock<RequestBodies.ITextApi>();
		api.Setup(a => a.PostText("hello")).ReturnsAsync(RequestBodies.ITextApi.PostTextResult.Ok("hello"));

		await SendAsync(api, client => client.PostAsync("/request-bodies/text", new StringContent("hello", Encoding.UTF8, "text/plain")),
			VerifyResponse(200));
	}

	[Fact]
	public async Task Read_binary_bodies()
	{
		byte[]? body = null;
		var api = new Mock<RequestBodies.IBinaryApi>();
		api.Setup(a => a.PostBinary(It.IsAny<Stream>()))
			.Returns(async (Stream stream) =>
			{
				using var copy = new MemoryStream();
				await stream.CopyToAsync(copy);
				body = copy.ToArray();
				return RequestBodies.IBinaryApi.PostBinaryResult.Ok(body.Length);
			});

		await SendAsync(api, client => client.PostAsync("/request-bodies/binary", new ByteArrayContent([1, 2, 3]) { Headers = { { "Content-Type", "application/octet-stream" } } }),
			VerifyResponse(200, "application/json", "3"));
		Assert.Equal([1, 2, 3], body);
	}

	[Fact]
	public async Task Reject_a_missing_required_body()
	{
		var api = new Mock<RequestBodies.ITextApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.PostAsync("/request-bodies/text", null), VerifyResponse(415));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Bind_an_optional_body(bool send)
	{
		IOptional<RequestBodies.Note>? body = null;
		var api = new Mock<RequestBodies.IOptionalApi>();
		api.Setup(a => a.PostOptional(It.IsAny<IOptional<RequestBodies.Note>>()))
			.Callback((IOptional<RequestBodies.Note> value) => body = value)
			.ReturnsAsync(RequestBodies.IOptionalApi.PostOptionalResult.NoContent());

		await SendAsync(api, client => client.PostAsync("/request-bodies/optional", send ? JsonContent.Create(new { text = "note" }) : null),
			VerifyResponse(204));

		if (send)
			Assert.Equal("note", Optional.GetValueOrThrow(body!).Text);
		else
			Assert.Null(body);
	}

	[Fact]
	public async Task Select_the_operation_for_the_JSON_media_type_of_several()
	{
		var api = new Mock<RequestBodies.IMultipleApi>(MockBehavior.Strict);
		api.Setup(a => a.PostMultipleapplicationJson(new RequestBodies.Note("json"))).ReturnsAsync(RequestBodies.IMultipleApi.PostMultipleResult.NoContent());

		await SendAsync(api, client => client.PostAsync("/request-bodies/multiple", JsonContent.Create(new { text = "json" })),
			VerifyResponse(204));
	}

	[Fact]
	public async Task Select_the_operation_for_the_form_media_type_of_several()
	{
		var api = new Mock<RequestBodies.IMultipleApi>(MockBehavior.Strict);
		api.Setup(a => a.PostMultipleapplicationXWwwFormUrlencoded("form")).ReturnsAsync(RequestBodies.IMultipleApi.PostMultipleResult.NoContent());

		await SendAsync(api, client => client.PostAsync("/request-bodies/multiple", new FormUrlEncodedContent(new Dictionary<string, string> { ["text"] = "form" })),
			VerifyResponse(204));
	}

	[Fact]
	public async Task Reject_media_types_not_listed()
	{
		var api = new Mock<RequestBodies.IMultipleApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.PostAsync("/request-bodies/multiple", new StringContent("text", Encoding.UTF8, "text/plain")),
			VerifyResponse(415));
	}
}
