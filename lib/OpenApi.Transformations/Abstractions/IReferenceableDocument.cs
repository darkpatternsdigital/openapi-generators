using System;
using System.Collections.Generic;
using System.Linq;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations.Abstractions;

public interface IReferenceableDocument : IReferenceableDocumentNode
{
}

public interface IReferenceableDocumentNode : IJsonDocumentNode
{
	Uri Id { get; }
}

public static class ReferenceableDocumentNodeExtensions
{
	public static IEnumerable<IJsonDocumentNode> GetNestedNodes(this IJsonDocumentNode node, bool recursive)
	{
		foreach (var n in node.GetNestedNodes())
		{
			yield return n;
			if (recursive)
				foreach (var deeper in n.GetNestedNodes(recursive))
					if (deeper.Metadata.Id == node.Metadata.Id
						|| deeper.Metadata.Id.Fragment.StartsWith(node.Metadata.Id.Fragment + "/"))
						yield return deeper;
		}
	}
}