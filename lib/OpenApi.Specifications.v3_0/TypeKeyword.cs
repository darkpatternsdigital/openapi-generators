
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using static DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation.TypeAnnotation;

namespace DarkPatterns.OpenApi.Specifications.v3_0;

// This follows OpenApi 3.0 TypeKeyword, not the actual standard at https://json-schema.org/draft/2020-12/json-schema-validation#name-type

public class TypeKeyword(string keyword, IReadOnlyList<PrimitiveType> allowedTypes, PrimitiveType openApiType) : TypeAnnotation(keyword, allowedTypes)
{
	public new static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	public PrimitiveType OpenApiType => openApiType;

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		return ParseType(nodeInfo).Select(pt => ToTypeKeyword(pt));

		DiagnosableResult<PrimitiveType> ParseType(ResolvableNode nodeInfo)
		{
			// TODO: incorporate null
			if (nodeInfo.Node is JsonValue val && val.TryGetValue<string>(out var s) && TryParsePrimitiveType(s, out var pt))
				return DiagnosableResult<PrimitiveType>.Pass(pt);
			return DiagnosableResult<PrimitiveType>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));
		}

		IJsonSchemaAnnotation ToTypeKeyword(PrimitiveType allowedType)
		{
			return new TypeKeyword(keyword, [allowedType], allowedType);
		}
	}
}

public record TypeKeywordMismatch(string TypeValue, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [TypeValue];
	public static DiagnosticException.ToDiagnostic Builder(string TypeValue) => (Location) => new TypeKeywordMismatch(TypeValue, Location);
}
