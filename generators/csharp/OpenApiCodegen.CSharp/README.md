Adds C# source generators from an OpenAPI specification file.

1. Add this package to your project, as well as a reference to `DarkPatterns.OpenApiCodegen.Json.Extensions`.

    ```xml
    <ItemGroup>
        <PackageReference Include="DarkPatterns.OpenApiCodegen.Json.Extensions" Version="0.17.1" />
        <PackageReference Include="DarkPatterns.OpenApiCodegen.CSharp" Version="0.20.0" PrivateAssets="All" />
    </ItemGroup>
    ```

    _Note:_ Take care to include `PrivateAssets="All"` on the reference to the analyzer
    package; you do not need to ship the package itself.

2. Add the OpenAPI file to the project.

    In Visual Studio, select the OpenAPI specification file from your project,
    and set the build action to `OpenApiSchemaMvcServer`. Within that folder's
    namespace, you'll automatically get the model and controller classes to
    implement your interface.

    In VS Code, you can also directly add a reference within the `.csproj` file as follows:

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
        <ItemGroup>
            <OpenApiSchemaMvcServer Include="schemas/petstore.yaml" />
        </ItemGroup>
    </Project>
    ```

Additional yaml files referenced via `$ref` in your OpenAPI documents should be
specified as the build action `JsonSchemaDocument` to be included in watch mode
and to control the Namespace.

## Requirements

- System.Text.Json
- C# 11+
- .NET 8
- Roslyn 3.11 or Roslyn 4.0+ (VS 2022 or later, or other up-to-date Roslyn installation.)
- A reference to `DarkPatterns.OpenApiCodegen.Json.Extensions`

## Other notes

- The namespace of the generated controllers and models will, by default, match the default
  namespace and the path for the file within the project (or the Link, if
  provided.)

## Configuration

Additional settings may be added within the `.csproj`. For example, see the below sections.

### MVC Server

```xml
<OpenApiSchemaMvcServer Include="schemas/petstore.yaml" Namespace="My.Extensions" Configuration="path/to/config.yaml" />
```

- `Namespace` - Overrides the namespace detected by the default namespace and
  path of the schema file
- `Configuration` - Additional configuration settings specific to this schema.
  See the configuration yaml documentation below.
- `PathPrefix` - Prefixes the paths of the generated paths with the given path.
- `SchemaId` - Specifies the "retrieval URI" used when resolving relative paths
  to external files. Otherwise, the absolute file-scheme URL will be used.

In addition, adding the following to an ItemGroup in the csproj (or adding the
yaml file with the build action `OpenApiSchemaOptions` via Visual
Studio) will set the configuration yaml file for all schemas.

### Client

```xml
<OpenApiSchemaClient Include="schemas/petstore.yaml" Namespace="My.Extensions" Configuration="path/to/config.yaml" />
```

- `Namespace` - Overrides the namespace detected by the default namespace and
  path of the schema file
- `Configuration` - Additional configuration settings specific to this schema.
  See the configuration yaml documentation below.
- `SchemaId` - Specifies the "retrieval URI" used when resolving relative paths
  to external files. Otherwise, the absolute file-scheme URL will be used.


### Configuration Yaml

While the full structure of the yaml can be seen within the source repository,
commonly, only one or two parameters are needed. Missing keys are merged with
the defaults. For example:

```yaml
extensions:
  controllerName: dotnet-mvc-server-controller
  typeNameOverride: dotnet-type-name
  namespaceOverride: dotnet-type-namespace
mapType: global::System.Collections.Generic.Dictionary<string, {}>
arrayType: global::System.Collections.Generic.IEnumerable<{}>
types:
  number:
    formats:
      float: float
      double: double
    default: double
overrideNames:
  proj://darkpatterns-openapi/multi-file-ref-types.yaml#/BadRequest: My.Common.BadRequest
```

- `extensions.controllerName` specifies the extension (for example,
  `x-dotnet-mvc-server-controller`) used to override the generated controller
  name. This may be specified on either the operation or the path level.
- `extensions.typeNameOverride` specifies the extension (for example,
  `x-dotnet-type-name`) used to override the generated type name. This may be
  specified on any JSON schema that will be emitted as its own class, enum, etc.
- `extensions.namespaceOverride` specifies the extension (for example,
  `x-dotnet-type-namespace`) used to override the generated type namespace. This
  may be specified on any JSON schema that will be emitted as its own class,
  enum, etc.
- `mapType` specifies the type to use for JSON maps, which occur when when
  `additionalProperties` is specified. `{}` is used as a placeholder for the
  type.
- `arrayType` specifies the type to use for JSON arrays. `{}` is used as a
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
- `overrideNames` is a dictionary of schema URIs to the namespace-qualified C#
  type name to use for the generated class. (Note: this feature is still
  experimental and may change or be removed in a later relaese.)

Configurations may be specified with the following setting:

```xml
<OpenApiSchemaOptions Include="path/to/config.yaml" />
```

Alternatively, if a `csharp.config.yaml` is placed in the root of your project
without any other schema options being set, it will be included.

### Schema extensions

Extensions in OpenAPI documents are additional properties, starting with `x-`
that can go nearly anywhere in an OpenAPI 3.0 document. The following extensions
are available:

- `x-dotnet-mvc-server-controller` overrides the name of the controller class
  generated for paths and operations. This extension may be specified either at
  the path or operation level.
- `x-dotnet-type-namespace` overrides the namespace for a single schema. This is
  a higher-priority than settings within the csproj but lower priority than
  individual schema name overrides in the options file.
- `x-dotnet-type-name` overrides the type name for a single schema. This is
  a higher-priority than settings within the csproj but lower priority than
  individual schema name overrides in the options file.
