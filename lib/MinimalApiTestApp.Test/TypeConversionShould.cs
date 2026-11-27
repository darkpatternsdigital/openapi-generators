using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp.FormAndCookieTypes;
using Moq;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

using static Utilities;

/// <summary>Form fields and cookies are converted to enums, bytes and numbers as the MVC output converts them</summary>
public class TypeConversionShould
{
	private sealed record FormTypes(Choice Choice, IOptional<Choice>? OptionalChoice, byte[] Data, IOptional<byte[]>? OptionalData, string? Note);

	private static async Task<FormTypes?> PostFormTypesAsync(Dictionary<string, string> fields, int expectedStatusCode = 204)
	{
		FormTypes? request = null;
		var api = new Mock<IFormTypesApi>();
		api.Setup(a => a.PostFormTypes(It.IsAny<Choice>(), It.IsAny<IOptional<Choice>>(), It.IsAny<byte[]>(), It.IsAny<IOptional<byte[]>>(), It.IsAny<string?>()))
			.Callback((Choice choice, IOptional<Choice> optionalChoice, byte[] data, IOptional<byte[]> optionalData, string? note) => request = new(choice, optionalChoice, data, optionalData, note))
			.ReturnsAsync(IFormTypesApi.PostFormTypesResult.NoContent());

		await SendAsync(api, client => client.PostAsync("/form-and-cookie-types/form-types", new FormUrlEncodedContent(fields)), VerifyResponse(expectedStatusCode));
		return request;
	}

	[Fact]
	public async Task Bind_form_fields_that_need_conversion()
	{
		var request = await PostFormTypesAsync(new() { ["choice"] = "rock-paper", ["optionalChoice"] = "scissors", ["data"] = "AQID", ["optionalData"] = "BAU=", ["note"] = "hi" });

		Assert.Equal(Choice.RockPaper, request?.Choice);
		Assert.Equal<IOptional<Choice>?>(Optional.Create(Choice.Scissors), request?.OptionalChoice);
		Assert.Equal([1, 2, 3], request?.Data);
		Assert.Equal([4, 5], Optional.GetValueOrThrow(request!.OptionalData!));
		Assert.Equal("hi", request.Note);
	}

	[Fact]
	public async Task Bind_missing_optional_form_fields_that_need_conversion_as_missing()
	{
		var request = await PostFormTypesAsync(new() { ["choice"] = "scissors", ["data"] = "AQID", ["note"] = "hi" });

		Assert.Null(request?.OptionalChoice);
		Assert.Null(request?.OptionalData);
	}

	[Fact]
	public async Task Bind_an_empty_required_nullable_form_field_as_null()
	{
		var request = await PostFormTypesAsync(new() { ["choice"] = "scissors", ["data"] = "AQID", ["note"] = "" });

		Assert.NotNull(request);
		Assert.Null(request.Note);
	}

	[Theory]
	// The C# member name isn't the schema value
	[InlineData("RockPaper")]
	[InlineData("lizard")]
	public async Task Reject_form_enum_values_that_are_not_in_the_schema(string value)
	{
		var request = await PostFormTypesAsync(new() { ["choice"] = value, ["data"] = "AQID", ["note"] = "hi" }, expectedStatusCode: 400);

		Assert.Null(request);
	}

	[Theory]
	[InlineData("count=5; choice=rock-paper", 5, Choice.RockPaper)]
	[InlineData("count=5", 5, null)]
	// An empty cookie is omitted
	[InlineData("count=; choice=", null, null)]
	[InlineData("other=1", null, null)]
	public async Task Bind_optional_cookies_as_IOptional(string cookie, int? count, Choice? choice)
	{
		(IOptional<int>? Count, IOptional<Choice>? Choice)? request = null;
		var api = new Mock<ICookieTypesApi>();
		api.Setup(a => a.GetCookieTypes(It.IsAny<IOptional<int>>(), It.IsAny<IOptional<Choice>>()))
			.Callback((IOptional<int> countValue, IOptional<Choice> choiceValue) => request = (countValue, choiceValue))
			.ReturnsAsync(ICookieTypesApi.GetCookieTypesResult.NoContent());

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/form-and-cookie-types/cookie-types") { Headers = { { "Cookie", cookie } } }),
			VerifyResponse(204));

		Assert.Equal(count is { } c ? Optional.Create(c) : null, request?.Count);
		Assert.Equal(choice is { } ch ? Optional.Create(ch) : null, request?.Choice);
	}

	private sealed record QueryAndHeaderTypes(IOptional<int>? Count, IOptional<Choice>? Choice, IOptional<IEnumerable<Choice>>? Choices, IOptional<string>? Note);

	private static async Task<QueryAndHeaderTypes?> GetQueryAndHeaderTypesAsync(string query, string? note)
	{
		QueryAndHeaderTypes? request = null;
		var api = new Mock<IQueryAndHeaderTypesApi>();
		api.Setup(a => a.GetQueryAndHeaderTypes(It.IsAny<IOptional<int>>(), It.IsAny<IOptional<Choice>>(), It.IsAny<IOptional<IEnumerable<Choice>>>(), It.IsAny<IOptional<string>>()))
			.Callback((IOptional<int> count, IOptional<Choice> choice, IOptional<IEnumerable<Choice>> choices, IOptional<string> xNote) => request = new(count, choice, choices, xNote))
			.ReturnsAsync(IQueryAndHeaderTypesApi.GetQueryAndHeaderTypesResult.NoContent());

		var message = new HttpRequestMessage(HttpMethod.Get, "/form-and-cookie-types/query-and-header-types" + query);
		if (note != null)
			message.Headers.Add("X-Note", note);
		await SendAsync(api, client => client.SendAsync(message), VerifyResponse(204));
		return request;
	}

	[Fact]
	public async Task Bind_optional_query_and_header_parameters_that_are_sent_as_IOptional()
	{
		var request = await GetQueryAndHeaderTypesAsync("?count=5&choice=scissors&choices=rock-paper&choices=scissors", "hi");

		Assert.Equal<IOptional<int>?>(Optional.Create(5), request?.Count);
		Assert.Equal<IOptional<Choice>?>(Optional.Create(Choice.Scissors), request?.Choice);
		Assert.Equal([Choice.RockPaper, Choice.Scissors], Optional.GetValueOrThrow(request!.Choices!));
		Assert.Equal<IOptional<string>?>(Optional.Create("hi"), request.Note);
	}

	[Theory]
	[InlineData("", null)]
	// An empty value is omitted, as for cookies
	[InlineData("?count=&choice=&choices=", "")]
	public async Task Bind_omitted_optional_query_and_header_parameters_as_null(string query, string? note)
	{
		var request = await GetQueryAndHeaderTypesAsync(query, note);

		Assert.NotNull(request);
		Assert.Null(request.Count);
		Assert.Null(request.Choice);
		Assert.Null(request.Choices);
		Assert.Null(request.Note);
	}

	[Theory]
	[InlineData("count=abc")]
	[InlineData("choice=RockPaper")]
	public async Task Reject_cookies_that_cannot_be_converted(string cookie)
	{
		var api = new Mock<ICookieTypesApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/form-and-cookie-types/cookie-types") { Headers = { { "Cookie", cookie } } }),
			VerifyResponse(400));
	}
}
