# OpenAPI Codegen for a TypeScript-friendly Open API clients

Use an OpenAPI specification to generate generalized TypeScript objects for
creating clients of many shapes. Delivered via an npm package.

```sh
npm i @darkpatternsdigital/openapi-codegen-typescript
```

You must also have the .NET 8.0 runtime installed on your machine.

This will provide a corresponding bin to generate the typescript files.

```sh
openapi-codegen-typescript api.yaml api-generated/ -c
```

The above example will take as input an `api.yaml`, output an `api-generated/`
folder with all the typescript files and a gitignore. You should add this as
part of your CI process with only the OpenAPI spec checked in, or download the
spec as part of the CI process.

Written in C#.

## CLI arguments

Ordered arguments:

1. `input-openapi-document` - path to the OpenAPI Schema.
2. `output-path` - path to the output directory where files will be placed.

| Switch               | Description |
| :------------------- | :---------- |
| `-?`, `-h`, `--help` | Display help |
| `-o <path>`, `--options <path>` | Provides path to an Options file, see below |
| `-x`, `--exclude-gitignore` | Do not emit a `.gitignore` file |
| `-c`, `--clean` | Remove all files in the output directory before generating. |


### Options file

While the full structure of the yaml can be seen within the source repository,
commonly, only one or two parameters are needed. Missing keys are merged with
the defaults. For example:

```yaml
mapType: Record<string, {}>
arrayType: Array<{}>
fallbackType: any
types:
  number:
  string:
    formats:
      binary: Blob
    default: string
```

- `mapType` specifies the type to use for JSON maps, which occur when when
  `additionalProperties` is specified. `{}` is used as a placeholder for the
  type.
- `arrayType` specifies the type to use for JSON arrays. `{}` is used as a
  placeholder for the type.
- `fallbackType` specifies the type to use when a schema is unable to be emitted. Other recommended options are `unknown` and `never`.
- `types` must be an object with keys that correspond to the JSON Schema `type`
  properties. Within, the configuration may specify either the `default` (for if
  the format is either not specified or not found) or custom `formats`. Note that this only changes the type of the generated
