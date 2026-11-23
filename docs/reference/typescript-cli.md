# TypeScript CLI and options

The `openapi-codegen-typescript` command comes from the
`@darkpatternsdigital/openapi-codegen-typescript` npm package or the
`DarkPatterns.OpenApiCodegen.Client.TypeScript` .NET tool. Both take the same
arguments. See [getting started](../getting-started/typescript.md).

## Arguments

```sh
openapi-codegen-typescript <input-openapi-document> <output-path> [options]
```

| Argument | Meaning |
| :------- | :------ |
| `input-openapi-document` | The OpenAPI document. Documents it references with relative `$ref`s are read from disk. |
| `output-path` | The folder to write to. It's created if needed. |

| Switch | Meaning |
| :----- | :------ |
| `-o <path>`, `--options <path>` | An [options file](#options-file). A path that doesn't exist is ignored without a warning. |
| `-c`, `--clean` | Delete everything in `output-path` before writing. |
| `-x`, `--exclude-gitignore` | Don't write a `.gitignore` to `output-path`. By default one is written that ignores the whole folder. |
| `-?`, `-h`, `--help` | Show help. |

The output has an `operations` folder, whose `index` exports every operation,
and a `models` folder. Comments in the generated files name their source
relative to the folder the command ran in.

If the document can't be parsed, the errors are printed to standard error,
nothing is written, and the exit code is `2` or `3`. A missing argument exits
with `1`. Other problems in the document are printed but still exit with `0`.

## Options file

An options file is YAML, merged over the built-in defaults, so it only needs the
keys you change:

```yaml
fallbackType: unknown
types:
  string:
    formats:
      date-time: string
      binary: Blob
    default: string
```

The defaults are in
[`typescript.config.yaml`](../../lib/OpenApi.TypeScript/typescript.config.yaml).

| Key | Default | Meaning |
| :-- | :------ | :------ |
| `mapType` | `Record<string, {}>` | The type for objects with `additionalProperties`. `{}` is replaced with the value type. |
| `arrayType` | `Array<{}>` | The type for arrays. `{}` is replaced with the item type. |
| `fallbackType` | `any` | The type for a schema whose `type` isn't listed under `types`. `unknown` and `never` are stricter choices. |
| `types` | see below | The TypeScript type for each JSON Schema `type` and `format`. These change only the generated types, not how values are serialized. |
| `allowedMimeTypes` | `application/json`, `application/x-www-form-urlencoded`, `application/octet-stream` | Request body media types to generate. Request bodies of other media types are left out. Responses of every media type are generated. |
| `globalReservedIdentifiers` | JavaScript and TypeScript keywords, and names the generated code uses | Names that get a `_` prefix when used as identifiers. |

`types` defaults:

| `type` | Default | Formats |
| :----- | :------ | :------ |
| `boolean` | `boolean` | |
| `string` | `string` | `binary`: `Blob` |
| `number` | `number` | |
| `integer` | `number` | |
| `object` | (a generated type) | `any`: `any` |

Lists such as `allowedMimeTypes` are merged by position: the first entry in
your file replaces the first default entry, and so on. To change a list, repeat
the whole list.

## Schema extensions

`x-ps-optional-as-nullable: true` on an object schema adds `| null` to the type
of each optional property, matching the C# generator's [extension](./csharp-options.md#schema-extensions).
