using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DarkPatterns.OpenApi.Transformations.Specifications;

namespace DarkPatterns.OpenApi.Transformations.DocumentTypes;

public interface IDocumentTypeLoader
{
	DocumentTypes.IDocumentReference LoadDocument(Uri retrievalUri, TextReader textReader, IJsonSchemaDialect? dialect);
}
