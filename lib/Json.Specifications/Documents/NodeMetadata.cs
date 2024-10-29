using System;

namespace DarkPatterns.Json.Documents;

public record NodeMetadata(Uri Id, NodeMetadata? Context = null)
{
	public static NodeMetadata FromRoot(IDocumentReference documentReference)
	{
		return new NodeMetadata(documentReference.BaseUri);
	}

}
