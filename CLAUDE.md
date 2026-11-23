# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

OpenAPI code generators (C# source generator, plus TypeScript generators shipped via npm). The generators are written in C#; the TypeScript npm packages wrap a .NET console app and add runtime helpers. Everything is driven by MSBuild — `openapi-codegen.sln` includes the npm packages as projects (via the `DarkPatterns.Build.Pnpm` SDK), so `dotnet build`/`dotnet test` also build and test the TypeScript side.

Prereqs: .NET 8 SDK (`global.json`), Node 20 (`.nvmrc`), pnpm 8 (`packageManager` in `package.json`).

## Commands

```sh
pnpm install                         # JS workspace deps (workspace = every package.json in the repo)
dotnet build                         # builds C# libs, generators, and npm packages
dotnet build -c Release              # Release treats warnings (incl. code analysis) as errors — matches CI
dotnet test                          # all tests: xUnit projects + vitest (npm projects run vitest through VSTest)

# Single test project / single test
dotnet test lib/OpenApiCodegen.Server.Mvc.Test
dotnet test lib/OpenApiCodegen.Server.Mvc.Test --filter "FullyQualifiedName~ComprehensiveTransformsShould"

# JS/TS lint (CI runs `dotnet build -t:PrepareLint` first so generated test code exists)
dotnet build -t:PrepareLint
pnpm lint                            # tsc -b + eslint + prettier
pnpm lint:fix
```

## Before suggesting a commit

Run each CI check (`.github/workflows/`) that your change could break, and say which ones you ran. What each check catches:

- **Release build** (`dotnet build -c Release`): warnings, including code analysis, are errors. Any change to C#, templates, `.props`/`.targets`, or `schemas/` can break it, because TestApp and the npm projects generate code from schemas during the build.
- **Tests** (`dotnet test -c Release`, or just the affected project): behavior changes in parsers, transforms, templates, or schemas.
- **JS lint** (`dotnet build -t:PrepareLint`, then `pnpm lint`):
  - `tsc -b` and eslint cover TS sources plus generated test code, so changes to TS templates or generators can break them.
  - Prettier covers every `*.{cjs,mjs,js,mts,cts,ts,tsx,yaml,json}` in the repo, including `.claude/settings.json` and `schemas/*.yaml`, and expects tab indentation. `pnpm lint:fix` repairs formatting.
- **Version sync** (`git diff --exit-code` after the build): when you change a version property in `Directory.Build.props`, the build rewrites the matching npm `package.json` `version`. Commit the rewritten files.

All of these run in one CI job (`dotnet-build.yml`). CI skips changes that only touch Markdown, `adr/` or `docs/`, except `AnalyzerReleases.*.md`, which is a build input.

`dotnet format` isn't a PR check. Its workflow is disabled because on .NET 8 it can't see source-generated types.

## Architecture

Pipeline: **load documents → parse/validate into abstractions → transform to source files via Handlebars → emit** (Roslyn source generator for C#, files on disk for TS).

- `lib/Json.Specifications` — document/schema registry (`DocumentRegistry`, `SchemaRegistry`), URI-based node resolution, JSON Schema dialect matching, and the diagnostics model (`DiagnosableResult`, `DiagnosticException`). Every node is addressed by URI + JSON Pointer; see `adr/01-system-text-json-refactor.md` for the design rationale.
- `lib/Json.Loaders` — YAML → `JsonNode` loading.
- `lib/OpenApi.Abstractions` — version-independent OpenAPI object model (modeled on the latest spec) plus a visitor (`OpenApiDocumentVisitor`).
- `lib/OpenApi.Specifications.v3_0` — OpenAPI 3.0 parser/document factory that adapts raw JSON nodes into the abstractions. Only OpenAPI 3.0 is supported; `lib/OpenApi.Transformations/Specifications/OpenApi3_1` is incomplete.
- `lib/OpenApi.Transformations` — shared transform infrastructure: `ISourceProvider` composition (`CompositeOpenApiSourceProvider`, `SafeSourceProvider`), operation/path-controller grouping transformers, and diagnostic conversion.
- `lib/OpenApi.CSharp`, `lib/OpenApi.TypeScript` — language-specific schema → model generation (naming, type info, inline schemas) with Handlebars templates in `Templates/` and default option files (`csharp.config.yaml`, `typescript.config.yaml`) that are embedded and overridable by user options.
- `lib/OpenApiCodegen.Handlebars` — shared Handlebars setup and template-processing base.
- `lib/OpenApiCodegen` — `IOpenApiCodeGenerator` and `OptionsLoader` (merges default + user YAML options).

Generators:
- `generators/csharp/OpenApiCodegen.CSharp.Base` — `CSharpGenerator` with three outputs: `MvcServer` (abstract controllers, `AddServices`, security policies), `Client` (extension methods), `WebhookClient`.
- `generators/csharp/OpenApiCodegen.CSharp.Analyzers` — the Roslyn source generator, built three times against Roslyn 3.11 / 4.0 / 4.1 (`Analyzer.targets`). It loads the Base assembly by name rather than referencing types directly. Analyzer diagnostics must be recorded in `AnalyzerReleases.Unshipped.md`.
- `generators/csharp/OpenApiCodegen.CSharp` — the NuGet package; its `.props` maps the user-facing MSBuild items (`OpenApiSchemaMvcServer`, `OpenApiSchemaClient`, `OpenApiSchemaWebhookClient`, `JsonSchemaDocument`, `OpenApiSchemaOptions`) and metadata (`PathPrefix`, `SchemaId`, `Namespace`, …) onto `AdditionalFiles` with `DPD*` metadata the generator reads.
- `generators/typescript/OpenApiCodegen.Client.TypeScript` — .NET console app (`Program.cs`) that writes TS operation files + models. Also packed as a .NET tool (`openapi-codegen-typescript`), so CLI changes ship via both NuGet and npm.
- `generators/typescript/npm` (`@darkpatternsdigital/openapi-codegen-typescript`) — on build, publishes the console app into `dotnet/` and exposes it via `bin/index.js` (which spawns `dotnet <dll>`), plus runtime helpers in `src/`. Its csproj regenerates `tests/generated/<schema>/` from `schemas/*.yaml` before tests/lint.
- `generators/typescript-fetch`, `typescript-rxjs`, `typescript-msw` — thin runtime adapters on top of the base TS package; each also generates test code from shared schemas.

Lib-level shared assemblies (Json.*, OpenApi.*) are versioned together; bump `SharedAnalyzerLibrariesVersion` in `Directory.Build.props` whenever any internal library changes.

Consumers reference `OpenApiCodegen.Json.Extensions` themselves; the C# package doesn't depend on it. When C# templates start using a newer `Json.Extensions` API, raise `MinimumJsonExtensionsVersion` in `OpenApiCSharpGenerator.cs`, so older references get warning `DPDAPI002` instead of a compile error.

## Testing conventions

- `schemas/` holds the shared OpenAPI fixtures used by every test suite. Adding a feature usually means adding a schema here and wiring it into the relevant consumers.
- `lib/TestApp` is a real ASP.NET app that consumes the source generator (referenced as an analyzer) on many schemas; `lib/TestApp.Test` exercises it over HTTP. Generated files are emitted to `lib/TestApp/generated/` for inspection. `lib/TestApp.Bad` covers schemas that should produce diagnostics.
- `lib/TestApp.Test/GeneratedCodeShould` snapshots everything in `lib/TestApp/generated/` with [Verify](https://github.com/VerifyTests/Verify), replacing the generator version with a placeholder. Any change to generated output fails it and leaves `*.received.cs` files next to the `*.verified.cs` snapshots. Review the diff, then rename the received files to `.verified.cs`. `generated/` isn't cleaned automatically, so delete it before rebuilding if outputs were removed or renamed.
- The "Compatibility fixtures" item group in `lib/TestApp`'s csproj recreates MSBuild configuration patterns from real-world projects. Expect their snapshots to change only when a change is meant to affect existing users.
- `lib/OpenApiCodegen.Server.Mvc.Test/ComprehensiveTransformsShould` generates and dynamically compiles each schema; add new schemas to its `InlineData` list when they're added to the TestApp.
- `lib/OpenApiCodegen.Client.TypeScript.Test` generates TS and runs it in `testing/` with `pnpm exec ts-node` (ts-node is pinned in `testing/package.json`; it type-checks, which tests like `CheckModel` rely on), so it needs pnpm but not a global Node.
- `generators/SourceGenerators.Test` checks diagnostics reported by the Roslyn generator.
- `examples/` are separate solutions that consume the built NuGet packages (CI runs `examples/dotnetcore-server-interfaces/tooling/build-examples.ps1` on PRs).

## Versioning and packaging

- NuGet dependency versions live in `Directory.Packages.props` (Central Package Management): add a `PackageVersion` there and a `PackageReference` without `Version` in the project. Exceptions: the analyzers pin Roslyn per build with `VersionOverride` (`eng/roslyn.targets`); `examples/` (its own `Directory.Packages.props`) and the pnpm SDK projects (`ManagePackageVersionsCentrally` false, because the SDK adds a versioned reference) opt out.
- Package versions live in `Directory.Build.props` (`OpenApiCSharpVersion`, `OpenApiTypeScriptClientVersion`, etc.). npm packages take their version from these via `VersionPrefix`; keep each `package.json` `version` consistent.
- Build output goes to `artifacts/` (`UseArtifactsOutput`); packages land in `artifacts/package/release/`. Non-main CI builds publish prerelease packages with a `sha.<commit>` suffix to GitHub Packages (see `docs/testing-github-nuget-packages.md`).
- Assemblies are strong-named when `OpenApiCodegen.snk` exists at the repo root (CI only).
- Error/diagnostic messages are in `Errors.resx` files, compiled via `eng/resx-generation.targets`.
