Adds source generators to generate C# client extension methods from an OpenAPI
specification file.

Add this package, select the OpenAPI specification file from your project, and
set the build action to `OpenApiSchemaClient`. Within that folder's namespace,
you'll automatically get the model and extension methods to invoke the API.

You can also directly add a reference within the `.csproj` file as follows:

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<ItemGroup>
		<OpenApiSchemaClient Include="schemas/petstore.yaml" />
	</ItemGroup>
</Project>
```

This integrates during the build phase, so you can be sure your classes are up
to date with your schema documentation.

Additional yaml files referenced via `$ref` in your OpenAPI documents should be
specified as the build action `JsonSchemaDocument` to be included in watch mode
and to control the Namespace.

## Requirements

- C# 11+
- .NET 8
- Roslyn 3.11 or Roslyn 4.0+ (VS 2022 or later, or other up-to-date Roslyn
  installation.)
- A reference to `DarkPatterns.OpenApiCodegen.Json.Extensions`

## Other notes

- Take care to include `PrivateAssets="All"` on the reference to the analyzer
  package; you do not need to ship the package itself.
- The namespace of the generated extensions will, by default, match the default
  namespace and the path for the file within the project (or the Link, if
  provided.)

## Configuration

Additional settings may be added within the `.csproj`. For example:

```xml
<OpenApiSchemaClient Include="schemas/petstore.yaml" Namespace="My.Extensions" Configuration="path/to/config.yaml" />
```

- `Namespace` - Overrides the namespace detected by the default namespace and
  path of the schema file
- `Configuration` - Additional configuration settings specific to this schema.
  See the configuration yaml documentation below.
- `SchemaId` - Specifies the "retrieval URI" used when resolving relative paths
  to external files. Otherwise, the absolute file-scheme URL will be used.

In addition, adding the following to an ItemGroup in the csproj (or adding the
yaml file with the build action `OpenApiSchemaCSharpClientOptions` via Visual
Studio) will set the configuration yaml file for all client schemas that do not
have one specifically set.

```xml
<OpenApiSchemaCSharpClientOptions Include="path/to/config.yaml" />
```

### Configuration Yaml

While the full structure of the yaml can be seen within the source repository,
commonly, only one or two parameters are needed. Missing keys are merged with
the defaults. For example:

```yaml
mapType: global::System.Collections.Generic.Dictionary<string, {}>
arrayType: global::System.Collections.Generic.IEnumerable<{}>
types:
  number:
    formats:
      float: float
      double: double
    default: double
```

- `mapType` - Specifies the type to use for JSON maps, which occur when when
  `additionalProperties` is specified. `{}` is used as a placeholder for the
  type.
- `arrayType` - Specifies the type to use for JSON arrays. `{}` is used as a
  placeholder for the type.
- `types` must be an object with keys that correspond to the JSON Schema `type`
  properties. Within, the configuration may specify either the `default` (for if
  the format is either not specified or not found) or custom `formats`. Custom
  types may be used here to customize JSON serialization and deserialization,
  especially to standardize string formats for currency or APRs.

    The number example provided will generate a float if the following schema is used:

    ```yaml
    type: number
    format: float
    ```
