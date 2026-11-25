
# Working with this source code

This package bundles the `OpenApiCodegen.Client.TypeScript` .NET console app
(copied into `dotnet/` at build time) along with TypeScript runtime helpers, and
tests via vitest.

Prerequisites:

    .NET 11 SDK and .NET 10 runtime, Node, pnpm

Build (also regenerates `tests/generated/` from the shared `schemas/`):

    dotnet build

To test:

    dotnet test

To package (output in `artifacts/package/release/`):

    dotnet build -c Release
