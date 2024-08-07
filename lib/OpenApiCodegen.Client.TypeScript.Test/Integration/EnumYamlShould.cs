﻿using DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils;
using static DarkPatterns.OpenApiCodegen.Client.TypeScript.Utils.GenerationUtilities;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Integration;

[Collection(CommonDirectoryFixture.CollectionName)]
public class EnumYamlShould
{
	private readonly CommonDirectoryFixture commonDirectory;

	public EnumYamlShould(CommonDirectoryFixture commonDirectory)
	{
		this.commonDirectory = commonDirectory;
	}

	[Fact]
	public async Task Be_able_to_generate_the_request()
	{
		var body = new { player1 = "rock", player2 = "paper" };

		var result = await commonDirectory.ConvertRequest("enum.yaml", "playRockPaperScissors", new { }, body);

		AssertRequestSuccess(result, "POST", "/rock-paper-scissors", body);
	}

	[Fact]
	public async Task Be_able_to_catch_type_errors()
	{
		var body = new { player1 = "rock", player2 = "spock" };

		var result = await commonDirectory.ConvertRequest("enum.yaml", "playRockPaperScissors", new { }, body);

		Assert.NotEqual(0, result.ExitCode);
	}

	[Fact]
	public async Task Transform_responses()
	{
		var statusCode = 200;
		var responseBody = "player1";

		var result = await commonDirectory.ConvertResponse("enum.yaml", "playRockPaperScissors", statusCode, responseBody);
		AssertResponseSuccess(result, statusCode, responseBody);
	}

	[Fact]
	public async Task Convert_unexpected_responses_to_Other()
	{
		var result = await commonDirectory.ConvertResponse("enum.yaml", "playRockPaperScissors", 503);
		AssertResponseOtherStatusCode(result);
	}
}
