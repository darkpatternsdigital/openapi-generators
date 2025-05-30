using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Any;

public class DataController : DataControllerBase
{
	protected override Task<GetDataActionResult> GetData()
	{
		throw new NotImplementedException();
	}

	protected override Task<PutDataActionResult> PutData(JsonNode putDataBody)
	{
		throw new NotImplementedException();
	}
}
