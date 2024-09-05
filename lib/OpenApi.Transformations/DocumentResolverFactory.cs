using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using DarkPatterns.OpenApiCodegen;

namespace DarkPatterns.OpenApi.Transformations;

public record DocumentRegistryOptions(
	IReadOnlyList<DocumentResolver> Resolvers
);

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

	public static (IDocumentReference, DocumentRegistry) FromInitialDocumentInMemory(Uri uri, string documentContents, DocumentRegistryOptions resolverOptions)
	{
		using var sr = new StringReader(documentContents);
		var doc = docLoader.LoadDocument(uri, sr, null);

		var registry = new DocumentRegistry(resolverOptions with
		{
			Resolvers = Enumerable.Concat([RelativeFrom(doc)], resolverOptions.Resolvers).ToArray()
		});
		registry.AddDocument(doc);
		return (doc, registry);
	}
}
