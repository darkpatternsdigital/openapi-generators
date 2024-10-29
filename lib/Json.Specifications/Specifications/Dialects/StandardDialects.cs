using System;
using System.Collections.Generic;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.Json.Specifications.Vocabularies;

namespace DarkPatterns.Json.Specifications.Dialects;

public static class StandardDialects
{
	public static readonly IJsonSchemaDialect CoreNext = new JsonSchemaDialect(
		new System.Uri("https://json-schema.org/draft/2020-12/schema"),
		"$id",
		[
			StandardVocabularies.Core202012,
			StandardVocabularies.Applicator202012,
			StandardVocabularies.Unevaluated202012,
			StandardVocabularies.Validation202012,
			StandardVocabularies.Metadata202012,
			StandardVocabularies.FormatAnnotation202012,
			StandardVocabularies.Content202012,
		],
		Keywords.UnknownKeyword.Instance
	);
	public static readonly DialectMatcher CoreNextMatcher = new DialectMatcher(
		(obj) => obj?["$schema"]?.GetValue<string>() == "https://json-schema.org/draft/2020-12/schema",
		CoreNext
	);

	// Draft-04
	public static readonly Uri Draft04Id = new Uri("http://json-schema.org/draft-04/schema#");
	public static readonly JsonSchemaVocabulary Draft04Vocabulary = new JsonSchemaVocabulary(
			// https://github.com/OAI/OpenAPI-Specification/blob/d4fdc6cae9043dfc1abcad3c1a55282c49b3a7eb/schemas/v3.0/schema.yaml#L203
			Draft04Id,
			[
				// ("id", null),
				// ("$schema", null),
				("title", Keywords.Draft2020_12Metadata.TitleKeyword.Instance),
				("description", Keywords.Draft2020_12Metadata.DescriptionKeyword.Instance),
				("default", Keywords.Draft2020_12Metadata.DefaultKeyword.Instance),
				// ("multipleOf", null),
				("maximum", Keywords.Draft2020_12Validation.MaximumKeyword.Instance),
				("minimum", Keywords.Draft2020_12Validation.MinimumKeyword.Instance),
				("exclusiveMaimum", Keywords.Draft04.ExclusiveMaximumKeyword.Instance),
				("exclusiveMinimum", Keywords.Draft04.ExclusiveMinimumKeyword.Instance),
				("maxLength", Keywords.Draft2020_12Validation.MaxLengthKeyword.Instance),
				("minLength", Keywords.Draft2020_12Validation.MinLengthKeyword.Instance),
				("pattern", Keywords.Draft2020_12Validation.PatternKeyword.Instance),
				// ("additionalItems", null),
				("items", ItemsKeyword.Instance),
				("maxItems", Keywords.Draft2020_12Validation.MaxItemsKeyword.Instance),
				("minItems", Keywords.Draft2020_12Validation.MinItemsKeyword.Instance),
				("maxProperties", Keywords.Draft2020_12Validation.MaxPropertiesKeyword.Instance),
				("minProperties", Keywords.Draft2020_12Validation.MinPropertiesKeyword.Instance),
				("required", Keywords.Draft2020_12Validation.RequiredKeyword.Instance),
				("additionalProperties", Keywords.Draft2020_12Applicator.AdditionalPropertiesKeyword.Instance),
				// ("definitions", null),
				("properties", Keywords.Draft2020_12Applicator.PropertiesKeyword.Instance),
				// ("patternProperties", null),
				// ("dependencies", null),
				("enum", Keywords.Draft2020_12Validation.EnumKeyword.Instance),
				//("type", TypeKeyword.Instance),
				("format", Keywords.Draft04.FormatKeyword.Instance),
				("not", Keywords.Draft2020_12Applicator.NotKeyword.Instance),
				("allOf", Keywords.Draft2020_12Applicator.AllOfKeyword.Instance),
				("oneOf", Keywords.Draft2020_12Applicator.OneOfKeyword.Instance),
				("anyOf", Keywords.Draft2020_12Applicator.AnyOfKeyword.Instance),

				//("$ref", RefKeyword.Instance),
			]
		);
	public static readonly IJsonSchemaDialect Draft04Dialect = new JsonSchemaDialect(
		Draft04Id,
		"$id",
		[
		//StandardVocabularies.Core202012,
		//StandardVocabularies.Applicator202012,
		//StandardVocabularies.Unevaluated202012,
		//StandardVocabularies.Validation202012,
		//StandardVocabularies.Metadata202012,
		//StandardVocabularies.FormatAnnotation202012,
		//StandardVocabularies.Content202012,
		],
		Keywords.UnknownKeyword.Instance
	);
	public static readonly DialectMatcher Draft04MatcherMatcher = new DialectMatcher(
		(obj) => obj?["$schema"]?.GetValue<string>() == Draft04Id.OriginalString,
		Draft04Dialect
	);


	public static readonly IReadOnlyList<DialectMatcher> StandardMatchers = [
		CoreNextMatcher
	];
}
