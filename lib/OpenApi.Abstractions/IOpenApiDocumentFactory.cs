using DarkPatterns.Json.Documents;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApi.Abstractions;

public interface IOpenApiDocumentFactory
{
	OpenApiDocument ConstructDocument(IDocumentReference documentReference);
}
