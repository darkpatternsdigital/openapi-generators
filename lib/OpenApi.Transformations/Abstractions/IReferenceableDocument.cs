using Json.Pointer;
using System;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public interface IReferenceableDocument : IReferenceableDocumentNode
{
}

public interface IReferenceableDocumentNode
{
	Uri Id { get; }
}
