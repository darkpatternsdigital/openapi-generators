using System;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApiCodegen.TestUtils
{
	public static class DocumentHelpers
	{
		public static ParseResult<OpenApiDocument> GetOpenApiDocument(string name, SchemaRegistry schemaRegistry)
		{
			var documentReference = GetDocumentReference(schemaRegistry, name);
			var parseResult = CommonParsers.DefaultParsers.Parse(documentReference, schemaRegistry);
			if (parseResult == null)
				throw new InvalidOperationException("No parser found");

			return parseResult;
		}

		public static IDocumentReference GetDocumentReference(string name)
			=> GetDocumentReference(DocumentLoader.CreateRegistry(), name);

		public static IDocumentReference GetDocumentReference(SchemaRegistry schemaRegistry, string name)
		{
			var uri = new Uri(DocumentLoader.Embedded, name);
			return GetDocumentByUri(schemaRegistry, uri);
		}

		public static IDocumentReference GetDocumentByUri(Uri uri)
		{
			return GetDocumentByUri(DocumentLoader.CreateRegistry(), uri);
		}

		public static IDocumentReference GetDocumentByUri(SchemaRegistry schemaRegistry, Uri uri)
		{
			return schemaRegistry.DocumentRegistry.ResolveDocument(uri, null) ?? throw new InvalidOperationException("Embeded document not found");
		}
	}
}
