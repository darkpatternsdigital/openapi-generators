using System;
using System.Collections.Generic;

namespace DarkPatterns.OpenApi.Abstractions;

public record OpenApiServerVariable(Uri Id, IReadOnlyList<string> AllowedValues, string DefaultValue, string? Description);
