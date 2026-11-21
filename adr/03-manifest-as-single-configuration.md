---
status: proposed
---

# Manifest as the single configuration

Configuration is currently split between csproj item metadata (C#), CLI arguments (TypeScript), and Options files, and MSBuild metadata can't express deeper settings: dialect overrides, which paths to import, name overrides. The manifest (YAML, with a published JSON Schema) becomes the one place for configuration, shared by C# and TypeScript. Source generators can only see files through MSBuild items, so C# keeps csproj items, but new items carry only the file path; everything else lives in the manifest.

## Consequences

- When no manifest is provided, the generator builds one in memory: from legacy csproj items, their metadata and Options files in C#, and from CLI arguments in TypeScript. There is then one code path: manifest → output.
- Legacy items (`OpenApiSchemaMvcServer`, `OpenApiSchemaClient`, `OpenApiSchemaWebhookClient`, `JsonSchemaDocument`) and their metadata keep working, and must produce the same output as the manifest written from them.
- A migration tool writes the in-memory manifest to disk so users can adopt it.
- Options files are folded into the manifest (inline per-language options, with precedence defaults → referenced Options file → inline). Once the manifest is stable, using an Options file triggers an informational deprecation diagnostic.
