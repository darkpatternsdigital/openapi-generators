using System.Linq;
using System.Text.Json.Nodes;
using Json.More;

namespace DarkPatterns.OpenApiCodegen.TestUtils;

public static class JsonCompare
{
	public static bool CompareJsonStrings(string left, string right)
	{
		var leftNode = JsonNode.Parse(left);
		var rightNode = JsonNode.Parse(right);
		return leftNode.IsEquivalentTo(rightNode);
	}

	public static bool CompareViaJson<T1, T2>(T1 left, T2? right)
	{
		var leftNode = System.Text.Json.JsonSerializer.SerializeToNode(left);
		var rightNode = System.Text.Json.JsonSerializer.SerializeToNode(right);
		return leftNode.IsEquivalentTo(rightNode);
	}

	public static bool CompareJson<T>(JsonNode? actualNode, T? expected)
	{
		var expectedNode = System.Text.Json.JsonSerializer.SerializeToNode(expected);
		return actualNode.IsEquivalentTo(expectedNode);
	}

	public static bool CompareJson<T>(string actualJson, T? expected)
	{
		return CompareJson(JsonNode.Parse(actualJson), expected);
	}
}