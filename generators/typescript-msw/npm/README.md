# OpenAPI Codegen for TypeScript-friendly MSW testing

Provides an adapter layer method for
[@darkpatternsdigital/openapi-codegen-typescript][1] to integrate with msw.

```sh
npm i @darkpatternsdigital/openapi-codegen-typescript
npm i -D @darkpatternsdigital/openapi-codegen-typescript-msw
```

You must also have the .NET 10 runtime, or a later one, installed on your machine.

This will provide a corresponding bin to generate the typescript files. (See the
[@darkpatternsdigital/openapi-codegen-typescript][1] package for command line usage
details.)

```sh
openapi-codegen-typescript api.yaml api-generated/ -c
```

You can then create a mock MSW service to handle specific requests:

```ts
import { setupServer } from 'msw/node';
import { toMswHandler } from '@darkpatternsdigital/openapi-codegen-typescript-msw';
import operations from './api-generated/operations';

const baseDomain = 'http://localhost/';
const findPets = toMswHandler(operations.findPets, { baseDomain });
const server = setupServer(
    findPets(
        { params: { tags: ['dog','cat'], limit: 10 } },
        { statusCode: 200, data: [{ name: 'Fido', tag: 'dog', id: 1234 }], mimeType: 'application/json' }
    )
);
```

This API will use the type safety from OpenAPI along with msw.

MSW can't mock `TRACE` operations: it builds a Fetch API `Request`, which
doesn't allow the `TRACE` method.

[1]: https://www.npmjs.com/package/@darkpatternsdigital/openapi-codegen-typescript

## Documentation

- [Getting started](https://github.com/darkpatternsdigital/openapi-generators/blob/main/docs/getting-started/typescript.md)
- [CLI arguments and options](https://github.com/darkpatternsdigital/openapi-generators/blob/main/docs/reference/typescript-cli.md)
- [Migration guide](https://github.com/darkpatternsdigital/openapi-generators/blob/main/docs/migration.md)
