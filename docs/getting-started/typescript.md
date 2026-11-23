# Getting started: TypeScript

The TypeScript generator writes a client that doesn't depend on any HTTP
library: each operation describes how to build its request and read its
responses. An adapter package then turns the operations into functions for
fetch or RxJS, or into MSW handlers for tests.

The generator is a .NET program, so it needs the .NET 8 runtime whichever way you
install it.

## 1. Install the generator

Choose one.

### From npm

```sh
npm i @darkpatternsdigital/openapi-codegen-typescript
```

The package provides the `openapi-codegen-typescript` command and the runtime
helpers the generated code imports. Run the command from a package script or
with `npx`.

### As a .NET tool

```sh
dotnet tool install --global DarkPatterns.OpenApiCodegen.Client.TypeScript
```

To pin the version for a repository instead, use a local tool:

```sh
dotnet new tool-manifest   # once per repository
dotnet tool install DarkPatterns.OpenApiCodegen.Client.TypeScript
dotnet tool run openapi-codegen-typescript api.yaml api-generated/ -c
```

The tool provides the same `openapi-codegen-typescript` command. The generated
code still imports `@darkpatternsdigital/openapi-codegen-typescript` at
runtime, so add the npm package to your app as well, at the same version as the
tool.

## 2. Generate the client

```sh
openapi-codegen-typescript api.yaml api-generated/ -c
```

This reads `api.yaml` and writes the operations and models to `api-generated/`.
`-c` clears the folder first, so removed operations don't linger. The folder
gets a `.gitignore`, because the intended workflow is to check in only the
OpenAPI document and generate the client during the build. See
[CLI arguments and options](../reference/typescript-cli.md).

## 3. Use an adapter

Install the adapter for how you call the API.

### fetch

```sh
npm i @darkpatternsdigital/openapi-codegen-typescript-fetch
```

```ts
import { toFetchApi } from '@darkpatternsdigital/openapi-codegen-typescript-fetch';
import operations from './api-generated/operations';

export const api = toFetchApi(operations, fetch);

const response = await api.findPets({ params: { tags: ['dog'], limit: 10 } });
if (response.statusCode === 200) {
	// response.data is typed from the 200 response
}
```

### RxJS

```sh
npm i @darkpatternsdigital/openapi-codegen-typescript-rxjs
```

```ts
import { toRxjsApi } from '@darkpatternsdigital/openapi-codegen-typescript-rxjs';
import operations from './api-generated/operations';

export const api = toRxjsApi(operations, 'https://example.com/');
```

### MSW (tests)

```sh
npm i -D @darkpatternsdigital/openapi-codegen-typescript-msw
```

```ts
import { setupServer } from 'msw/node';
import { toMswHandler } from '@darkpatternsdigital/openapi-codegen-typescript-msw';
import operations from './api-generated/operations';

const findPets = toMswHandler(operations.findPets, { baseDomain: 'http://localhost/' });
const server = setupServer(
	findPets(
		{ params: { tags: ['dog'], limit: 10 } },
		{ statusCode: 200, data: [{ id: 1234, name: 'Fido', tag: 'dog' }], mimeType: 'application/json' },
	),
);
```

Each adapter's README covers more options, such as using `node-fetch` or
running RxJS in Node.

## Next steps

- [CLI arguments and options](../reference/typescript-cli.md)
