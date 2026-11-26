# C# options and schema extensions

## Options files

An options file is YAML. It's merged over the built-in defaults, so it only
needs the keys you change. Apply one to the whole project with
`OpenApiSchemaOptions`, or to one document's controllers or client methods with
`Configuration` metadata. See
[MSBuild items and metadata](./csharp-msbuild.md).

```xml
<ItemGroup>
	<OpenApiSchemaOptions Include="api-options.yaml" />
</ItemGroup>
```

A `csharp.config.yaml` in the project folder is included automatically, without
an `OpenApiSchemaOptions` item.

Example:

```yaml
mapType: global::System.Collections.Generic.IReadOnlyDictionary<string, {}>
arrayType: global::System.Collections.Generic.IReadOnlyList<{}>
types:
  string:
    formats:
      date-time: global::System.DateTime
      currency: global::MyApp.Money
securityAttribute: MyApp.SecurityAttribute
```

The defaults are in
[`csharp.config.yaml`](../../lib/OpenApi.CSharp/csharp.config.yaml) and, for
servers,
[`MvcServer/csharp.config.yaml`](../../generators/csharp/OpenApiCodegen.CSharp.Base/MvcServer/csharp.config.yaml).

### Keys

| Key | Default | Meaning |
| :-- | :------ | :------ |
| `mapType` | `global::System.Collections.Generic.Dictionary<string, {}>` | The type for objects with `additionalProperties`. `{}` is replaced with the value type. |
| `arrayType` | `global::System.Collections.Generic.IEnumerable<{}>` | The type for arrays. `{}` is replaced with the item type. |
| `fallbackType` | `object` | The type for a schema whose `type` isn't listed under `types`. |
| `types` | see below | The C# type for each JSON Schema `type` and `format`. |
| `overrideNames` | none | Maps a schema's URI to a namespace-qualified C# type name. Experimental, and may change or be removed. |
| `securityAttribute` | none | An attribute added to each controller action whose operation has `security`, or inherits the document's. Its one argument is a string with those security requirements as JSON. Servers only. |
| `extensions.controllerName` | `dotnet-mvc-server-controller` | The extension name (without `x-`) that sets an operation's controller. |
| `extensions.typeNameOverride` | `dotnet-type-name` | The extension name (without `x-`) that sets a schema's type name. |
| `extensions.namespaceOverride` | `dotnet-type-namespace` | The extension name (without `x-`) that sets a schema's namespace. |
| `globalReservedIdentifiers` | C# keywords and `object` members | Names that get a `_` prefix when used as identifiers. |
| `contextualReservedIdentifiers` | members of `ControllerBase`, `enum` and the model base | Extra reserved names, keyed by where they're used: `ControllerBase`, `enum` or `object`. |

### `types`

Each key under `types` is a JSON Schema `type`. Give a `default`, and
`formats` for specific `format` values:

```yaml
types:
  number:
    formats:
      float: float
      double: double
    default: double
```

With this, `type: number` with `format: float` is a `float`, and `type: number`
with any other format, or none, is a `double`. Custom types let you control
serialization, such as a money type for `format: currency`. A custom type needs
a System.Text.Json converter.

| `type` | Default | Formats |
| :----- | :------ | :------ |
| `boolean` | `bool` | |
| `string` | `string` | `byte`: `byte[]`, `binary`: `Stream`, `date-time`: `DateTimeOffset`, `date`: `DateOnly`, `uuid` and `guid`: `Guid` |
| `number` | `double` | `float`, `double` |
| `integer` | `int` | `int8`: `byte`, `int16`: `short`, `int32`: `int`, `int64`: `long`, `sint8`: `sbyte`, `uint16`: `ushort`, `uint32`: `uint`, `uint64`: `ulong` |
| `object` | (a generated class) | `any`: `JsonNode` |

### Merging

Files are merged in order: built-in defaults, then `OpenApiSchemaOptions`
files, then the item's `Configuration`. A key in a later file replaces the same
key in an earlier one, and other keys are kept.

Lists such as `globalReservedIdentifiers` are merged by position: the first
entry in your file replaces the first default entry, and so on. To change a
list, repeat the whole list.

## Schema extensions

These extensions can be added to OpenAPI documents. The names of the first
three can be changed with `extensions` in the options file.

| Extension | Where | Effect |
| :-------- | :---- | :----- |
| `x-dotnet-mvc-server-controller` | path or operation | The controller the operations go in. The operation's value wins over the path's. |
| `x-dotnet-type-name` | a schema that becomes its own type | The type's name. |
| `x-dotnet-type-namespace` | a schema that becomes its own type | The type's namespace. |
| `x-ps-optional-as-nullable` | an object schema | `true` makes optional properties plain nullable types (`int?`) instead of `IOptional<int>`, so a missing property and `null` can't be told apart. Also read by the TypeScript generator. |

For a schema's type name and namespace, `overrideNames` in the options file
wins, then the `x-dotnet-type-name` and `x-dotnet-type-namespace` extensions,
then the item's `Namespace` or `Link`.

```yaml
components:
  schemas:
    Money:
      x-dotnet-type-name: Amount
      x-dotnet-type-namespace: MyApp.Billing
      type: object
```
