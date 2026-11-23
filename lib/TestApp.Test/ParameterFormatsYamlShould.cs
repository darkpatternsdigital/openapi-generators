using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DarkPatterns.OpenApiCodegen.Json.Extensions;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using ParameterFormats;

/// <summary>
/// Requests built by the generated C# client reach the generated server with the values intact,
/// even when the client runs in a culture that formats dates and numbers differently.
/// </summary>
public class ParameterFormatsYamlShould
{
	private static readonly DateTimeOffset When = new(2026, 11, 22, 16, 45, 0, TimeSpan.FromHours(-6));

	[Fact]
	public Task Round_trip_parameter_values()
	{
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		return TestSingleRequest<FormatsNameControllerBase.GetFormatsActionResult, (string Name, DateTimeOffset When, double Amount, bool Flag, Size Size, byte[] Data, Size XSize)>(new(
			FormatsNameControllerBase.GetFormatsActionResult.NoContent(),
			client => client.SendAsync(Operations.GetFormats(name: "a b", when: When, amount: 1.5, flag: true, size: Size.Small, data: [1, 2, 3], xSize: Size.Large))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal("a b", request.Name);
				Assert.Equal(When, request.When);
				Assert.Equal(1.5, request.Amount);
				Assert.True(request.Flag);
				Assert.Equal(Size.Small, request.Size);
				Assert.Equal([1, 2, 3], request.Data);
				Assert.Equal(Size.Large, request.XSize);
			},
			AssertResponseMessage = VerifyResponse(204),
		});
	}

	[Fact]
	public Task Round_trip_form_values()
	{
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		return TestSingleRequest<FormatsNameControllerBase.PostFormatsActionResult, FormatsForm>(new(
			FormatsNameControllerBase.PostFormatsActionResult.NoContent(),
			client => client.SendAsync(Operations.PostFormats(
				name: "n",
				size: Size.Large,
				when: Optional.Create(When),
				flag: Optional.Create(false),
				tags: Optional.Create<System.Collections.Generic.IEnumerable<string>>(["a", "b"]),
				note: Optional.Create<string?>(null)))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(Size.Large, request.Size);
				Assert.Equal<IOptional<DateTimeOffset>?>(Optional.Create(When), request.When);
				Assert.Equal<IOptional<bool>?>(Optional.Create(false), request.Flag);
				Assert.NotNull(request.Tags);
				Assert.Equal(["a", "b"], request.Tags.Value);
				Assert.Equal<IOptional<string?>?>(Optional.Create<string?>(null), request.Note);
			},
			AssertResponseMessage = VerifyResponse(204),
		});
	}

	[Fact]
	public Task Round_trip_missing_optional_form_fields() =>
		TestSingleRequest<FormatsNameControllerBase.PostFormatsActionResult, FormatsForm>(new(
			FormatsNameControllerBase.PostFormatsActionResult.NoContent(),
			client => client.SendAsync(Operations.PostFormats(name: "n", size: Size.Small, when: null, flag: null, tags: null, note: null))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(Size.Small, request.Size);
				Assert.Null(request.When);
				Assert.Null(request.Flag);
				Assert.Null(request.Tags);
				Assert.Null(request.Note);
			},
			AssertResponseMessage = VerifyResponse(204),
		});
}
