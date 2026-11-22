using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using GetInfoActionResult = Headers.InfoControllerBase.GetInfoActionResult;

/// <summary>
/// Generated action results expose their headers and underlying result, so tests don't need reflection.
/// </summary>
public class ActionResultAccessShould
{
	[Fact]
	public void Expose_the_response_headers()
	{
		var target = GetInfoActionResult.Ok(new() { ["foo"] = "bar" }, "some-header-data");

		Assert.Equal([new KeyValuePair<string, string>("X-Data", "some-header-data")], target.Headers);
	}

	[Fact]
	public void Expose_the_underlying_object_result()
	{
		var body = new Dictionary<string, string> { ["foo"] = "bar" };
		var target = GetInfoActionResult.Ok(body, "some-header-data");

		var result = Assert.IsType<ObjectResult>(target.UnderlyingResult);
		Assert.Equal(200, result.StatusCode);
		Assert.Same(body, result.Value);
	}

	[Fact]
	public void Expose_the_result_passed_to_unsafe()
	{
		var original = new BadRequestResult();
		var target = GetInfoActionResult.Unsafe(original);

		Assert.Same(original, target.UnderlyingResult);
		Assert.Empty(target.Headers);
	}

	[Fact]
	public void Handle_a_default_value()
	{
		var target = default(GetInfoActionResult);

		Assert.Null(target.UnderlyingResult);
		Assert.Empty(target.Headers);
	}
}
