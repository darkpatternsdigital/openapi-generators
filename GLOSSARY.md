# OpenAPI Codegen

Generates C# and TypeScript code from OpenAPI and JSON Schema documents.

## Language

**Output**:
One kind of code generated from a document: a Server, a Client, a Webhook Client, or Models (models only, for documents that supply shared schemas).
_Avoid_: File type, generator, MvcServer (in prose)

**Options file**:
A YAML file that overrides a language's default generation options.
_Avoid_: Config, schema options, configuration

**Manifest**:
A file, shared by the C# and TypeScript generators, that lists documents and their deeper settings (schema dialects, which paths to import, name overrides) without editing the documents themselves.

### C# `oneOf` generation

**Record output**:
A `oneOf` generated as a record with one nullable member per case.
_Avoid_: Type union model, one-of model

**Union output**:
A `oneOf` generated as a C# `union` type.
_Avoid_: Type union model, one-of model
