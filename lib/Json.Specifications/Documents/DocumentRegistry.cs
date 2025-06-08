using Json.Pointer;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;

namespace DarkPatterns.Json.Documents;

public delegate IDocumentReference? DocumentResolver(Uri baseUri, IDocumentReference? currentDocument);

public class DocumentRegistry(DocumentRegistryOptions registryOptions)
{
	private record DocumentRegistryEntry(
		IDocumentReference Document,
		IReadOnlyDictionary<string, JsonPointer> Anchors
	)
	{
		public Dictionary<string, IJsonDocumentNode> Parsed { get; } = new();
	}

	private readonly ICollection<DocumentRegistryEntry> entries = new HashSet<DocumentRegistryEntry>();

	public void AddDocument(IDocumentReference document)
	{
		if (document is null) throw new ArgumentNullException(nameof(document));
		if (!document.RetrievalUri.IsAbsoluteUri) throw new DiagnosticException(InvalidRetrievalUri.Builder(document.RetrievalUri));

		InternalAddDocument(document);
	}

	private DocumentRegistryEntry InternalAddDocument(IDocumentReference document)
	{
		if (TryResolveDialect(document.RootNode, out var dialect))
		{
			document.Settings.SetDialect(dialect);
		}
		var uri = document.BaseUri;
		if (uri.Fragment is { Length: > 0 }) throw new DiagnosticException(InvalidDocumentBaseUri.Builder(retrievalUri: document.RetrievalUri, baseUri: document.BaseUri));

		if (entries.Any(e => e.Document.BaseUri == uri))
			throw new ArgumentException(string.Format(Errors.DuplicateDocumentBaseUri, uri), nameof(document));

		var visitor = new DocumentRefVisitor();
		visitor.Visit(document.RootNode);

		var result = new DocumentRegistryEntry(document, visitor.Anchors);
		entries.Add(result);
		return result;
	}

	public bool HasDocument(Uri uri)
	{
		return entries.Any(doc => doc.Document.BaseUri == uri);
	}

	public IEnumerable<IJsonDocumentNode> GetAllRegisteredNodes() =>
		from entry in entries
		from node in entry.Parsed.Values
		select node;

	public void Register(IJsonDocumentNode node)
	{
		var set = new HashSet<string>();
		var stack = new Stack<IJsonDocumentNode>([node]);
		while (stack.Count > 0)
		{
			var next = stack.Pop();
			if (set.Contains(next.Metadata.Id.OriginalString))
				continue;
			else
				set.Add(next.Metadata.Id.OriginalString);
			var entry = entries.FirstOrDefault(doc => doc.Document.BaseUri == node.Metadata.Id);
			if (!entry.Parsed.ContainsKey(next.Metadata.Id.Fragment))
				entry.Parsed[next.Metadata.Id.Fragment] = next;
			foreach (var n in next.GetNestedNodes()) stack.Push(n);
		}
	}

	public bool TryGetNode<T>(Uri nodeUri, [NotNullWhen(true)] out T? node)
		where T : class, IJsonDocumentNode
	{
		var entry = entries
			.FirstOrDefault(doc => doc.Document.BaseUri == nodeUri);
		if (entry == null)
		{
			node = null;
			return false;
		}
		node = entry.Parsed.TryGetValue(nodeUri.Fragment, out var result) ? result as T : null;
		return node != null;
	}

	public bool TryGetAllNodes(Uri documentUri, [NotNullWhen(true)] out IEnumerable<IJsonDocumentNode>? nodes)
	{
		var entry = entries
			.FirstOrDefault(doc => doc.Document.BaseUri == documentUri);
		if (entry == null)
		{
			nodes = null;
			return false;
		}
		nodes = entry.Parsed.Values;
		return true;
	}

	public bool TryGetDocument(Uri uri, [NotNullWhen(true)] out IDocumentReference? doc)
	{
		doc = entries.FirstOrDefault(e => e.Document.BaseUri == uri)?.Document;
		return doc != null;
	}

	public ResolvableNode ResolveMetadataNode(Uri uri, NodeMetadata? context = null) => uri.IsAbsoluteUri
		? ResolveMetadataNode(new NodeMetadata(uri, context))
		: context is { Id: Uri baseUri }
		? ResolveMetadataNode(new NodeMetadata(new Uri(baseUri, uri), context))
		: throw new InvalidOperationException(Errors.ReceivedRelativeUriWithoutDocument);

