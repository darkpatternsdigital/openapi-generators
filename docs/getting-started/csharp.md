# Getting started: C#

`DarkPatterns.OpenApiCodegen.CSharp` is a Roslyn source generator. It reads
OpenAPI documents listed in your project file and generates code during the
build, so nothing is checked in and the code always matches the document.

## Requirements

- .NET 8 SDK or later, with C# 12 or later (the default for .NET 8)
- Roslyn 4.8 or later (the .NET 8 SDK, or Visual Studio 2022 17.8 or later)
- System.Text.Json
- A reference to `DarkPatterns.OpenApiCodegen.Json.Extensions`, which the
  generated code uses at runtime

## 1. Add the packages

```xml
<ItemGroup>
	<PackageReference Include="DarkPatterns.OpenApiCodegen.Json.Extensions" Version="0.22.0" />
	<PackageReference Include="DarkPatterns.OpenApiCodegen.CSharp" Version="0.34.0" PrivateAssets="All" />
</ItemGroup>
```

`PrivateAssets="All"` keeps the generator from becoming a dependency of your
package. The generator checks the `Json.Extensions` version: warning `DPDAPI001`
means the reference is missing, and `DPDAPI002` means it's older than the
generated code needs.

## 2. Add an OpenAPI document

Add the document to the project with the item for what you want generated:

```xml
<ItemGroup>
	<OpenApiSchemaMvcServer Include="Api/petstore.yaml" />
</ItemGroup>
```

In Visual Studio, you can instead set the file's build action to
`OpenApiSchemaMvcServer`.

The generated types go in a namespace made of the project's root namespace and
the file's folder, `MyProject.Api` in this example. To choose a different one,
add `Namespace` or `Link` metadata. See
[how namespaces are chosen](../reference/csharp-msbuild.md#namespaces).

| Item | Generates |
| :--- | :-------- |
| `OpenApiSchemaMvcServer` | Abstract controllers, models, an `AddOpenApi…` service registration method and security policies |
| `OpenApiSchemaClient` | Extension methods on `HttpMessageInvoker` (and so `HttpClient`) for each operation, and models |
| `OpenApiSchemaWebhookClient` | Extension methods for calling the document's webhooks and callbacks, and models |
| `OpenApiSchemaModels` | Only the models in `components/schemas`, for documents shared by other documents |
| `JsonSchemaDocument` | Nothing by itself; registers a file referenced with `$ref`, which every such file needs, and gives its types their own namespace |

## 3. Implement a server

Operations are grouped by path into abstract controllers. To choose the
grouping and the controller names, use the `x-dotnet-mvc-server-controller`
extension. Inherit from the base class and implement each operation:

```csharp
public class PetsController : PetsControllerBase
{
	protected override async Task<FindPetsActionResult> FindPets(IEnumerable<string>? tags, int? limit)
	{
		var pets = await LoadPets(tags, limit);
		return FindPetsActionResult.Ok(pets);
	}
}
```

The nested `…ActionResult` type has a factory method for each documented
response, so only documented status codes and bodies can be returned. Use
`Unsafe` to return any other `IActionResult`. Request bodies, parameters and
validation attributes come from the document, and ASP.NET Core model validation
applies them.

Register the controllers with the generated method, named after the document's
title (`Swagger Petstore` here). It takes one type parameter per generated
controller, so when the document gains a controller, the build fails until you
implement it:

```csharp
builder.Services.AddOpenApiSwaggerPetstore<PetsController>();
```

## 4. Call an API

With `OpenApiSchemaClient`, each operation becomes an extension method on
`HttpMessageInvoker`. Set the `HttpClient`'s `BaseAddress` to the server's base
URL, ending in `/`:

```csharp
var result = await httpClient.FindPets(tags: ["dog"], limit: 10);
switch (result)
{
	case Operations.FindPetsReturnType.Ok { Body: var pets }:
		// pets is typed from the 200 response
		break;
	default:
		// any other response; result.Response is the HttpResponseMessage
		break;
}
```

Each operation also has an overload that builds the `HttpRequestMessage` and a
`Parse…` method for the `HttpResponseMessage`, for when you need to send the
request yourself.

## Next steps

- [MSBuild items and metadata](../reference/csharp-msbuild.md)
- [Options and schema extensions](../reference/csharp-options.md)
- To see generated code, set `EmitCompilerGeneratedFiles` to `true` in the
  project. The files are written under `obj/`.
