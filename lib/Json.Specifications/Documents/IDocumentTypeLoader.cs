using System;
using System.IO;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.Json.Documents;

public interface IDocumentTypeLoader
{
	IDocumentReference LoadDocument(Uri retrievalUri, TextReader textReader, IJsonSchemaDialect? dialect);
}
