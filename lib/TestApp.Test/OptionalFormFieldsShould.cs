using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using ValidationOptional;

/// <summary>
/// Optional fields in an application/x-www-form-urlencoded body reach the controller as
/// IOptional values: missing = null, sent = Optional.Create(value). Validation of those values is
/// covered by <see cref="ValidationOptionalYamlShould"/>.
/// </summary>
public class OptionalFormFieldsShould
{
	[Fact]
	public Task Bind_every_field_that_is_sent() =>
		PostForm(new()
		{
			["name"] = "abc",
			["code"] = "ABC",
			["count"] = "5",
			["note"] = "hello",
		}, (controller, request) =>
		{
			Assert.True(controller.ModelState.IsValid);
			Assert.Equal<IOptional<string>?>(Optional.Create("abc"), request.Name);
			Assert.Equal<IOptional<string>?>(Optional.Create("ABC"), request.Code);
			Assert.Equal<IOptional<int>?>(Optional.Create(5), request.Count);
			Assert.Equal<IOptional<string?>?>(Optional.Create<string?>("hello"), request.Note);
		});

	[Fact]
	public Task Bind_fields_that_are_not_sent_as_missing() =>
		PostForm(new(), (controller, request) =>
		{
			Assert.True(controller.ModelState.IsValid);
			Assert.Null(request.Name);
			Assert.Null(request.Code);
			Assert.Null(request.Count);
			Assert.Null(request.Note);
		});

	[Fact]
	public Task Distinguish_missing_fields_from_sent_ones() =>
		PostForm(new()
		{
			["name"] = "abc",
			["count"] = "5",
		}, (controller, request) =>
		{
			Assert.True(controller.ModelState.IsValid);
			Assert.Equal<IOptional<string>?>(Optional.Create("abc"), request.Name);
			Assert.Null(request.Code);
			Assert.Equal<IOptional<int>?>(Optional.Create(5), request.Count);
			Assert.Null(request.Note);
		});

	[Fact]
	public Task Bind_an_empty_nullable_field_as_present_and_null() =>
		PostForm(new()
		{
			["note"] = "",
		}, (controller, request) =>
		{
			Assert.True(controller.ModelState.IsValid);
			Assert.Equal<IOptional<string?>?>(Optional.Create<string?>(null), request.Note);
		});

	[Fact]
	public Task Report_a_value_that_cannot_be_parsed_as_a_model_error() =>
		PostForm(new()
		{
			["count"] = "abc",
		}, (controller, request) =>
		{
			Assert.False(controller.ModelState.IsValid);
			Assert.True(controller.ModelState.ContainsKey("count"));
			Assert.Null(request.Count);
		});

	private static Task PostForm(Dictionary<string, string> fields, Action<Microsoft.AspNetCore.Mvc.ControllerBase, OptionalValues> assertRequest) =>
		TestSingleRequest<FormControllerBase.PostFormActionResult, OptionalValues>(new(
			FormControllerBase.PostFormActionResult.NoContent(),
			client => client.PostAsync("/validation-optional/form", new FormUrlEncodedContent(fields))
		)
		{
			AssertRequest = assertRequest,
			AssertResponseMessage = VerifyResponse(204),
		});
}
