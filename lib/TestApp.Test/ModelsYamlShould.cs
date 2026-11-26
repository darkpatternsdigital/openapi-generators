using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.TestUtils;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using Models;

/// <summary>
/// Most operations in oas-server-models.yml echo their request body, so these tests check that values round-trip.
/// </summary>
public class ModelsYamlShould
{
	private static Func<HttpClient, Task<HttpResponseMessage>> Post(string path, string json) =>
		client => client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));

	private static Func<HttpResponseMessage, Task> VerifyJson(string expectedJson) =>
		async response =>
		{
			Assert.Equal(200, (int)response.StatusCode);
			Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
			var actual = await response.Content.ReadAsStringAsync();
			Assert.True(JsonCompare.CompareJsonStrings(actual, expectedJson), $"Expected {expectedJson} but received {actual}");
		};

	private const string FormatsJson = """
		{
			"float": 1.5,
			"double": 2.25,
			"int64": 9007199254740993,
			"date": "2024-01-02",
			"dateTime": "2024-01-02T03:04:05+00:00",
			"uuid": "0f8fad5b-d9cb-469f-a165-70867728950e",
			"byte": "aGVsbG8="
		}
		""";

	[Fact]
	public Task Round_trip_string_and_number_formats() =>
		TestSingleRequest<FormatsControllerBase.EchoFormatsActionResult, Formats?>(new(
			FormatsControllerBase.EchoFormatsActionResult.Unsafe(new NoContentResult()),
			Post("/models/formats", FormatsJson)
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(new DateOnly(2024, 1, 2), body?.Date);
				Assert.Equal(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"), body?.Uuid);
				Assert.Equal(9007199254740993L, body?.Int64);
				Assert.Equal("hello", Encoding.UTF8.GetString(body?.Byte ?? []));
			},
			AssertResponseMessage = VerifyJson(FormatsJson),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Models): a required readOnly property is [JsonRequired] in requests, so ModelState is invalid")]
	public Task Not_require_read_only_properties_in_requests() =>
		TestSingleRequest<ReadWriteControllerBase.EchoReadWriteActionResult, ReadWrite?>(new(
			ReadWriteControllerBase.EchoReadWriteActionResult.Unsafe(new NoContentResult()),
			Post("/models/read-write", """{"name":"Fido","password":"secret"}""")
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("Fido", body?.Name);
				Assert.Equal("secret", body?.Password);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Models): writeOnly properties are serialized in responses")]
	public Task Omit_write_only_properties_from_responses() =>
		TestSingleRequest<ReadWriteControllerBase.EchoReadWriteActionResult, ReadWrite?>(new(
			ReadWriteControllerBase.EchoReadWriteActionResult.Ok(new ReadWrite("1", "Fido", "secret")),
			Post("/models/read-write", """{"id":"1","name":"Fido","password":"secret"}""")
		)
		{
			AssertResponseMessage = VerifyJson("""{"id":"1","name":"Fido"}"""),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Boolean schemas): additionalProperties: true drops unknown properties")]
	public Task Keep_additional_properties_when_they_are_allowed() =>
		TestSingleRequest<OpenObjectControllerBase.EchoOpenObjectActionResult, OpenObject?>(new(
			OpenObjectControllerBase.EchoOpenObjectActionResult.Unsafe(new NoContentResult()),
			Post("/models/open-object", """{"name":"Fido","extra":{"nested":true}}""")
		)
		{
			AssertRequest = (controller, body) => Assert.True(controller.ModelState.IsValid),
			AssertResponseMessage = VerifyJson("""{"name":"Fido","extra":{"nested":true}}"""),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Boolean schemas): additionalProperties: false accepts unknown properties")]
	public Task Reject_additional_properties_when_they_are_not_allowed() =>
		TestSingleRequest<ClosedObjectControllerBase.EchoClosedObjectActionResult, ClosedObject?>(new(
			ClosedObjectControllerBase.EchoClosedObjectActionResult.Unsafe(new NoContentResult()),
			Post("/models/closed-object", """{"name":"Fido","extra":1}""")
		)
		{
			AssertRequest = (controller, body) => Assert.False(controller.ModelState.IsValid),
		});

	[Fact]
	public Task Accept_objects_without_additional_properties_when_they_are_not_allowed() =>
		TestSingleRequest<ClosedObjectControllerBase.EchoClosedObjectActionResult, ClosedObject?>(new(
			ClosedObjectControllerBase.EchoClosedObjectActionResult.Unsafe(new NoContentResult()),
			Post("/models/closed-object", """{"name":"Fido"}""")
		)
		{
			AssertRequest = (controller, body) => Assert.True(controller.ModelState.IsValid),
			AssertResponseMessage = VerifyJson("""{"name":"Fido"}"""),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Models): additionalProperties alongside properties is dropped")]
	public Task Keep_typed_additional_properties_alongside_properties() =>
		TestSingleRequest<TypedAdditionalPropertiesControllerBase.EchoTypedAdditionalPropertiesActionResult, TypedAdditionalProperties?>(new(
			TypedAdditionalPropertiesControllerBase.EchoTypedAdditionalPropertiesActionResult.Unsafe(new NoContentResult()),
			Post("/models/typed-additional-properties", """{"name":"Fido","age":3}""")
		)
		{
			AssertRequest = (controller, body) => Assert.True(controller.ModelState.IsValid),
			AssertResponseMessage = VerifyJson("""{"name":"Fido","age":3}"""),
		});

	[Fact]
	public Task Round_trip_arrays_of_arrays() =>
		TestSingleRequest<MatrixControllerBase.EchoMatrixActionResult, Matrix?>(new(
			MatrixControllerBase.EchoMatrixActionResult.Unsafe(new NoContentResult()),
			Post("/models/matrix", """{"rows":[[1,2],[3],[]]}""")
		)
		{
			AssertRequest = (controller, body) => Assert.True(controller.ModelState.IsValid),
			AssertResponseMessage = VerifyJson("""{"rows":[[1,2],[3],[]]}"""),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Models): default values are not applied to missing properties")]
	public Task Apply_default_values_to_missing_properties() =>
		TestSingleRequest<DefaultsControllerBase.EchoDefaultsActionResult, Defaults?>(new(
			DefaultsControllerBase.EchoDefaultsActionResult.Unsafe(new NoContentResult()),
			Post("/models/defaults", "{}")
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(10, body?.Count?.Value);
				Assert.Equal("none", body?.Label?.Value);
			},
		});

	[Fact]
	public Task Round_trip_recursive_models() =>
		TestSingleRequest<TreeControllerBase.EchoTreeActionResult, TreeNode?>(new(
			TreeControllerBase.EchoTreeActionResult.Unsafe(new NoContentResult()),
			Post("/models/tree", """{"name":"root","children":[{"name":"a","children":[{"name":"a1"}]},{"name":"b"}]}""")
		)
		{
			AssertRequest = (controller, body) => Assert.True(controller.ModelState.IsValid),
			AssertResponseMessage = VerifyJson("""{"name":"root","children":[{"name":"a","children":[{"name":"a1"}]},{"name":"b"}]}"""),
		});

	[Fact]
	public Task Accept_integer_enum_values() =>
		TestSingleRequest<IntegerEnum.PriorityControllerBase.SetPriorityActionResult, int>(new(
			IntegerEnum.PriorityControllerBase.SetPriorityActionResult.NoContent(),
			Post("/integer-enum/priority", "2")
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(2, body);
			},
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Non-string enums): an integer enum is generated as int, so values outside the enum are accepted")]
	public Task Reject_integers_outside_the_enum() =>
		TestSingleRequest<IntegerEnum.PriorityControllerBase.SetPriorityActionResult, int>(new(
			IntegerEnum.PriorityControllerBase.SetPriorityActionResult.Unsafe(new BadRequestResult()),
			Post("/integer-enum/priority", "7")
		)
		{
			AssertRequest = (controller, body) => Assert.False(controller.ModelState.IsValid),
			AssertResponseMessage = VerifyResponse(400),
		});
}
