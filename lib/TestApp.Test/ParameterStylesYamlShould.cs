using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using ParameterStyles;

public class ParameterStylesYamlShould
{
	private static readonly int[] OneTwo = [1, 2];

	[Fact]
	public Task Bind_query_arrays_with_style_form_and_explode() =>
		TestSingleRequest<QueryFormExplodeControllerBase.GetFormExplodeActionResult, int[]>(new(
			QueryFormExplodeControllerBase.GetFormExplodeActionResult.Ok(OneTwo),
			client => client.GetAsync("/parameter-styles/query/form-explode?ids=1&ids=2")
		)
		{
			AssertRequest = (controller, ids) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(OneTwo, ids);
			},
			AssertResponseMessage = VerifyResponse(200, OneTwo),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): explode: false is ignored; ?ids=1,2 fails model binding")]
	public Task Bind_query_arrays_with_style_form_without_explode() =>
		TestSingleRequest<QueryFormNoExplodeControllerBase.GetFormNoExplodeActionResult, int[]>(new(
			QueryFormNoExplodeControllerBase.GetFormNoExplodeActionResult.Ok(OneTwo),
			client => client.GetAsync("/parameter-styles/query/form-no-explode?ids=1,2")
		)
		{
			AssertRequest = (controller, ids) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(OneTwo, ids);
			},
			AssertResponseMessage = VerifyResponse(200, OneTwo),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): spaceDelimited is ignored; ?ids=1%202 fails model binding")]
	public Task Bind_query_arrays_with_style_space_delimited() =>
		TestSingleRequest<QuerySpaceDelimitedControllerBase.GetSpaceDelimitedActionResult, int[]>(new(
			QuerySpaceDelimitedControllerBase.GetSpaceDelimitedActionResult.Ok(OneTwo),
			client => client.GetAsync("/parameter-styles/query/space-delimited?ids=1%202")
		)
		{
			AssertRequest = (controller, ids) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(OneTwo, ids);
			},
			AssertResponseMessage = VerifyResponse(200, OneTwo),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): pipeDelimited is ignored; ?ids=1|2 fails model binding")]
	public Task Bind_query_arrays_with_style_pipe_delimited() =>
		TestSingleRequest<QueryPipeDelimitedControllerBase.GetPipeDelimitedActionResult, int[]>(new(
			QueryPipeDelimitedControllerBase.GetPipeDelimitedActionResult.Ok(OneTwo),
			client => client.GetAsync("/parameter-styles/query/pipe-delimited?ids=1%7C2")
		)
		{
			AssertRequest = (controller, ids) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(OneTwo, ids);
			},
			AssertResponseMessage = VerifyResponse(200, OneTwo),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): deepObject is ignored; filter[color]=red binds no properties")]
	public Task Bind_query_objects_with_style_deep_object() =>
		TestSingleRequest<QueryDeepObjectControllerBase.GetDeepObjectActionResult, object?>(new(
			QueryDeepObjectControllerBase.GetDeepObjectActionResult.Unsafe(new Microsoft.AspNetCore.Mvc.NoContentResult()),
			client => client.GetAsync("/parameter-styles/query/deep-object?filter%5Bcolor%5D=red&filter%5Bsize%5D=2")
		)
		{
			AssertRequest = (controller, filter) =>
			{
				Assert.True(controller.ModelState.IsValid);
				AssertFilter(filter);
			},
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): an exploded form object binds only under its name, so ?color=red binds no properties")]
	public Task Bind_query_objects_with_style_form_and_explode() =>
		TestSingleRequest<QueryFormObjectControllerBase.GetFormObjectActionResult, object?>(new(
			QueryFormObjectControllerBase.GetFormObjectActionResult.Unsafe(new Microsoft.AspNetCore.Mvc.NoContentResult()),
			client => client.GetAsync("/parameter-styles/query/form-object?color=red&size=2")
		)
		{
			AssertRequest = (controller, filter) =>
			{
				Assert.True(controller.ModelState.IsValid);
				AssertFilter(filter);
			},
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): a parameter with content is typed object instead of its schema")]
	public Task Bind_query_parameters_with_content() =>
		TestSingleRequest<QueryContentControllerBase.GetContentParameterActionResult, object?>(new(
			QueryContentControllerBase.GetContentParameterActionResult.Unsafe(new Microsoft.AspNetCore.Mvc.NoContentResult()),
			client => client.GetAsync("/parameter-styles/query/content?filter=" + System.Uri.EscapeDataString("""{"color":"red","size":2}"""))
		)
		{
			AssertRequest = (controller, filter) =>
			{
				Assert.True(controller.ModelState.IsValid);
				AssertFilter(filter);
			},
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): allowEmptyValue is ignored; ?search= fails [Required]")]
	public Task Accept_empty_values_when_allowed() =>
		TestSingleRequest<QueryAllowEmptyValueControllerBase.GetAllowEmptyValueActionResult, string>(new(
			QueryAllowEmptyValueControllerBase.GetAllowEmptyValueActionResult.Ok(""),
			client => client.GetAsync("/parameter-styles/query/allow-empty-value?search=")
		)
		{
			AssertRequest = (controller, search) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("", search);
			},
			AssertResponseMessage = VerifyResponse(200, ""),
		});

	[Fact]
	public Task Accept_reserved_characters_when_allowed() =>
		TestSingleRequest<QueryAllowReservedControllerBase.GetAllowReservedActionResult, string>(new(
			QueryAllowReservedControllerBase.GetAllowReservedActionResult.Ok("a/b?c"),
			client => client.GetAsync("/parameter-styles/query/allow-reserved?path=a/b?c")
		)
		{
			AssertRequest = (controller, path) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("a/b?c", path);
			},
			AssertResponseMessage = VerifyResponse(200, "a/b?c"),
		});

	[Fact]
	public Task Bind_header_arrays_with_style_simple() =>
		TestSingleRequest<HeaderSimpleControllerBase.GetHeaderArrayActionResult, int[]>(new(
			HeaderSimpleControllerBase.GetHeaderArrayActionResult.Ok(OneTwo),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/parameter-styles/header/simple")
			{
				Headers = { { "X-Ids", "1,2" } },
			})
		)
		{
			AssertRequest = (controller, ids) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(OneTwo, ids);
			},
			AssertResponseMessage = VerifyResponse(200, OneTwo),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): a simple-style path array /1,2 fails model binding")]
	public Task Bind_path_arrays_with_style_simple() =>
		TestSingleRequest<PathSimpleIdsControllerBase.GetPathSimpleArrayActionResult, int[]>(new(
			PathSimpleIdsControllerBase.GetPathSimpleArrayActionResult.Ok(OneTwo),
			client => client.GetAsync("/parameter-styles/path/simple/1,2")
		)
		{
			AssertRequest = (controller, ids) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(OneTwo, ids);
			},
			AssertResponseMessage = VerifyResponse(200, OneTwo),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): label style is ignored; /.5 fails model binding")]
	public Task Bind_path_parameters_with_style_label() =>
		TestSingleRequest<PathLabelIdControllerBase.GetPathLabelActionResult, int>(new(
			PathLabelIdControllerBase.GetPathLabelActionResult.Ok(5),
			client => client.GetAsync("/parameter-styles/path/label/.5")
		)
		{
			AssertRequest = (controller, id) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(5, id);
			},
			AssertResponseMessage = VerifyResponse(200, 5),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Parameter style): matrix style is ignored; /;id=5 fails model binding")]
	public Task Bind_path_parameters_with_style_matrix() =>
		TestSingleRequest<PathMatrixIdControllerBase.GetPathMatrixActionResult, int>(new(
			PathMatrixIdControllerBase.GetPathMatrixActionResult.Ok(5),
			client => client.GetAsync("/parameter-styles/path/matrix/;id=5")
		)
		{
			AssertRequest = (controller, id) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(5, id);
			},
			AssertResponseMessage = VerifyResponse(200, 5),
		});

	private static void AssertFilter(object? filter)
	{
		var actual = Assert.IsType<Filter>(filter);
		Assert.Equal("red", actual.Color?.Value);
		Assert.Equal(2, actual.Size?.Value);
	}
}
