using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications.Dialects;
using DarkPatterns.OpenApi.Specifications.v3_0;

namespace DarkPatterns.OpenApi.Transformations;

public static class OpenApiTransforms
{
	/*
	 * CSC : warning CS8785: Generator 'OpenApiCSharpGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'TypeInitializationException' with message 'The type initializer for 'DarkPatterns.OpenApi.Transformations.OpenApiTransforms' threw an exception.'.
	 * */
	public static readonly IReadOnlyList<DialectMatcher> Matchers =
		[
			OpenApi3_0DocumentFactory.JsonSchemaDialectMatcher,
			.. StandardDialects.StandardMatchers
		];
}
