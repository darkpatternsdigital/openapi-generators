using DarkPatterns.OpenApi.Transformations.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

public interface IOpenApiDocumentFactory
{
	OpenApiDocument ConstructDocument(IDocumentReference documentReference);
}
