﻿using System;
using DarkPatterns.Json.Specifications;
using System.IO;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Loaders;

namespace DarkPatterns.OpenApiCodegen.TestUtils;

public class DocumentLoader
{
	public static readonly Uri Embedded = new Uri("proj://embedded/");
	private static readonly YamlDocumentLoader docLoader = new YamlDocumentLoader();

	private static IDocumentReference? DocumentResolver(Uri baseUri, IDocumentReference? currentDocument = null)
	{
		switch (baseUri)
		{
			case { Scheme: "proj", Host: "embedded" }:
				return LoadEmbeddedDocument(baseUri, currentDocument?.Dialect);
			default:
				return null;
		}
	}


	private static IDocumentReference LoadEmbeddedDocument(Uri baseUri, IJsonSchemaDialect? dialect)
	{
		using var documentStream = GetEmbeddedDocumentStream(baseUri);
		using var sr = new StreamReader(documentStream);
		var result = docLoader.LoadDocument(baseUri, sr, dialect);
		return result;
	}

	public static Stream GetEmbeddedDocumentStream(Uri baseUri)
	{
		if (baseUri is not { Scheme: "proj", Host: "embedded" })
			throw new ArgumentException("Uri was not of the proper format", nameof(baseUri));
		return typeof(DocumentHelpers).Assembly.GetManifestResourceStream($"DarkPatterns.OpenApiCodegen.TestUtils.schemas.{baseUri.LocalPath.Substring(1)}");
	}

	public static DocumentRegistry CreateRegistry()
	{
		return new DocumentRegistry(new([DocumentResolver]));
	}
}
