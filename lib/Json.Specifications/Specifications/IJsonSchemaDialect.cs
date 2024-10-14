using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Specifications;

public interface IJsonSchemaDialect
{
	Uri Id { get; }
	string? IdField { get; }
	IReadOnlyCollection<IJsonSchemaVocabulary> Vocabularies { get; }
	IJsonSchemaKeyword UnknownKeyword { get; }

	DocumentInfo GetInfo(IDocumentReference doc);
}

public record DocumentInfo(string? Title, string? Description);

public record JsonSchemaDialect(
	Uri Id,
	string? IdField,
	IReadOnlyCollection<IJsonSchemaVocabulary> Vocabularies,
	IJsonSchemaKeyword UnknownKeyword,
	Func<IDocumentReference, DocumentInfo>? GetInfoDefinition = null
) : IJsonSchemaDialect
{
	public DocumentInfo GetInfo(IDocumentReference doc)
	{
		return GetInfoDefinition?.Invoke(doc) ?? new DocumentInfo(Title: $"JSON Schema from {doc.BaseUri.OriginalString}", Description: null);
	}
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
