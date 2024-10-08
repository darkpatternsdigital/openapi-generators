﻿using System;
using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApi.Transformations.Abstractions;
using DarkPatterns.OpenApi.Transformations;

namespace DarkPatterns.OpenApiCodegen.TestUtils
{
	public static class DocumentHelpers
	{
		public static ParseResult<OpenApiDocument> GetOpenApiDocument(string name, DocumentRegistry registry)
		{
			var documentReference = GetDocumentReference(registry, name);
			var parseResult = CommonParsers.DefaultParsers.Parse(documentReference, registry);
			if (parseResult == null)
				throw new InvalidOperationException("No parser found");

			return parseResult;
		}

		public static IDocumentReference GetDocumentReference(string name)
			=> GetDocumentReference(DocumentLoader.CreateRegistry(), name);

		public static IDocumentReference GetDocumentReference(DocumentRegistry registry, string name)
		{
			var uri = new Uri(DocumentLoader.Embedded, name);
			return GetDocumentByUri(registry, uri);
		}

		public static IDocumentReference GetDocumentByUri(Uri uri)
		{
			return GetDocumentByUri(DocumentLoader.CreateRegistry(), uri);
		}

		public static IDocumentReference GetDocumentByUri(DocumentRegistry registry, Uri uri)
		{
			return registry.ResolveDocument(uri, null) ?? throw new InvalidOperationException("Embeded document not found");
		}
	}
}
