using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

using static Utilities;

/// <summary>Enums are read and written by their values in the schema, not their C# names</summary>
public class EnumsShould
{
	[Fact]
	public async Task Bind_enum_query_parameters_by_their_schema_values()
	{
		var api = new Mock<Enum.IRockPaperScissorsQueryApi>();
		api.Setup(a => a.PlayRockPaperScissorsQuery(Enum.Option.Rock, Enum.Option.Scissors))
			.ReturnsAsync(Enum.IRockPaperScissorsQueryApi.PlayRockPaperScissorsQueryResult.Ok(Enum.PlayRockPaperScissorsQueryResponse.Player1));

		await SendAsync(api, client => client.GetAsync("/enum/rock-paper-scissors-query?player1=rock&player2=scissors"),
			VerifyResponse(200, "application/json", "\"player1\""));
	}

	[Fact]
	public async Task Bind_enum_values_that_are_not_valid_identifiers()
	{
		var api = new Mock<Enum.IDifficultEnumApi>();
		api.Setup(a => a.DifficultQueryStringEnum(Enum.DifficultQueryStringEnumEnum.PlayerOne))
			.ReturnsAsync(Enum.IDifficultEnumApi.DifficultQueryStringEnumResult.Ok("ok"));

		await SendAsync(api, client => client.GetAsync("/enum/difficult-enum?enum=player%20one"),
			VerifyResponse(200));
	}

	[Theory]
	// The C# name and a number aren't schema values
	[InlineData("Rock")]
	[InlineData("0")]
	[InlineData("lizard")]
	public async Task Reject_values_that_are_not_in_the_enum(string value)
	{
		var api = new Mock<Enum.IRockPaperScissorsQueryApi>(MockBehavior.Strict);

		await SendAsync(api, client => client.GetAsync($"/enum/rock-paper-scissors-query?player1={value}&player2=rock"),
			VerifyResponse(400));
	}

	[Fact]
	public async Task Bind_enums_in_JSON_bodies_by_their_schema_values()
	{
		var api = new Mock<Enum.IRockPaperScissorsApi>();
		api.Setup(a => a.PlayRockPaperScissors(new(Enum.Option.Paper, Enum.Option.Rock)))
			.ReturnsAsync(Enum.IRockPaperScissorsApi.PlayRockPaperScissorsResult.Ok(Enum.PlayRockPaperScissorsResponse.Player1));

		await SendAsync(api, client => client.PostAsync("/enum/rock-paper-scissors", new StringContent("""{ "player1": "paper", "player2": "rock" }""", Encoding.UTF8, "application/json")),
			VerifyResponse(200, "application/json", "\"player1\""));
	}
}
