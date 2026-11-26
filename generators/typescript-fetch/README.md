# OpenAPI Codegen for a TypeScript-friendly Fetch client

Provides an adapter layer method for
[@darkpatternsdigital/openapi-codegen-typescript][1] to integrate with fetch.

```sh
npm i @darkpatternsdigital/openapi-codegen-typescript
npm i -D @darkpatternsdigital/openapi-codegen-typescript-fetch
```

You must also have the .NET 10 runtime, or a later one, installed on your machine.

This will provide a corresponding bin to generate the typescript files. (See the
[@darkpatternsdigital/openapi-codegen-typescript][1] package for command line usage
details.)

```sh
openapi-codegen-typescript api.yaml api-generated/ -c
```

You can then create an API wrapper such as:

```ts
import { toFetchApi } from '@darkpatternsdigital/openapi-codegen-typescript-fetch';
import operations from './api-generated/operations';

export default toFetchApi(operations, fetch);
```

This API will use the type safety from OpenAPI along with `fetch`.

The Fetch API doesn't allow the `TRACE` method, so calling a `TRACE` operation
throws a `TypeError`.

## Use with `node-fetch`

To use with `node-fetch`, a fully-qualified URL must be provided.  See the following example:

```typescript
import fetch from 'node-fetch';
import { toFetchApi, type FetchImplementation } from '@darkpatternsdigital/openapi-codegen-typescript-fetch';
import operations from './api-generated/operations';

const baseDomain = 'http://localhost/';
const fetchImpl: FetchImplementation<unknown> = (url, params) => {
	return fetch(new URL(url, baseDomain), params);
};
const fetchApi = toFetchApi(operations, fetchImpl);
```

1. Ensure you have a compatible version of `node-fetch` installed.
2. Add a `types.d.ts` file (or other `.d.ts` file to be picked up by TypeScript in your Node sections) to your project with the following global type declarations:

    ```typescript
    type FormData = typeof import('formdata-polyfill/esm.min.js').FormData;
    type Headers = import('node-fetch').Headers;
    type Blob = NodeJS.ReadableStream;
    ```

[1]: https://www.npmjs.com/package/@darkpatternsdigital/openapi-codegen-typescript

## Documentation

- [Getting started](https://github.com/darkpatternsdigital/openapi-generators/blob/main/docs/getting-started/typescript.md)
- [CLI arguments and options](https://github.com/darkpatternsdigital/openapi-generators/blob/main/docs/reference/typescript-cli.md)
- [Migration guide](https://github.com/darkpatternsdigital/openapi-generators/blob/main/docs/migration.md)
