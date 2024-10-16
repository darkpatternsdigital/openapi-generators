using static DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.ControllerExtensions.InformationControllerBase;
using static DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Enum.RockPaperScissorsQueryControllerBase;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Enum
{
	public class RockPaperScissorsController : RockPaperScissorsControllerBase
	{
		protected override Task<PlayRockPaperScissorsActionResult> PlayRockPaperScissors(PlayRockPaperScissorsRequest playRockPaperScissorsBody)
		{
			this.DelegateRequest(playRockPaperScissorsBody);
			return this.DelegateResponse<PlayRockPaperScissorsActionResult>();
		}
	}

	public class RockPaperScissorsQueryController : RockPaperScissorsQueryControllerBase
	{
		protected override Task<PlayRockPaperScissorsQueryActionResult> PlayRockPaperScissorsQuery(Option player1, Option player2)
		{
			this.DelegateRequest((player1, player2));
			return this.DelegateResponse<PlayRockPaperScissorsQueryActionResult>();
		}
	}

	public class DifficultEnumController : DifficultEnumControllerBase
	{
		protected override Task<DifficultQueryStringEnumActionResult> DifficultQueryStringEnum(DifficultQueryStringEnumEnum _enum)
		{
			this.DelegateRequest(_enum);
			return this.DelegateResponse<DifficultQueryStringEnumActionResult>();
		}
	}
}
