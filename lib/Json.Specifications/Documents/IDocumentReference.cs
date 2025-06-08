using Json.Pointer;
using DarkPatterns.Json.Specifications;
using System;
using DarkPatterns.Json.Diagnostics;
using System.Linq;

namespace DarkPatterns.Json.Documents;

public interface IDocumentReference : IBaseDocument
{
	/// <summary>
	/// The RetrievalUri of the document.
	/// </summary>
	Uri RetrievalUri { get; }

	/// <summary>
	/// The root element of the document.
	/// </summary>
	System.Text.Json.Nodes.JsonNode? RootNode { get; }

	/// <summary>
	/// May be a file path on disk or may be a Uri.
	/// </summary>
	string OriginalPath { get; }

	/// <summary>
	/// The settings, including the dialect, for this document
	/// </summary>
	DocumentSettings Settings { get; }

	FileLocationRange? GetLocation(JsonPointer path);
}
