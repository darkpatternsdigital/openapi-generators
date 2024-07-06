using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Json.Pointer;
using PrincipleStudios.OpenApi.Transformations;
using PrincipleStudios.OpenApi.Transformations.Specifications;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft04;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Applicator;
using PrincipleStudios.OpenApi.Transformations.Specifications.OpenApi3_0;

namespace PrincipleStudios.OpenApi.CSharp;


public record CSharpInlineDefinition(string Text, bool Nullable = false, bool IsEnumerable = false)
{
	// Assumes C#8, since it's standard in VS2019+, which is when nullable reference types were introduced
	public CSharpInlineDefinition MakeNullable() =>
		Nullable ? this : new(Text + "?", Nullable: true, IsEnumerable: IsEnumerable);
}

public class CSharpInlineSchemas(DocumentRegistry documentRegistry, CSharpSchemaOptions options)
{
	public CSharpInlineDefinition ToInlineDataType(JsonSchema? schema)
	{
		if (schema == null) return new("object", Nullable: true);

		CSharpInlineDefinition result = CSharpTypeInfo.From(schema) switch
		{
			{ Type: "object", Properties: { Count: 0 }, AdditionalProperties: JsonSchema dictionaryValueSchema } =>
				new(options.ToMapType(ToInlineDataType(dictionaryValueSchema).Text), IsEnumerable: true),
			{ Type: "array", Items: JsonSchema items } =>
				new(options.ToArrayType(ToInlineDataType(items).Text), IsEnumerable: true),
			{ Type: string type, Format: var format } when !ProduceSourceEntry(schema) && options.Find(type, format) != "object" =>
				new(options.Find(type, format)),
			// { Reference: not null } =>
			// 	new(UseReferenceName(schema)),
			_ when ProduceSourceEntry(schema) =>
				new(GetClassName(schema)),
			{ Type: string type, Format: var format } =>
				new(options.Find(type, format)),
			_ => throw new NotSupportedException("Unknown schema"),
		};
		return schema?.TryGetAnnotation<NullableKeyword>() is { IsNullable: true }
			? result.MakeNullable()
			: result;
	}

	public bool ProduceSourceEntry(JsonSchema schema)
	{
		// C# can't inline things that must be referenced, and vice versa.
		// (Except with tuples, but those don't serialize/deserialize reliably yet.)
		return CSharpTypeInfo.From(schema) switch
		{
			{ Type: "object", Properties: { Count: 0 }, AdditionalProperties: JsonSchema _ } => false,
			// { UnresolvedReference: true, Reference: { IsExternal: false } } => ProduceSourceEntry((OpenApiSchema)GetApiContexts(schema).First().Reverse().Select(e => e.Element).OfType<OpenApiDocument>().Last().ResolveReference(schema.Reference)),
			// { UnresolvedReference: true } => throw new ArgumentException("Unable to resolve reference"),
			{ AllOf: { Count: > 1 } } => true,
			{ AnyOf: { Count: > 1 } } => true,
			{ OneOf: { Count: > 1 } } => true,
			{ Type: "string", Enum: { Count: > 0 } } => true,
			{ Type: "array" } => false,
			{ Items: JsonSchema _ } => false,
			{ Type: string type, Format: var format, Properties: { Count: 0 }, Enum: { Count: 0 } } => options.Find(type, format) == "object",
			{ Type: "object", Format: null } => true,
			{ Properties: { Count: > 1 } } => true,
			{ Type: "string" or "number" or "integer" or "boolean" } => false,
			{ } => false,
			_ => throw new NotSupportedException("Unknown schema"),
		};
	}

	private string GetClassName(JsonSchema schema)
	{
		return CSharpNaming.ToClassName(UriToIdentifier(schema.Metadata.Id), options.ReservedIdentifiers());
	}

#pragma warning disable CA1707 // Identifiers should not contain underscores
	private static readonly Regex _2xxRegex = new Regex("2[0-9]{2}");
#pragma warning restore CA1707 // Identifiers should not contain underscores
	private string UriToIdentifier(Uri uri)
	{
		// TODO
		var docReference = documentRegistry.ResolveDocument(uri, null);

		var (parts, remaining) = Simplify(JsonPointer.Parse(uri.Fragment).Segments);
		while (remaining.Count > 0)
		{
			IEnumerable<string> newParts;
			(newParts, remaining) = Simplify(remaining);
			parts = parts.Concat(newParts).ToArray();
		}

		return string.Join(" ", parts);

		(IEnumerable<string> parts, IReadOnlyList<PointerSegment> remaining) Simplify(IReadOnlyList<PointerSegment> context)
		{
			// TODO
			return (context.Select(s => s.Value), Array.Empty<PointerSegment>());
		}
	}
}