	public ResolvableNode ResolveMetadataNode(NodeMetadata nodeMetadata)
	{
		var registryEntry = ResolveDocumentEntryFromMetadata(nodeMetadata);

		return new ResolvableNode(nodeMetadata, this, registryEntry.Document, ResolveDocumentFragment(nodeMetadata.Id.Fragment, registryEntry));
	}

	private DocumentRegistryEntry ResolveDocumentEntryFromMetadata(NodeMetadata nodeMetadata)
	{
		var relativeDocument = nodeMetadata.Context?.Id is Uri prevUri ? InternalResolveDocumentEntry(prevUri, null).Document : null;
		var uri = nodeMetadata.Id;
		return InternalResolveDocumentEntry(uri, relativeDocument); ;
	}

	public IDocumentReference ResolveDocumentFromMetadata(NodeMetadata nodeMetadata)
	{
		return ResolveDocumentEntryFromMetadata(nodeMetadata).Document;
	}

	public JsonNode? ResolveNodeFromMetadata(NodeMetadata nodeMetadata)
	{
		var registryEntry = ResolveDocumentEntryFromMetadata(nodeMetadata);

		return ResolveDocumentFragment(nodeMetadata.Id.Fragment, registryEntry);
	}

	private static JsonNode? ResolveDocumentFragment(string fragment, DocumentRegistryEntry registryEntry)
	{
		// fragments must start with `#` or be empty
		if (fragment is not { Length: > 1 })
			return registryEntry.Document.RootNode;

		var uri = new UriBuilder(registryEntry.Document.BaseUri) { Fragment = fragment.TrimStart('#') }.Uri;
		if (!ResolvePointer(uri, registryEntry).TryEvaluate(registryEntry.Document.RootNode, out var node))
			throw new DiagnosticException(CouldNotFindTargetNodeDiagnostic.Builder(uri));

		return node;
	}

	private static JsonPointer ResolvePointer(Uri uri, DocumentRegistryEntry registryEntry)
	{
		// Fragments always start with `#`
		if (uri.Fragment is not { Length: > 1 }) return JsonPointer.Empty;
		if (uri.Fragment.StartsWith("#/"))
		{
			if (!JsonPointer.TryParse(Uri.UnescapeDataString(uri.Fragment).Substring(1), out var pointer)) throw new DiagnosticException(InvalidFragmentDiagnostic.Builder(uri.Fragment));
			// needs not-null assertion because we're supporting .NET Standard 2.0, which was pre-NotNullWhenAttribute
			return pointer!;
		}
		else
		{
			if (!registryEntry.Anchors.TryGetValue(uri.Fragment.Substring(1), out var pointer)) throw new DiagnosticException(UnknownAnchorDiagnostic.Builder(uri));
			return pointer;
		}
	}


	public IDocumentReference ResolveDocument(Uri uri, IDocumentReference? relativeDocument) =>
		InternalResolveDocumentEntry(uri, relativeDocument).Document;

	private DocumentRegistryEntry InternalResolveDocumentEntry(Uri uri, IDocumentReference? relativeDocument)
	{
		Uri[] uris = uri.IsAbsoluteUri ? [uri]
			: relativeDocument != null && relativeDocument.BaseUri != relativeDocument.RetrievalUri ? [new Uri(relativeDocument.BaseUri, uri), new Uri(relativeDocument.RetrievalUri, uri)]
			: relativeDocument != null ? [new Uri(relativeDocument.BaseUri, uri)]
			// Throw an exception here because this is a problem with the usage of this class, not data
			: throw new InvalidOperationException(Errors.ReceivedRelativeUriWithoutDocument);

		foreach (var docUri in uris)
			// .NET's Uri type doesn't include the Fragment in equality, so we don't need to check until we fetch
			if (entries.FirstOrDefault(e => e.Document.BaseUri == docUri || e.Document.RetrievalUri == docUri) is var document and not null)
				return document;
		return InternalFetch(relativeDocument, uris.First());
	}

	private DocumentRegistryEntry InternalFetch(IDocumentReference? relativeDocument, Uri docUri)
	{
		if (docUri.Fragment is { Length: > 0 })
			docUri = new UriBuilder(docUri) { Fragment = "" }.Uri;

		var document = (from resolver in registryOptions.Resolvers
						let doc = resolver(docUri, relativeDocument)
						where doc != null
						select doc).FirstOrDefault();
		if (document == null)
			throw new DiagnosticException(ResolveDocumentDiagnostic.Builder(docUri));

		return InternalAddDocument(document);
	}

