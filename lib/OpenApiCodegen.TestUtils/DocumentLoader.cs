using System;
using DarkPatterns.Json.Specifications;
using System.IO;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Loaders;
using DarkPatterns.OpenApi.Specifications.v3_0;
using DarkPatterns.Json.Specifications.Dialects;
using System.Collections.Generic;

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
				return LoadEmbeddedDocument(baseUri, currentDocument?.Settings.SettingObjects ?? []);
			default:
				return null;
		}
	}


	private static IDocumentReference LoadEmbeddedDocument(Uri baseUri, IEnumerable<object> settings)
	{
		using var documentStream = GetEmbeddedDocumentStream(baseUri);
		using var sr = new StreamReader(documentStream);
		var result = docLoader.LoadDocument(baseUri, sr, settings);
		return result;
	}

	public static Stream GetEmbeddedDocumentStream(Uri baseUri)
	{
		if (baseUri is not { Scheme: "proj", Host: "embedded" })
			throw new ArgumentException("Uri was not of the proper format", nameof(baseUri));
		return typeof(DocumentHelpers).Assembly.GetManifestResourceStream($"DarkPatterns.OpenApiCodegen.TestUtils.schemas.{baseUri.LocalPath.Substring(1)}");
	}

	public static SchemaRegistry CreateRegistry()
	{
		return new SchemaRegistry(new DocumentRegistry(new([DocumentResolver], [
			OpenApi3_0DocumentFactory.JsonSchemaDialectMatcher,
			.. StandardDialects.StandardMatchers
		])));
	}
}
