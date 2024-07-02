﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PrincipleStudios.OpenApi.Transformations.Specifications;

namespace PrincipleStudios.OpenApi.Transformations.DocumentTypes;

public interface IDocumentTypeLoader
{
	DocumentTypes.IDocumentReference LoadDocument(Uri retrievalUri, TextReader textReader, IJsonSchemaDialect? dialect);
}