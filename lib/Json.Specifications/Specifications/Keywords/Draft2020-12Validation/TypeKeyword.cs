using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using static TypeAnnotation;

/// <see cref="https://json-schema.org/draft/2020-12/json-schema-validation#name-type">Draft 2020-12 type keyword</see>
public class TypeAnnotation(string keyword, IReadOnlyList<PrimitiveType> allowedTypes) : IJsonSchemaAnnotation
{
	public static readonly IJsonSchemaKeyword Instance = new JsonSchemaKeyword(Parse);

	private static DiagnosableResult<IJsonSchemaAnnotation> Parse(string keyword, ResolvableNode nodeInfo, JsonSchemaParserOptions options)
	{
		if (nodeInfo.Node is JsonArray arr)
		{
			var items = from i in Enumerable.Range(0, arr.Count)
						let node = nodeInfo.Navigate(i)
						select ParseType(nodeInfo);
			return items.AggregateAll().Select(expected => ToTypeKeyword([.. expected]));
		}
		return ParseType(nodeInfo).Select(pt => ToTypeKeyword([pt]));

		DiagnosableResult<PrimitiveType> ParseType(ResolvableNode nodeInfo)
		{
			if (nodeInfo.Node is JsonValue val && val.TryGetValue<string>(out var s) && TryParsePrimitiveType(s, out var pt))
				return DiagnosableResult<PrimitiveType>.Pass(pt);
			return DiagnosableResult<PrimitiveType>.Fail(new UnableToParseKeyword(keyword, options.Registry.ResolveLocation(nodeInfo)));
		}

		IJsonSchemaAnnotation ToTypeKeyword(IReadOnlyList<PrimitiveType> allowedTypes)
		{
			return new TypeAnnotation(keyword, allowedTypes);
		}
	}

	public string Keyword => keyword;

	public IReadOnlyList<PrimitiveType> AllowedTypes { get; } =
		allowedTypes.Contains(PrimitiveType.Number) && !allowedTypes.Contains(PrimitiveType.Integer)
			? [.. allowedTypes, PrimitiveType.Integer]
			: allowedTypes;

	public bool AllowsArray => AllowedTypes.Contains(PrimitiveType.Array);
	public bool AllowsObject => AllowedTypes.Contains(PrimitiveType.Object);
	public bool AllowsBoolean => AllowedTypes.Contains(PrimitiveType.Boolean);
	public bool AllowsString => AllowedTypes.Contains(PrimitiveType.String);
	public bool AllowsNumber => AllowedTypes.Contains(PrimitiveType.Number);
	public bool AllowsInteger => AllowedTypes.Contains(PrimitiveType.Integer);
	public bool AllowsNull => AllowedTypes.Contains(PrimitiveType.Null);

	public IEnumerable<JsonSchema> GetReferencedSchemas() => [];
	public IEnumerable<IJsonSchemaAnnotation> GetDynamicAnnotations()
		=> [];

	public IEnumerable<DiagnosticBase> Evaluate(ResolvableNode nodeMetadata, JsonSchemaInfo context, EvaluationContext evaluationContext)
	{
		var actualType = nodeMetadata.Node switch
		{
			JsonArray _ => PrimitiveType.Array,
			JsonObject _ => PrimitiveType.Object,
			JsonValue v when v.TryGetValue(out bool _) => PrimitiveType.Boolean,
			JsonValue v when v.TryGetValue(out string? s) && s is not null => PrimitiveType.String,
			JsonValue v when v.TryGetValue(out double d) =>
				0 == d % 1 ? PrimitiveType.Integer : PrimitiveType.Number,
			_ => PrimitiveType.Null,
		};

		if (AllowedTypes.Contains(actualType))
			return [];
		return [new TypeKeywordMismatch(actualType, AllowedTypes, evaluationContext.DocumentRegistry.ResolveLocation(nodeMetadata))];
	}

	public static class Common
	{
#pragma warning disable CA1720 // Identifier contains type name
		public const string Array = "array";
		public const string Object = "object";
		public const string Boolean = "boolean";
		public const string String = "string";
		public const string Number = "number";
		public const string Integer = "integer";
		public const string Null = "null";
#pragma warning restore CA1720 // Identifier contains type name
	}
	public enum PrimitiveType
	{
#pragma warning disable CA1720 // Identifier contains type name
		Array,
		Object,
		Boolean,
		String,
		Number,
		Integer,
		Null,
#pragma warning restore CA1720 // Identifier contains type name
	}

	public static string ToPrimitiveTypeString(PrimitiveType type)
	{
		return type switch
		{
			PrimitiveType.Array => Common.Array,
			PrimitiveType.Object => Common.Object,
			PrimitiveType.Boolean => Common.Boolean,
			PrimitiveType.String => Common.String,
			PrimitiveType.Number => Common.Number,
			PrimitiveType.Integer => Common.Integer,
			PrimitiveType.Null => Common.Null,
			_ => throw new ArgumentException("Primitive Type was not a known enum value", nameof(type)),
		};
	}

	public static bool TryParsePrimitiveType(string input, out PrimitiveType type)
	{
		switch (input)
		{
			case Common.Array:
				type = PrimitiveType.Array;
				return true;
			case Common.Object:
				type = PrimitiveType.Object;
				return true;
			case Common.Boolean:
				type = PrimitiveType.Boolean;
				return true;
			case Common.String:
				type = PrimitiveType.String;
				return true;
			case Common.Number:
				type = PrimitiveType.Number;
				return true;
			case Common.Integer:
				type = PrimitiveType.Integer;
				return true;
			case Common.Null:
				type = PrimitiveType.Null;
				return true;
			default:
				type = PrimitiveType.Null;
				return false;
		};
	}
}

public record TypeKeywordMismatch(PrimitiveType ActualPrimitiveType, IReadOnlyList<PrimitiveType> ExpectedPrimitiveTypes, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [
		ToPrimitiveTypeString(ActualPrimitiveType),
		string.Join(", ", ExpectedPrimitiveTypes.Select(ToPrimitiveTypeString))
	];
	public static DiagnosticException.ToDiagnostic Builder(PrimitiveType actualPrimitiveType, IReadOnlyList<PrimitiveType> expectedPrimitiveTypes) =>
		(Location) => new TypeKeywordMismatch(actualPrimitiveType, expectedPrimitiveTypes, Location);
}
