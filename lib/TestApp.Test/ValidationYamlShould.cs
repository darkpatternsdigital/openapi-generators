using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using Validation;

public class ValidationYamlShould
{
	private static Func<HttpClient, Task<HttpResponseMessage>> Post(string path, string json) =>
		client => client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));

	private static Task TestNumbers(string query, bool valid) =>
		TestSingleRequest<NumbersControllerBase.GetNumbersActionResult>(new(
			NumbersControllerBase.GetNumbersActionResult.NoContent(),
			client => client.GetAsync("/validation/numbers?" + query)
		)
		{
			AssertRequest = (controller, _) => Assert.Equal(valid, controller.ModelState.IsValid),
		});

	private static Task TestBody<T>(T response, string path, string json, bool valid) =>
		TestSingleRequest<T>(new(response, Post(path, json))
		{
			AssertRequest = (controller, _) => Assert.Equal(valid, controller.ModelState.IsValid),
		});

	[Theory]
	[InlineData("multipleOfFive=10")]
	[InlineData("atLeastOne=1")]
	[InlineData("atMostTen=10")]
	[InlineData("exclusive=1")]
	[InlineData("exclusive=9")]
	public Task Accept_numbers_that_satisfy_their_constraints(string query) => TestNumbers(query, valid: true);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): multipleOf is not enforced on parameters")]
	public Task Reject_numbers_that_are_not_a_multiple() => TestNumbers("multipleOfFive=7", valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): minimum without maximum emits no [Range] on parameters")]
	public Task Reject_numbers_below_a_minimum_without_a_maximum() => TestNumbers("atLeastOne=0", valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): maximum without minimum emits no [Range] on parameters")]
	public Task Reject_numbers_above_a_maximum_without_a_minimum() => TestNumbers("atMostTen=11", valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): exclusiveMinimum (OpenAPI 3.0 boolean) is ignored; [Range] is inclusive")]
	public Task Reject_numbers_equal_to_an_exclusive_minimum() => TestNumbers("exclusive=0", valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): exclusiveMaximum (OpenAPI 3.0 boolean) is ignored; [Range] is inclusive")]
	public Task Reject_numbers_equal_to_an_exclusive_maximum() => TestNumbers("exclusive=10", valid: false);

	[Fact]
	public Task Accept_arrays_that_satisfy_their_constraints() =>
		TestBody(ArraysControllerBase.PostArrayActionResult.NoContent(), "/validation/arrays", """["a","b"]""", valid: true);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): minItems is not enforced on request bodies")]
	public Task Reject_arrays_with_fewer_than_min_items() =>
		TestBody(ArraysControllerBase.PostArrayActionResult.NoContent(), "/validation/arrays", "[]", valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): maxItems is not enforced on request bodies")]
	public Task Reject_arrays_with_more_than_max_items() =>
		TestBody(ArraysControllerBase.PostArrayActionResult.NoContent(), "/validation/arrays", """["a","b","c","d"]""", valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): uniqueItems is not enforced")]
	public Task Reject_arrays_with_duplicate_unique_items() =>
		TestBody(ArraysControllerBase.PostArrayActionResult.NoContent(), "/validation/arrays", """["a","a"]""", valid: false);

	[Fact]
	public Task Accept_objects_that_satisfy_their_constraints() =>
		TestBody(ObjectsControllerBase.PostObjectActionResult.NoContent(), "/validation/objects", """{"a":"1"}""", valid: true);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): minProperties is not enforced")]
	public Task Reject_objects_with_fewer_than_min_properties() =>
		TestBody(ObjectsControllerBase.PostObjectActionResult.NoContent(), "/validation/objects", "{}", valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): maxProperties is not enforced")]
	public Task Reject_objects_with_more_than_max_properties() =>
		TestBody(ObjectsControllerBase.PostObjectActionResult.NoContent(), "/validation/objects", """{"a":"1","b":"2","c":"3"}""", valid: false);

	[Fact]
	public Task Accept_properties_that_satisfy_their_constraints() =>
		TestBody(PropertiesControllerBase.PostPropertiesActionResult.NoContent(), "/validation/properties",
			"""{"multipleOfFive":5,"atLeastOne":1,"atMostTen":10,"tags":["a"]}""", valid: true);

	private static Task TestInvalidProperties(string json) =>
		TestBody(PropertiesControllerBase.PostPropertiesActionResult.Unsafe(new BadRequestResult()), "/validation/properties", json, valid: false);

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): multipleOf is not enforced on model properties")]
	public Task Reject_properties_that_are_not_a_multiple() =>
		TestInvalidProperties("""{"multipleOfFive":7,"atLeastOne":1,"atMostTen":10,"tags":["a"]}""");

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): minimum without maximum emits no [Range] on model properties")]
	public Task Reject_properties_below_a_minimum_without_a_maximum() =>
		TestInvalidProperties("""{"multipleOfFive":5,"atLeastOne":0,"atMostTen":10,"tags":["a"]}""");

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): maximum without minimum emits no [Range] on model properties")]
	public Task Reject_properties_above_a_maximum_without_a_minimum() =>
		TestInvalidProperties("""{"multipleOfFive":5,"atLeastOne":1,"atMostTen":11,"tags":["a"]}""");

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): minItems is not enforced on model properties")]
	public Task Reject_array_properties_with_fewer_than_min_items() =>
		TestInvalidProperties("""{"multipleOfFive":5,"atLeastOne":1,"atMostTen":10,"tags":[]}""");

	[Fact(Skip = "OpenAPI 3.0 gap (Validation): maxItems is not enforced on model properties")]
	public Task Reject_array_properties_with_more_than_max_items() =>
		TestInvalidProperties("""{"multipleOfFive":5,"atLeastOne":1,"atMostTen":10,"tags":["a","b","c"]}""");
}
