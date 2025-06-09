using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Yaml2JsonNode;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace DarkPatterns.OpenApiCodegen;

public static class OptionsLoader
{
	public static T LoadOptions<T>(this IEnumerable<JsonNode> nodes)
	{
		var resultNode = CombineJson(nodes);
		var result = resultNode.Deserialize<T>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		return result
			// TODO - generate diagnostic instead of throwing exception
			?? throw new InvalidOperationException("Could not build schema options");
	}

	public static List<JsonNode> AddYamlStream(this List<JsonNode> nodes, Stream stream)
	{
		nodes.Add(LoadYaml(stream));
		return nodes;
	}
	public static List<JsonNode> AddYamlFromString(this List<JsonNode> nodes, string yaml)
	{
		nodes.Add(LoadYaml(yaml));
		return nodes;
	}
	public static JsonNode LoadYaml(this Stream stream)
	{
		using var streamReader = new StreamReader(stream);
		return LoadYamlTextReader(streamReader);
	}
	public static JsonNode LoadYaml(this string yaml)
	{
		using var stringReader = new StringReader(yaml);
		return LoadYamlTextReader(stringReader);
	}
	public static JsonNode LoadYamlFromFile(string fileName)
	{
		using var streamReader = new StreamReader(fileName);
		return LoadYamlTextReader(streamReader);
	}
	public static JsonNode LoadYamlTextReader(TextReader textReader)
	{
		var yamlStream = new YamlStream();
		try
		{
			yamlStream.Load(textReader);
		}
		catch (YamlException)
		{
			// TODO - generate diagnostic instead of throwing exception
			throw;
		}

		// TODO: check $ top-level variables for vocabulary overrides
		return yamlStream.ToJsonNode().OfType<JsonNode>().First();
	}

	public static JsonNode CombineJson(IEnumerable<JsonNode> newValues)
	{
		var values = newValues.ToArray();
		var result = values.Last();
		switch (result)
		{
			case JsonObject:
				var index = values.Length - values.Reverse().TakeWhile(x => x is JsonObject).Count();
				return CombineJsonObjects(newValues.Skip(index).OfType<JsonObject>());
			default:
				return result.DeepClone();
		}
	}

	public static JsonObject CombineJsonObjects(IEnumerable<JsonObject> newValues)
	{
		var properties = from obj in newValues.Select((x, i) => new { x, i })
						 from e in obj.x
						 group new { e.Value, Index = obj.i } by e.Key into property
						 let resultValue = CombineJson(property.OrderBy(x => x.Index).Select(x => x.Value))
						 select new KeyValuePair<string, JsonNode>(property.Key, resultValue);
		return new JsonObject(properties);
	}
}
