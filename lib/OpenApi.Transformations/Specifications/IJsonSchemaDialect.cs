using System;
using System.Collections.Generic;
using System.Linq;
using PrincipleStudios.OpenApi.Transformations.Diagnostics;

namespace PrincipleStudios.OpenApi.Transformations.Specifications;

public interface IJsonSchemaDialect
{
	Uri Id { get; }
	string IdField { get; }
	IReadOnlyCollection<IJsonSchemaVocabulary> Vocabularies { get; }
	IJsonSchemaKeyword UnknownKeyword { get; }
	DiagnosableResult<JsonSchema> ParseMiddleware(ResolvableNode nodeInfo, JsonSchemaParserOptions options, Func<ResolvableNode, JsonSchemaParserOptions, DiagnosableResult<JsonSchema>> next);
}

public record JsonSchemaDialect(
	Uri Id,
	string IdField,
	IReadOnlyCollection<IJsonSchemaVocabulary> Vocabularies,
	IJsonSchemaKeyword UnknownKeyword,
	Func<ResolvableNode, JsonSchemaParserOptions, Func<ResolvableNode, JsonSchemaParserOptions, DiagnosableResult<JsonSchema>>, DiagnosableResult<JsonSchema>>? process = null
) : IJsonSchemaDialect
{
	public DiagnosableResult<JsonSchema> ParseMiddleware(ResolvableNode nodeInfo, JsonSchemaParserOptions options, Func<ResolvableNode, JsonSchemaParserOptions, DiagnosableResult<JsonSchema>> next) =>
		process?.Invoke(nodeInfo, options, next) ?? next(nodeInfo, options);
}

public interface IJsonSchemaVocabulary
{
	Uri Id { get; }
	IReadOnlyDictionary<string, IJsonSchemaKeyword> Keywords { get; }
}

public record JsonSchemaVocabulary(
	Uri Id,
	IReadOnlyDictionary<string, IJsonSchemaKeyword> Keywords
) : IJsonSchemaVocabulary
{
	public JsonSchemaVocabulary(Uri id, (string Keyword, IJsonSchemaKeyword Definition)[] keywords)
		: this(id, keywords.ToDictionary(e => e.Keyword, e => e.Definition))
	{
	}
}
