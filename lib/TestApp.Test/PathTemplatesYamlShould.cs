using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using PathTemplates;

public class PathTemplatesYamlShould
{
	[Fact]
	public Task Bind_several_path_parameters() =>
		TestSingleRequest<OwnersOwnerIdPetsPetIdControllerBase.GetOwnerPetActionResult, (int OwnerId, string PetId)>(new(
			OwnersOwnerIdPetsPetIdControllerBase.GetOwnerPetActionResult.Ok("Fido"),
			client => client.GetAsync("/paths/owners/12/pets/fido")
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal((12, "fido"), request);
			},
			AssertResponseMessage = VerifyResponse(200, "Fido"),
		});

	[Fact]
	public Task Bind_several_path_parameters_in_one_segment() =>
		TestSingleRequest<FilesNameExtControllerBase.GetFileActionResult, (string Name, string Ext)>(new(
			FilesNameExtControllerBase.GetFileActionResult.Ok("report.pdf"),
			client => client.GetAsync("/paths/files/report.pdf")
		)
		{
			AssertRequest = (controller, request) => Assert.Equal(("report", "pdf"), request),
			AssertResponseMessage = VerifyResponse(200, "report.pdf"),
		});

	[Fact]
	public Task Bind_parameters_defined_in_components() =>
		TestSingleRequest<SharedParametersControllerBase.GetWithSharedParametersActionResult, (int Limit, string? TraceId)>(new(
			SharedParametersControllerBase.GetWithSharedParametersActionResult.Ok(5),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/paths/shared-parameters?limit=5")
			{
				Headers = { { "X-Trace-Id", "trace-1" } },
			})
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal((5, "trace-1"), request);
			},
			AssertResponseMessage = VerifyResponse(200, 5),
		});

	[Fact]
	public Task Reject_a_missing_required_parameter_defined_in_components() =>
		TestSingleRequest<SharedParametersControllerBase.GetWithSharedParametersActionResult, (int Limit, string? TraceId)>(new(
			SharedParametersControllerBase.GetWithSharedParametersActionResult.Unsafe(new Microsoft.AspNetCore.Mvc.BadRequestResult()),
			client => client.GetAsync("/paths/shared-parameters")
		)
		{
			AssertRequest = (controller, request) => Assert.False(controller.ModelState.IsValid),
			AssertResponseMessage = VerifyResponse(400),
		});

	[Fact]
	public Task Route_operations_without_an_operation_id() =>
		TestSingleRequest<NoOperationIdIdControllerBase.GetPathsNoOperationIdIdActionResult, string>(new(
			NoOperationIdIdControllerBase.GetPathsNoOperationIdIdActionResult.Ok("x"),
			client => client.GetAsync("/paths/no-operation-id/x")
		)
		{
			AssertRequest = (controller, id) => Assert.Equal("x", id),
			AssertResponseMessage = VerifyResponse(200, "x"),
		});

	[Fact]
	public Task Route_operations_with_a_body_and_without_an_operation_id() =>
		TestSingleRequest<NoOperationId.WidgetsControllerBase.PostNoOperationIdWidgetsActionResult, string>(new(
			NoOperationId.WidgetsControllerBase.PostNoOperationIdWidgetsActionResult.NoContent(),
			client => client.PostAsync("/no-operation-id/widgets", JsonContent.Create("sprocket"))
		)
		{
			AssertRequest = (controller, body) => Assert.Equal("sprocket", body),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact(Skip = "OpenAPI 3.0 gap (Request bodies): without an operationId, an operation with a request body generates a method named _")]
	public void Name_operations_with_a_body_and_without_an_operation_id_from_the_method_and_path()
	{
		// The ActionResult is named from the method and path; the abstract method should be, too
		var method = typeof(NoOperationId.WidgetsControllerBase).GetMethod("PostNoOperationIdWidgets", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
	}
}
