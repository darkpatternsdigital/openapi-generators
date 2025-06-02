using System;
using System.IO;
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

	public static readonly DocumentResolver RelativePathResolver = (baseUri, documentReference) =>
	{
		if (documentReference == null) return null;
		var relative = documentReference.BaseUri.MakeRelativeUri(baseUri);
		if (relative.IsAbsoluteUri) return null;
		var path = new Uri(documentReference.RetrievalUri, relative);
		if (path.Scheme != "file") return null;
		if (!File.Exists(path.LocalPath)) return null;
		using var sr = new StreamReader(path.LocalPath);
		return docLoader.LoadDocument(baseUri, sr, documentReference.Dialect);
	};
}
