﻿using Json.Pointer;
using DarkPatterns.OpenApi.Transformations.Diagnostics;
using DarkPatterns.OpenApi.Transformations.Specifications;
using DarkPatterns.OpenApiCodegen;
using System;

namespace DarkPatterns.OpenApi.Transformations.DocumentTypes;

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
	/// The json schema dialect to use for this document
	/// </summary>
	IJsonSchemaDialect Dialect { get; set; }

	FileLocationRange? GetLocation(JsonPointer path);
}
