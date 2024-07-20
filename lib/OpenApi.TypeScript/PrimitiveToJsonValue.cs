using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApi.TypeScript;

public static class PrimitiveToJsonValue
{
	public static string GetPrimitiveValue(JsonNode? any)
	{
		return any?.ToJsonString() ?? "null";
	}
}
