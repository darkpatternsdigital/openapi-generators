using System;
using System.Collections.Generic;

namespace DarkPatterns.OpenApi.Abstractions;

public record OpenApiServer(Uri Id, Uri Url, string? Description, IReadOnlyDictionary<string, OpenApiServerVariable> Variables);
