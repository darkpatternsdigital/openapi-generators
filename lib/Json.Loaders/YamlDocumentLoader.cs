﻿using Json.Pointer;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using Yaml2JsonNode;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using DarkPatterns.Json.Documents;

namespace DarkPatterns.Json.Loaders;

public class YamlDocumentLoader : IDocumentTypeLoader
{
	public IDocumentReference LoadDocument(Uri retrievalUri, TextReader textReader, IJsonSchemaDialect? dialect)
	{
		var yamlStream = new YamlStream();
		try
		{
			yamlStream.Load(textReader);
		}
		catch (YamlException ex)
		{
			throw new DocumentException(YamlLoadDiagnostic.Builder(ex, retrievalUri), Errors.UnableToLoadYaml, ex);
		}

		// TODO: check $ top-level variables for vocabulary overrides
		return new YamlDocument(retrievalUri, yamlStream, dialect ?? Json.Specifications.Dialects.StandardDialects.CoreNext);
	}

	private class YamlDocument : IDocumentReference
	{
		private YamlStream yamlStream;

		public YamlDocument(Uri retrievalUri, YamlStream yamlStream, IJsonSchemaDialect dialect)
		{
			this.RetrievalUri = retrievalUri;
			this.yamlStream = yamlStream;
			this.RootNode = yamlStream.Documents[0].ToJsonNode();
			this.Dialect = dialect;
		}

		public Uri BaseUri => JsonDocumentUtils.GetDocumentBaseUri(this);

		public Uri RetrievalUri { get; }

		public JsonNode? RootNode { get; }

		public IJsonSchemaDialect Dialect { get; set; }

		string IDocumentReference.OriginalPath => RetrievalUri.OriginalString;

		public FileLocationRange? GetLocation(JsonPointer path)
		{
			var rootNode = yamlStream.Documents[0].RootNode;
			var targetNode = path.Evaluate(rootNode);
			if (targetNode == null)
				return null;

			return new FileLocationRange(
				targetNode.AllNodes.Min(n => n.Start).ToFileLocationMark(),
				targetNode.AllNodes.Max(n => n.End).ToFileLocationMark()
			);
		}
	}
}

public static class YamlUtils
{
	public static FileLocationRange FromException(YamlException ex) =>
		new FileLocationRange(
			ToFileLocationMark(ex.Start),
			ToFileLocationMark(ex.End)
		);

	public static FileLocationMark ToFileLocationMark(this Mark mark) => new FileLocationMark(mark.Line, mark.Column);

	public static YamlNode? Evaluate(this JsonPointer jsonPointer, YamlNode node)
	{
		foreach (var segment in jsonPointer.Segments)
		{
			switch (node)
			{
				case YamlMappingNode obj:
					if (!obj.Children.TryGetValue(segment.Value, out node))
						return null;
					continue;
				case YamlSequenceNode array:
					var index = int.Parse(segment.Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture);
					if (array.Children.Count <= index) return null;
					node = array.Children[index];
					continue;
				default:
					// pointer kept going, but no-where else to go
					return null;
			}
		}
		return node;
	}
}

public record YamlLoadDiagnostic(Location Location, string Message) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments() => [Message];

	public static YamlLoadDiagnostic Builder(YamlException ex, Uri retrievalUri)
	{
		var location = YamlUtils.FromException(ex);
		return new YamlLoadDiagnostic(new Location(retrievalUri, location), ex.Message);
	}
}