	public Location ResolveLocation(ResolvableNode node) => ResolveLocation(node.Metadata);

	public Location ResolveLocation(NodeMetadata key) => ResolveLocation(key.Id);

	public Location ResolveLocation(Uri id)
	{
		var registryEntry = InternalResolveDocumentEntry(id, null);
		var fileLocation = registryEntry.Document.GetLocation(ResolvePointer(id, registryEntry));
		return new Location(registryEntry.Document.RetrievalUri, fileLocation);
	}

	private bool TryResolveDialect(JsonNode? rootNode, [NotNullWhen(true)] out IJsonSchemaDialect? result)
	{
		if (rootNode is JsonObject obj)
			if (registryOptions.DialectMatchers.FirstOrDefault(matcher => matcher.MatchesDocument(obj)) is { Dialect: var dialect })
			{
				result = dialect;
				return true;
			}
		result = null;
		return false;
	}

	private class DocumentRefVisitor : JsonNodeVisitor
	{
		public Dictionary<string, JsonPointer> Anchors { get; } = new Dictionary<string, JsonPointer>();
		public Dictionary<string, JsonPointer> BundledSchemas { get; } = new Dictionary<string, JsonPointer>();

		protected override void VisitObject(JsonObject obj, JsonPointer elementPointer)
		{
			if (obj.TryGetPropertyValue("$anchor", out var elem) && elem?.GetValue<string>() is string anchorId)
				Anchors.Add(anchorId, elementPointer);
			if (elementPointer != JsonPointer.Empty && obj.TryGetPropertyValue("$id", out elem) && elem?.GetValue<string>() is string bundledSchemaId)
				BundledSchemas.Add(bundledSchemaId, elementPointer);
			base.VisitObject(obj, elementPointer);
		}
	}
}

public static class JsonDocumentUtils
{
	public static Uri GetDocumentBaseUri(IDocumentReference document) =>
		document.RootNode.GetBaseUri(document.RetrievalUri, document.Settings.GetDialect());


	public static Uri GetBaseUri(this JsonNode? jsonNode, Uri retrievalUri, IJsonSchemaDialect dialect) =>
		jsonNode is JsonObject obj && dialect.IdField is string field
		&& obj.TryGetPropertyValue(field, out var id) && id?.GetValue<string>() is string baseId
			? new Uri(retrievalUri, baseId)
			: retrievalUri;

}

public record InvalidRetrievalUri(Uri RetrievalUri, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [RetrievalUri.OriginalString];
	public static DiagnosticException.ToDiagnostic Builder(Uri retrievalUri) => (Location) => new InvalidRetrievalUri(retrievalUri, Location);
}

public record InvalidDocumentBaseUri(Uri RetrievalUri, Uri BaseUri, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [RetrievalUri.OriginalString, BaseUri.OriginalString];
	public static DiagnosticException.ToDiagnostic Builder(Uri retrievalUri, Uri baseUri) => (Location) => new InvalidDocumentBaseUri(retrievalUri, baseUri, Location);
}

public record InvalidFragmentDiagnostic(string ActualFragment, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [ActualFragment];
	public static DiagnosticException.ToDiagnostic Builder(string actualFragment) => (Location) => new InvalidFragmentDiagnostic(actualFragment, Location);
}

public record InvalidRefDiagnostic(string RefValue, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [RefValue];
	public static DiagnosticException.ToDiagnostic Builder(string refValue) => (Location) => new InvalidRefDiagnostic(refValue, Location);
}

public record CouldNotFindTargetNodeDiagnostic(Uri Uri, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Uri.OriginalString];
	internal static DiagnosticException.ToDiagnostic Builder(Uri uri) => (Location) => new CouldNotFindTargetNodeDiagnostic(uri, Location);
}

public record UnknownAnchorDiagnostic(Uri Uri, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Uri.OriginalString];
	internal static DiagnosticException.ToDiagnostic Builder(Uri uri) => (Location) => new UnknownAnchorDiagnostic(uri, Location);
}

public record ResolveDocumentDiagnostic(Uri Uri, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Uri.OriginalString];
	internal static DiagnosticException.ToDiagnostic Builder(Uri uri) => (Location) => new ResolveDocumentDiagnostic(uri, Location);
}
