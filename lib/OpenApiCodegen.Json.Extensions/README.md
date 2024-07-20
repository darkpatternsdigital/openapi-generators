Adds common utilities for System.Text.Json leveraged by DarkPatterns.OpenApiCodegen nuget packages.

- `JsonStringEnumPropertyNameConverter`
    - Supports string enum values via the `System.Text.Json.Serialization.JsonPropertyNameAttribute`.
- `Optional<T>`
    - A class that is either `null` or of a subtype `Optional<T>.Present`. By using `System.Text.Json.Serialization.JsonIgnoreAttribute`, serialization can use this type to distinguish between null and omitted properties in JSON.
