using DarkPatterns.OpenApiCodegen.Json.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using ValidationOptional;

public class ValidationOptionalYamlShould
{
	public static TheoryData<bool, string?, object?> Cases => new()
	{
		{ true, null, null },
		{ false, "name", "a" },
		{ true, "name", "abc" },
		{ false, "name", "abcde" },
		{ false, "code", "abc" },
		{ true, "code", "ABC" },
		{ false, "count", 0 },
		{ true, "count", 5 },
		{ false, "count", 11 },
	};

	[Theory]
	[MemberData(nameof(Cases))]
	public Task Validate_optional_json_properties(bool expectedIsValid, string? propertyName, object? value) =>
		TestSingleRequest<JsonControllerBase.PostJsonActionResult, OptionalValues>(new(
			JsonControllerBase.PostJsonActionResult.NoContent(),
			client => client.PostAsync("/validation-optional/json", JsonContent.Create(ToDictionary(propertyName, value)))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.Equal(expectedIsValid, controller.ModelState.IsValid);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Theory]
	[MemberData(nameof(Cases))]
	public Task Validate_optional_form_fields(bool expectedIsValid, string? propertyName, object? value) =>
		TestSingleRequest<FormControllerBase.PostFormActionResult, OptionalValues>(new(
			FormControllerBase.PostFormActionResult.NoContent(),
			client => client.PostAsync("/validation-optional/form", new FormUrlEncodedContent(
				ToDictionary(propertyName, value).ToDictionary(e => e.Key, e => e.Value.ToString())
			))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.Equal(expectedIsValid, controller.ModelState.IsValid);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Distinguish_missing_form_fields_from_present_ones() =>
		TestSingleRequest<FormControllerBase.PostFormActionResult, OptionalValues>(new(
			FormControllerBase.PostFormActionResult.NoContent(),
			client => client.PostAsync("/validation-optional/form", new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["name"] = "abc",
				["count"] = "5",
			}))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal<IOptional<string>?>(Optional.Create("abc"), request.Name);
				Assert.Null(request.Code);
				Assert.Equal<IOptional<int>?>(Optional.Create(5), request.Count);
				Assert.Null(request.Note);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Treat_an_empty_nullable_form_field_as_present_and_null() =>
		TestSingleRequest<FormControllerBase.PostFormActionResult, OptionalValues>(new(
			FormControllerBase.PostFormActionResult.NoContent(),
			client => client.PostAsync("/validation-optional/form", new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["note"] = "",
			}))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal<IOptional<string?>?>(Optional.Create<string?>(null), request.Note);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	private static Dictionary<string, object> ToDictionary(string? propertyName, object? value) =>
		propertyName == null || value == null ? new() : new() { [propertyName] = value };
}
