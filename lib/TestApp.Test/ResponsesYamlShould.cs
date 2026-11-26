using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using Responses;

public class ResponsesYamlShould
{
	[Fact]
	public void Generate_a_result_for_each_response_range()
	{
		// 200, 2XX and 4XX are each documented, so each should have a factory besides Unsafe
		var factories = typeof(RangesControllerBase.GetRangesActionResult)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => m.ReturnType == typeof(RangesControllerBase.GetRangesActionResult) && m.Name != nameof(RangesControllerBase.GetRangesActionResult.Unsafe))
			.ToArray();
		Assert.Equal(3, factories.Length);
	}

	[Fact]
	public Task Respond_with_a_status_code_in_a_range() =>
		TestSingleRequest<RangesControllerBase.GetRangesActionResult>(new(
			RangesControllerBase.GetRangesActionResult.ClientError(404, "missing"),
			client => client.GetAsync("/responses/ranges")
		)
		{
			AssertResponseMessage = VerifyResponse(404, "missing"),
		});

	[Fact]
	public void Reject_a_status_code_outside_of_the_range() =>
		Assert.Throws<ArgumentOutOfRangeException>(() => RangesControllerBase.GetRangesActionResult.ClientError(500, "oops"));

	[Fact]
	public Task Respond_with_a_specific_status_code() =>
		TestSingleRequest<DefaultControllerBase.GetDefaultActionResult>(new(
			DefaultControllerBase.GetDefaultActionResult.Ok("fine"),
			client => client.GetAsync("/responses/default")
		)
		{
			AssertResponseMessage = VerifyResponse(200, "fine"),
		});

	[Fact]
	public Task Respond_with_the_default_response() =>
		TestSingleRequest<DefaultControllerBase.GetDefaultActionResult>(new(
			DefaultControllerBase.GetDefaultActionResult.OtherStatusCode(503, new Error("unavailable")),
			client => client.GetAsync("/responses/default")
		)
		{
			AssertResponseMessage = VerifyResponse(503, new { code = "unavailable" }),
		});

	[Fact]
	public Task Respond_with_json_of_several_media_types() =>
		TestSingleRequest<MediaTypesControllerBase.GetMediaTypesActionResult>(new(
			MediaTypesControllerBase.GetMediaTypesActionResult.ApplicationJsonOk("hello"),
			client => client.GetAsync("/responses/media-types")
		)
		{
			AssertResponseMessage = VerifyResponse(200, "hello"),
		});

	[Fact]
	public Task Respond_with_text_of_several_media_types() =>
		TestSingleRequest<MediaTypesControllerBase.GetMediaTypesActionResult>(new(
			MediaTypesControllerBase.GetMediaTypesActionResult.TextPlainOk("hello"),
			client => client.GetAsync("/responses/media-types")
		)
		{
			AssertResponseMessage = async (response) =>
			{
				Assert.Equal(200, (int)response.StatusCode);
				Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
				Assert.Equal("hello", await response.Content.ReadAsStringAsync());
			},
		});

	[Fact]
	public Task Respond_with_response_headers() =>
		TestSingleRequest<HeadersControllerBase.GetHeadersActionResult>(new(
			HeadersControllerBase.GetHeadersActionResult.Ok("ok", "10", "req-1"),
			client => client.GetAsync("/responses/headers")
		)
		{
			AssertResponseMessage = async (response) =>
			{
				await VerifyResponse(200, "ok")(response);
				Assert.Equal("10", response.Headers.GetValues("X-Rate-Limit").Single());
				Assert.Equal("req-1", response.Headers.GetValues("X-Request-Id").Single());
			},
		});

	[Fact]
	public Task Omit_optional_response_headers_without_a_value() =>
		TestSingleRequest<HeadersControllerBase.GetHeadersActionResult>(new(
			HeadersControllerBase.GetHeadersActionResult.Ok("ok", "10", null!),
			client => client.GetAsync("/responses/headers")
		)
		{
			AssertResponseMessage = async (response) =>
			{
				await VerifyResponse(200, "ok")(response);
				Assert.False(response.Headers.Contains("X-Request-Id"));
			},
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Response headers): response header values are always string, even for an integer schema")]
	public void Type_response_headers_by_their_schema()
	{
		var ok = typeof(HeadersControllerBase.GetHeadersActionResult).GetMethod(nameof(HeadersControllerBase.GetHeadersActionResult.Ok))!;
		var rateLimit = ok.GetParameters().Single(p => p.Name == "headerXRateLimit");
		Assert.Equal(typeof(int), rateLimit.ParameterType);
	}

	[Fact]
	public Task Respond_with_responses_defined_in_components() =>
		TestSingleRequest<RefControllerBase.GetRefActionResult>(new(
			RefControllerBase.GetRefActionResult.Ok("hi"),
			client => client.GetAsync("/responses/ref")
		)
		{
			AssertResponseMessage = VerifyResponse(200, "hi"),
		});

	[Fact]
	public Task Respond_with_error_responses_defined_in_components() =>
		TestSingleRequest<RefControllerBase.GetRefActionResult>(new(
			RefControllerBase.GetRefActionResult.NotFound(new Error("missing")),
			client => client.GetAsync("/responses/ref")
		)
		{
			AssertResponseMessage = VerifyResponse(404, new { code = "missing" }),
		});
}
