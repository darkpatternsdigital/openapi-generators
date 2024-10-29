using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications.Dialects;
using DarkPatterns.OpenApi.Specifications.v3_0;

namespace DarkPatterns.OpenApi.Transformations;

public static class OpenApiTransforms
{
	public static readonly IReadOnlyList<DialectMatcher> Matchers =
		[
			OpenApi3_0DocumentFactory.JsonSchemaDialectMatcher,
			.. StandardDialects.StandardMatchers
		];
}
