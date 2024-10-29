using DarkPatterns.Json.Diagnostics;
using System;
using System.Text.Json.Nodes;

namespace DarkPatterns.Json.Documents;

public class ResolvableNode(NodeMetadata metadata, DocumentRegistry registry)
{
	private readonly Lazy<IDocumentReference> documentReference = new(() => registry.ResolveDocumentFromMetadata(metadata));
	private readonly Lazy<JsonNode?> node = new(() => registry.ResolveNodeFromMetadata(metadata));

	public ResolvableNode(NodeMetadata metadata, DocumentRegistry registry, IDocumentReference document) : this(metadata, registry)
	{
		this.documentReference = new Lazy<IDocumentReference>(() => document);
	}

	public ResolvableNode(NodeMetadata metadata, DocumentRegistry registry, IDocumentReference document, JsonNode? node) : this(metadata, registry)
	{
		this.documentReference = new(() => document);
		this.node = new(() => node);
	}

	public static ResolvableNode FromRoot(DocumentRegistry registry, IDocumentReference documentReference)
	{
		return new ResolvableNode(NodeMetadata.FromRoot(documentReference), registry, documentReference);
	}

	public Location ToLocation() =>
		registry.ResolveLocation(metadata);

	public Uri Id => metadata.Id;
	public DocumentRegistry Registry => registry;
	public NodeMetadata Metadata => metadata;
	public IDocumentReference Document => documentReference.Value;
	public JsonNode? Node => node.Value;
}
