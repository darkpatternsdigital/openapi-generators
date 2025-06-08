using System;
using System.Collections.Generic;
using System.IO;

namespace DarkPatterns.Json.Documents;

public interface IDocumentTypeLoader
{
	IDocumentReference LoadDocument(Uri retrievalUri, TextReader textReader, IEnumerable<object>? settings = null);
}
