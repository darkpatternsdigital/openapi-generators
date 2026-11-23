using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;

/// <summary>
/// A required parameter or form field with a value type (boolean, number) is reported when it's
/// missing, rather than bound as its default (false, 0): ASP.NET Core validates [Required] on a
/// top-level parameter that model binding couldn't set. Regression tests for planning #28.
/// </summary>
public class RequiredValueTypesShould
{
	[Fact]
	public Task Report_a_missing_required_boolean_form_field() =>
		TestSingleRequest<Form.BasicControllerBase.PostBasicFormActionResult, (string Name, IOptional<string> Tag, bool HasIdTag)>(new(
			Form.BasicControllerBase.PostBasicFormActionResult.Ok(17),
			client => client.PostAsync("/form/basic", new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["name"] = "Fido",
			}))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.False(controller.ModelState.IsValid);
				Assert.True(controller.ModelState.ContainsKey("hasIdTag"));
			},
			AssertResponseMessage = VerifyResponse(200, 17),
		});

	[Fact]
	public Task Report_missing_required_value_type_query_parameters() =>
		TestSingleRequest<ParameterFormats.FormatsNameControllerBase.GetFormatsActionResult, (string Name, DateTimeOffset When, double Amount, bool Flag, ParameterFormats.Size Size, byte[] Data, ParameterFormats.Size XSize)>(new(
			ParameterFormats.FormatsNameControllerBase.GetFormatsActionResult.NoContent(),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/formats/n?when=2026-11-22T16%3A45%3A00Z&size=small&data=AQID")
			{
				Headers = { { "X-Size", "large" } },
			})
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.False(controller.ModelState.IsValid);
				Assert.True(controller.ModelState.ContainsKey("amount"));
				Assert.True(controller.ModelState.ContainsKey("flag"));
			},
			AssertResponseMessage = VerifyResponse(204),
		});
}
