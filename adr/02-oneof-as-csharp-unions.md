# `oneOf` as C# unions

C# 15 (.NET 11) adds `union` types. Union output is opt-in through the Options file (`oneOf: record | union`); record output stays the default. Every `oneOf` becomes a union, discriminated or not. We use System.Text.Json's built-in union classifier because its exactly-one-match behavior matches `oneOf` semantics, plus a generated classifier for discriminators. Union output requires net11.0. If a project can't support it, or STJ can't classify a schema (e.g. several string cases), that schema falls back to record output with an informational diagnostic, never a warning.

## Considered Options

- **Closed class hierarchies** for discriminated `oneOf`, which Microsoft recommends for discriminators. Rejected: OpenAPI schemas are often shared by several `oneOf`s, and a C# class can't derive from several bases.
- **Generated classifier that copies the record output's try-each-case matching.** Rejected: STJ's classifier is closer to what `oneOf` means. The cost is that, for ambiguous payloads, union output and record output can deserialize differently.

## Consequences

- `anyOf` never becomes a union, because several cases can match at once.
