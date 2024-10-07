
using System.Collections.Generic;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Specifications.OpenApi3_0;

/// OpenAPI 3.0 `items` schema
public static class ItemsKeyword
{
	// `items` does not allow an array in OpenAPI 3.0, but does in Draft 04 and newer. Can still use the same keyword instance.
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		var schemaResult = JsonSchemaParser.Deserialize(nodeInfo, options);
		return schemaResult.Select<IJsonSchemaAnnotation>(schema => new Json.Specifications.Keywords.Draft2020_12Applicator.ItemsKeyword(keyword, schema));
	}
}
