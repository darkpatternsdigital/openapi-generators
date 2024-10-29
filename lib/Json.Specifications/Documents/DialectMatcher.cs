using System;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.Json.Documents;

public record DialectMatcher(
	Predicate<JsonObject> MatchesDocument,
	IJsonSchemaDialect Dialect
);
