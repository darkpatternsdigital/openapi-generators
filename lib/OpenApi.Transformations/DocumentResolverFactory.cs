using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Loaders;

namespace DarkPatterns.OpenApi.Transformations;

public static class DocumentResolverFactory
{
	private static readonly YamlDocumentLoader docLoader = new YamlDocumentLoader();

	public static DocumentResolver LoadAs(Uri uri, string documentContents)
	{
		return (baseUri, _) =>
		{
			if (baseUri != uri) return null;
			using var sr = new StringReader(documentContents);
			return docLoader.LoadDocument(uri, sr, null);
		};
	}

	public static DocumentResolver RelativeFrom(IDocumentReference documentReference)
	{
		return (baseUri, _) =>
		{
			var relative = documentReference.BaseUri.MakeRelativeUri(baseUri);
			if (relative.IsAbsoluteUri) return null;
			var path = new Uri(documentReference.RetrievalUri, relative);
			if (path.Scheme != "file") return null;
			using var sr = new StreamReader(path.LocalPath);
			return docLoader.LoadDocument(baseUri, sr, documentReference.Dialect);
		};
	}

	public static IDocumentReference Load(Uri uri, string documentContents)
	{
		using var sr = new StringReader(documentContents);
		var doc = docLoader.LoadDocument(uri, sr, null);
		return doc;
	}

	public static DocumentRegistry From(IEnumerable<IDocumentReference> documents, DocumentRegistryOptions resolverOptions)
	{
		var allDocs = documents.ToArray();

		var registry = new DocumentRegistry(resolverOptions with
		{
			Resolvers = [.. documents.Select(RelativeFrom), .. resolverOptions.Resolvers],
		});
		foreach (var doc in allDocs)
			registry.AddDocument(doc);
		return registry;
	}

	public static (IDocumentReference, SchemaRegistry) FromInitialDocumentInMemory(Uri uri, string documentContents, DocumentRegistryOptions resolverOptions)
	{
		using var sr = new StringReader(documentContents);
		var doc = docLoader.LoadDocument(uri, sr, null);

		return (doc, new SchemaRegistry(From([doc], resolverOptions)));
	}
}
