using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.Abstractions;

public interface IReferenceableDocument : IReferenceableDocumentNode
{
	IJsonSchemaDialect Dialect { get; }
}

public interface IReferenceableDocumentNode : IJsonDocumentNode
{
	Uri Id { get; }
}

public static class ReferenceableDocumentNodeExtensions
{
	public static IEnumerable<IJsonDocumentNode> GetNestedNodes(this IJsonDocumentNode node, bool recursive)
	{
		if (!recursive) return node.GetNestedNodes();
		var result = new HashSet<IJsonDocumentNode>();
		var stack = new Stack<IJsonDocumentNode>(node.GetNestedNodes());
		while (stack.Count > 0)
		{
			var next = stack.Pop();
			if (result.Add(next))
				foreach (var n in next.GetNestedNodes())
					stack.Push(n);
		}
		return result;
	}
}