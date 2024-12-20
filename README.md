# OpenAPI Codegen for principled development

Collaboration with APIs between frontend and backend developers can be tough. In
many of the best scenarios, the schema gets discussed up front, both teams go
their own way thinking they understand the direction, and inevitably they need
to "true up" at the end, making adjustments. In other situations, frontend devs
work around not knowing what the API will look like and finish the last touches
when the backend devs deliver a working API, even if it is just a prototype.
Many developers use tools such as OpenAPI and Swagger to accomplish such a task.

However, there is a better way. OpenAPI supports a YAML specification for
writing an API collaboratively. There are [many][1] [OpenAPI][2] [editors][3]
that let you design the API up front. And there are [code generators][4] that
allow you to generate either your server or client. This way, during those
up-front design sessions with frontend and backend developers, you can end with
a real API specification and both teams working towards that, with either team
making the mapping layer as needed.

# So, why these packages?

We've found that many of the code generators create "finished" code - they don't
make it easy to iterate on the API. While this might be great in some scenarios,
such as a versioned API, it doesn't work for many other scenarios, such as a
frontend and backend deployed together when you don't need to support old
versions of the API.

These generators are designed to be fast and lightweight such that you can
include them with your CI process in order to build your API client or your API
server interfaces and ensure that any changes to the API get detected by both
sides.

Generators currently available:

* ![DarkPatterns.OpenApiCodegen.CSharp NuGet](https://img.shields.io/nuget/v/DarkPatterns.OpenApiCodegen.CSharp)
  [C# Source generation from OpenAPI](./generators/csharp), including .NET Core Server MVC Interfaces for the modern C# MVC server approach and .NET Standard Client extension methods.
* ![DarkPatterns.OpenApiCodegen.Json.Extensions NuGet](https://img.shields.io/nuget/v/DarkPatterns.OpenApiCodegen.Client)
  [OpenAPI Codegen Extensions](./lib/OpenApiCodegen.Json.Extensions/), a set of System.Text.Json-compatible extensions supporting Optional parameters and enum serialization.
* ![@darkpatternsdigital/openapi-codegen-typescript at npm](https://img.shields.io/npm/v/@darkpatternsdigital/openapi-codegen-typescript)
  [TypeScript](./generators/typescript), for a TypeScript client that is agnostic about how you fetch data.
* ![@darkpatternsdigital/openapi-codegen-typescript-fetch at npm](https://img.shields.io/npm/v/@darkpatternsdigital/openapi-codegen-typescript-fetch)
  [TypeScript Fetch](./generators/typescript-fetch), for a TypeScript client that leverages the fetch API.
* ![@darkpatternsdigital/openapi-codegen-typescript-rxjs at npm](https://img.shields.io/npm/v/@darkpatternsdigital/openapi-codegen-typescript-rxjs)
  [TypeScript RXJS](./generators/typescript-rxjs), for a TypeScript client that leverages RxJS.
* ![@darkpatternsdigital/openapi-codegen-typescript-msw at npm](https://img.shields.io/npm/v/@darkpatternsdigital/openapi-codegen-typescript-msw)
  [TypeScript MSW](./generators/typescript-msw), for setting up MSW in a strongly-typed way to mock an API.


[1]: https://editor.swagger.io/
[2]: https://mermade.github.io/openapi-gui/
[3]: https://openapi.tools/#gui-editors
[4]: https://github.com/OpenAPITools/openapi-generator
