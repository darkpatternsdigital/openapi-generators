Adds source generators to generate C# controller base classes from an OpenAPI specification file.

Add this package, select the OpenAPI specification file from your project, and set the build action to `OpenApiSchemaMvcServer`. Within that folder's namespace, you'll automatically get the model and controller classes to implement your interface.

A `services.AddOpenApi...` extension method is added for use in your startup file to ensure you have all base controllers implemented. This function call is not required; it only adds services for MVC.

You can also directly add a reference within the `.csproj` file as follows:

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<ItemGroup>
		<OpenApiSchemaMvcServer Include="schemas/petstore.yaml" />
	</ItemGroup>
</Project>
```

This integrates during the build phase, so you can be sure your classes are up to date with your schema documentation.

Requirements:

- System.Text.Json
- C# 11+
- .NET 8
- Roslyn 3.11 or Roslyn 4.0+ (VS 2022 or later, or other up-to-date Roslyn installation.)
- A reference to `DarkPatterns.OpenApiCodegen.Json.Extensions`

## Other notes

- Take care to include `PrivateAssets="All"` on the reference to the analyzer
  package; you do not need to ship the package itself.
- The namespace of the generated controllers and models will, by default, match the default
  namespace and the path for the file within the project (or the Link, if
  provided.)

## Configuration

Additional settings may be added within the `.csproj`. For example:

```xml
<OpenApiSchemaMvcServer Include="schemas/petstore.yaml" Namespace="My.Extensions" Configuration="path/to/config.yaml" />
```

- `Namespace` - Overrides the namespace detected by the default namespace and
  path of the schema file
- `Configuration` - Additional configuration settings specific to this schema.
  See the configuration yaml documentation below.

In addition, adding the following to an ItemGroup in the csproj (or adding the
yaml file with the build action `OpenApiSchemaCSharpServerOptions` via Visual
Studio) will set the configuration yaml file for all client schemas that do not
have one specifically set.

```xml
<OpenApiSchemaCSharpServerOptions Include="path/to/config.yaml" />
```

### Configuration Yaml

While the full structure of the yaml can be seen within the source repository,
commonly, only one or two parameters are needed. Missing keys are merged with
the defaults. For example:

```yaml
controllerNameExtension: dotnet-mvc-server-controller
mapType: global::System.Collections.Generic.Dictionary<string, {}>
arrayType: global::System.Collections.Generic.IEnumerable<{}>
types:
  number:
    formats:
      float: float
      double: double
    default: double
```

- `controllerNameExtension` specifies the extension (for example,
  `x-dotnet-mvc-server-controller`) used to override the generated controller
  name. This may be specified on either the operation or the path level.
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
