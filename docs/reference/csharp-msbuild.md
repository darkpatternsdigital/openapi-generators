# C# MSBuild items and metadata

The C# generator reads OpenAPI documents, JSON Schema documents and options
files from items in your project file.

## Items

| Item | What it does |
| :--- | :----------- |
| `OpenApiSchemaMvcServer` | Generates abstract MVC controllers, an `AddOpenApi<Title>` service registration method, security policies and the models they use. |
| `OpenApiSchemaClient` | Generates extension methods on `HttpMessageInvoker` for each operation, and the models they use. |
| `OpenApiSchemaWebhookClient` | Generates extension methods for calling the document's webhooks and callbacks, and the models they use. |
| `OpenApiSchemaModels` | Generates every schema in the document's `components/schemas`, named as in a server or client, without any operations. Use it for OpenAPI documents that other documents reference for shared schemas. |
| `JsonSchemaDocument` | Generates nothing by itself. Registers a file referenced with `$ref`, so it can be resolved and its types get their own namespace. Types are only generated when another document uses them. |
| `OpenApiSchemaOptions` | An [options file](./csharp-options.md) that applies to every document in the project. |

All of them can be used more than once, and with wildcards:

```xml
<ItemGroup>
	<OpenApiSchemaMvcServer Include="Api/petstore.yaml" PathPrefix="/api" />
	<OpenApiSchemaClient Include="Clients/*.yaml" />
	<JsonSchemaDocument Include="Api/common/*.yaml" />
</ItemGroup>
```

If a referenced document is an OpenAPI document (it has `components/schemas`),
register it as `OpenApiSchemaModels` rather than `JsonSchemaDocument`.
`JsonSchemaDocument` names its schemas from their JSON path, which gives names
like `ComponentsSchemasWidget`.

Every file referenced with `$ref` must be in one of these items; otherwise the
build fails with an error that the document can't be resolved.

## Metadata

| Metadata | Applies to | Meaning |
| :------- | :--------- | :------ |
| `Namespace` | all except `OpenApiSchemaOptions` | The namespace for generated types. Overrides the namespace from `Link` or the file's path. |
| `Link` | all except `OpenApiSchemaOptions` | The file's path within the project, as in any other MSBuild item. Used for the namespace when `Namespace` isn't set. |
| `Configuration` | `OpenApiSchemaMvcServer`, `OpenApiSchemaClient`, `OpenApiSchemaWebhookClient` | A path to an [options file](./csharp-options.md) for this document's controllers or client methods, applied after the project-wide `OpenApiSchemaOptions`. Separate several paths with `;`. Models are generated once for the whole project, so they use only the project-wide options. |
| `PathPrefix` | `OpenApiSchemaMvcServer` | Added to the start of every route, such as `/api`. |
| `SchemaId` | all except `OpenApiSchemaOptions` | The URI that `$ref`s are resolved against, instead of the file's own location. Other documents can then reference this one by that URI, wherever the files are on disk. It's also the URI used by `overrideNames` in the options file. |

Use full paths for `Configuration`, such as
`$(MSBuildProjectDirectory)/api-options.yaml`. A file that can't be found is
ignored without a warning.

### The same file in several items

A file can be in more than one item, for example as both
`OpenApiSchemaMvcServer` and `OpenApiSchemaClient`. Each of
`OpenApiSchemaMvcServer`, `OpenApiSchemaClient` and `OpenApiSchemaWebhookClient`
generates its controllers or client methods with its own `Namespace`, `Link`,
`PathPrefix` and `Configuration`.

The file's models are generated once, so they share one namespace and one
`SchemaId`. The first item that sets a value wins, in this order:
`JsonSchemaDocument`, `OpenApiSchemaMvcServer`, `OpenApiSchemaClient`,
`OpenApiSchemaWebhookClient`, `OpenApiSchemaModels`.

Listing a file twice as the same item type with the same metadata is the same as
listing it once. With different `Namespace`, `Link` or `PathPrefix` values, that
output isn't generated and the build reports `DPD_OPENAPI_CS_002`. Different
`Configuration` values are all applied.

## Namespaces

The namespace for a document's types is chosen in this order:

1. The item's `Namespace` metadata.
2. The project's `RootNamespace` followed by the folders in the item's `Link`.
   `Link="Api/Pets/petstore.yaml"` in a project with root namespace `MyApp`
   gives `MyApp.Api.Pets`.
3. The project's `RootNamespace` followed by the folders in the path the item
   was included with. `Include="Api/petstore.yaml"` gives `MyApp.Api`, and a
   file in the project folder gets `MyApp`.

Folder names are used as they are, so they must be valid C# identifiers. A
folder name of exactly two characters, such as `V1`, becomes `__`; this also
covers `..`. For files outside the project folder, such as
`Include="../schemas/api.yaml"`, set `Link` or `Namespace`.

Individual schemas can go in another namespace with the
`x-dotnet-type-namespace` extension or `overrideNames` in the options file. See
[options and schema extensions](./csharp-options.md).

## Project-wide options

Every `OpenApiSchemaOptions` item applies to every document in the project,
including its models. A `csharp.config.yaml` file in the project folder is
included automatically, so don't list it as an `OpenApiSchemaOptions` item too.

## Seeing the generated code

Generated code is part of the compilation and isn't written to disk. To inspect
it, set:

```xml
<PropertyGroup>
	<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

The files are written under `obj/`, or to `CompilerGeneratedFilesOutputPath` if
you set it. Generated models and operations have a comment naming the document and
JSON pointer they were generated from.

## Diagnostics

Problems in a document are reported as build errors with a `DPD_` code, at the
document's location. The generator also reports:

| Code | Meaning |
| :--- | :------ |
| `DPDAPI001` | The project doesn't reference `DarkPatterns.OpenApiCodegen.Json.Extensions`, which the generated code needs. |
| `DPDAPI002` | The `DarkPatterns.OpenApiCodegen.Json.Extensions` reference is older than the generated code needs. Update it to the version named in the warning. |
| `DPDAPI003` | Code generation failed with an exception, so generated code may be missing. The message has the whole exception; include it when reporting a bug. |
