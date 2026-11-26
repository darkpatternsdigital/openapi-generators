# Migration guide

What changes in each release, newest first, and what to do when upgrading. Each
section lists the package versions it applies to. When you skip releases, read
every section between your version and the new one.

Releases before C# 0.25.4 and TypeScript 0.13.0 aren't covered.

## Unreleased

**Each output of a file uses its own metadata.** When the same file is listed as
more than one of `OpenApiSchemaMvcServer`, `OpenApiSchemaClient` and
`OpenApiSchemaWebhookClient`, each output now uses its own `Namespace`, `Link`,
`PathPrefix` and `Configuration`. Before, all outputs used the first value set,
in the order `JsonSchemaDocument`, `OpenApiSchemaMvcServer`,
`OpenApiSchemaClient`, `OpenApiSchemaWebhookClient`. If your items for one file
set different values, the controllers or client methods may move to the
namespace their own item names. Models don't move. To keep the old namespace,
set the same `Namespace` (or `Link`) on every item for the file. Nothing changes
for files listed once, or listed with the same metadata on every item.

**A file listed twice as the same item type is generated.** With the same
metadata on both items, it's generated as if listed once; before, nothing was
generated for it. With different `Namespace`, `Link` or `PathPrefix` values,
the build reports `DPD_OPENAPI_CS_002`.

**Status code ranges (`2XX`, `4XX`, …) are no longer ignored.** Before, a
response for a range was dropped, so only documents with ranges change. An
explicit status code still takes precedence over a range, and a range over
`default`.

- MVC server: each range gets a factory named for its class (`Informational`,
  `Successful`, `Redirection`, `ClientError` or `ServerError`) that takes the
  status code, such as `ClientError(404, error)`. It throws
  `ArgumentOutOfRangeException` for a status code outside the range.
- C# client: a status code in a range parses into a return type of the same
  name, with its `Body`, instead of the `default` response or the base return
  type. Code that handled those responses through `OtherStatusCode` or the base
  type should check for the new type.
- TypeScript: a status code in a range keeps its number as `statusCode`
  instead of `'other'`, and its `data` is typed by the range's schema. Code that
  checked for `'other'` to handle those responses should check the status code.
  Generated code imports the new `StatusCodeRange` type, so upgrade the runtime
  package with the generator.

**The MVC server enforces more JSON Schema validation keywords**, so requests
that used to pass can now fail model validation, which returns 400 with
`[ApiController]`. Newly enforced on parameters, request bodies and model
properties:

- `minimum` without `maximum`, and `maximum` without `minimum`. Before, `[Range]`
  was only emitted when both were set.
- OpenAPI 3.0's boolean `exclusiveMinimum` and `exclusiveMaximum`. Before,
  ranges were always inclusive, and `exclusiveMaximum` wasn't recognized at all.
- `multipleOf`, `minItems`, `maxItems`, `uniqueItems`, `minProperties` and
  `maxProperties`.

Inclusive ranges with both bounds still use `[Range]`. One-sided and exclusive
bounds use the new `[Minimum]` and `[Maximum]` attributes, and the other
keywords use `[MultipleOf]`, `[UniqueItems]`, `[MinProperties]` and
`[MaxProperties]`. They all come from Json.Extensions, so **generated code needs
Json.Extensions 0.22.0 or later**. With an older reference, the build reports
`DPDAPI002`. Generated models used by clients get the same attributes.

**OpenAPI 3.0 documents are now validated against the whole OpenAPI 3.0
schema.** Before, only the document's top level was checked, so documents that
break the specification were still generated. Now they are reported as errors
(C# `DPD_JSON_*` diagnostics, which fail the build; the TypeScript generator
prints them and writes no files). Common causes are a response without `description`, an
operation without `responses`, `info` without `title` or `version`, a path
parameter without `required: true`, both `example` and `examples` on the same
object, unknown properties (other than `x-` extensions), and values of the
wrong type, such as `email: 5`. Fix the document to match the specification.

**C# and TypeScript clients send cookie parameters.** Parameters with
`in: cookie` are sent in a `Cookie` header, as `name=value` pairs joined with
`; ` and with values URL-encoded. They were accepted but dropped before. If you
added these cookies yourself, such as through a `CookieContainer` or a fetch
wrapper, stop, or the server may receive them twice. Browsers don't let fetch
set `Cookie`, so there the header is still left out.

**TypeScript's `HttpMethod` includes `'TRACE'`.** Code that switches over every
`HttpMethod` needs a `'TRACE'` case.

**Optional request bodies are `IOptional<T>` in C#.** A request body is
optional unless it sets `required: true`, which is the OpenAPI default. Before,
every body was treated as required. Now an optional body is an `IOptional<T>`
in MVC server methods, C# clients and webhook clients, so code for operations
whose `requestBody` doesn't set `required: true` stops compiling until it's
updated:

- MVC servers: change the override's parameter to `IOptional<T>`. A request
  without the body reaches it as `null`; before, it got 415.
- C# clients: pass the body as `Optional.Create(body)`, or `null` to send none.

If the body is meant to be required, set `required: true` in the document
instead, and nothing changes.

**MVC servers read `text/*` and binary request bodies themselves.** A `text/*`
body with a `string` schema arrives as the request's text, and a non-JSON body
with a `format: binary` schema arrives as `Request.Body`. They returned 415
unless an input formatter was registered for them; such formatters aren't used
any more. When the body is required, a request without a `Content-Type` still
gets 415.

**MVC server methods for operations without an `operationId` are named from
the method and path.** An operation with a request body and no `operationId`
generated an abstract method named `_`. It's now named like its action result,
such as `PostWidgets`. Rename the override.

**C# clients send text and binary request bodies as declared.** A `text/*` body
with a `string` schema is sent as text rather than as a quoted JSON string, and
a binary (`Stream`) body is sent with its `Content-Type`. This applies to
webhook clients too.

**TypeScript clients send text and binary request bodies.** `text/plain` is now
in the default `allowedMimeTypes`, so operations that accept it get that request
body alongside any others. If you set `allowedMimeTypes` with fewer than four
entries, `text/plain` is added too, since lists merge by position. `text/*`
bodies are typed `string` and other non-JSON bodies `Blob`; they used to throw
"Non-JSON requests not supported". The fetch and RxJS adapters send them as
given, and send no body when an optional body is left out, rather than
`"null"`. The fetch adapter's `FetchRequest.body` is now
`URLSearchParams | string | Blob | undefined`, so custom fetch implementations
typed against the old type may need updating. The MSW adapter matches text
bodies by their text, `Blob` bodies by their bytes, and a left-out optional
body as an empty request, instead of parsing every body as JSON.

## C# 0.32.0, Json.Extensions 0.21.0, TypeScript 0.16.0, fetch 0.12.0, RxJS 0.11.0, MSW 0.11.0

**The C# generator needs Roslyn 4.8 or later**, which ships with the .NET 8 SDK
and Visual Studio 2022 17.8. The builds for Roslyn 3.11, 4.0 and 4.1 are gone.
With an older compiler, such as the .NET 6 or 7 SDK or an older Visual Studio,
no code is generated and the build fails on the missing types. Update the SDK
or Visual Studio.

**Failures to load the C# generator are reported as `DPDAPI003`**, with the
whole exception. Before, Roslyn reported that it couldn't create the generator,
without the exception's details.

**The TypeScript generator needs the .NET 10 runtime or later**, both from npm
and as a .NET tool. It needed .NET 8 before. It also runs on later runtimes,
such as .NET 11, without .NET 10 installed. Update CI images and developer
machines that only have the .NET 8 runtime.

**The fetch, RxJS and MSW adapters require TypeScript 0.16.0**, so upgrade them
together. Nothing else changes in them.

**Json.Extensions targets .NET Standard 2.0, .NET 8 and .NET 10.** Projects on
.NET 6, .NET 7 or .NET Standard 2.1 use the .NET Standard 2.0 build. Nothing
needs to change.

## C# 0.31.2

**Generator failures are reported as `DPDAPI003`.** When code generation throws,
the build reports warning `DPDAPI003` with the whole exception, including inner
exceptions and stack traces, instead of Roslyn's `CS8785`, which only gave the
exception's type and message. Builds that suppress `CS8785` should suppress
`DPDAPI003` too.

## C# 0.31.1

**Default namespace for items without `Link` or `Namespace`.** An item inside
the project folder with neither `Link` nor `Namespace`, such as
`<OpenApiSchemaMvcServer Include="Api/petstore.yaml" />`, generated code with an
empty namespace that didn't compile. Its namespace is now the root namespace
plus the file's folders (`MyApp.Api`), as documented. Items that set `Link` or
`Namespace` aren't affected.

## C# 0.31.0, TypeScript 0.15.0, fetch 0.11.0, RxJS 0.10.0

**Clients read non-JSON responses.** Before, the clients parsed response bodies
as JSON, which threw for anything else.

- C#: a `text/*` response has a `string` `Body`. Other non-JSON responses,
  including binary types, have an `HttpContent` `Body`. **A binary response
  that declared a `Stream` `Body` now declares `HttpContent`;** call
  `ReadAsStreamAsync()` on it. `application/xml` responses, which used to be
  returned as the base `…ReturnType`, now have their own subtype, so `switch`
  statements over the result may need new cases.
- TypeScript: `text/*` data is a `string`, and other non-JSON data is a `Blob`.
  Responses of every media type now appear in the result types
  (`allowedMimeTypes` only limits request bodies), so code that switches over
  the responses may need new cases.

**Response media types match more reliably.** In C#, media types match
regardless of case, ranges such as `text/*` in the document work, and a response
without a `Content-Type` no longer throws. In TypeScript,
`application/json; charset=utf-8` now matches `application/json`, `mimeType`
is the media type without parameters, and in the fetch and RxJS adapters an
empty body with a JSON `Content-Type` is treated as no body.

## TypeScript 0.14.1, MSW 0.10.0

**Responses without a body are typed `undefined`.** For a response with no
content, such as a 204, `data` was typed `unknown` and is now `undefined`. Code
that used it never received a value, so only types change.

**The MSW adapter mocks more kinds of responses.**

- Responses without a body, such as
  `{ statusCode: 204, mimeType: '', data: undefined }`. Before, every response
  was sent as JSON.
- Non-JSON responses, sent as given with their `mimeType` as the
  `Content-Type`.
- Response `headers`, which were ignored.
- Default (`'other'`) responses, sent with the new `status` property, or 500
  if it isn't set. Before, they used status 0, which failed.

## C# 0.30.0, Json.Extensions 0.20.0, TypeScript 0.14.0, fetch 0.10.0, MSW 0.9.1

**Upgrade `DarkPatterns.OpenApiCodegen.Json.Extensions` to 0.20.0** along with
the C# package. With an older version, the build reports warning `DPDAPI002`
and generated code may not compile.

**C# clients format parameter and form values for OpenAPI.** Path, query,
header and form values no longer depend on the current culture: dates are ISO
8601, numbers use a decimal point, booleans are lowercase, enums send their
OpenAPI value, `format: byte` values are base64, and a space in a path segment
is sent as `%20` rather than `+`. Optional form fields and headers that aren't
set are left out instead of throwing. Servers that accepted the old formats by
accident may need checking.

**C# webhook clients send query parameters.** They were dropped before.

**TypeScript clients send forms URL-encoded.** Form bodies were sent as
multipart data under a URL-encoded `Content-Type`, which servers couldn't parse.
They're now `URLSearchParams`. A `null` value is sent empty, not as the string
`"null"`. The fetch adapter takes `mimeType` for non-JSON request bodies, like
the RxJS adapter.

## C# 0.29.1

**Optional form fields bind.** Optional fields in
`application/x-www-form-urlencoded` request bodies failed MVC model binding.
They now arrive as `IOptional` values, and an empty nullable field arrives as
present with a `null` value.

## C# 0.29.0

**New `OpenApiSchemaModels` item** for OpenAPI documents that only supply
shared schemas. If you registered such a document as `OpenApiSchemaMvcServer`
with a dummy `PathPrefix` just to get model names, switch to:

```xml
<OpenApiSchemaModels Include="shared.yaml" Namespace="MyApp.Shared" />
```

Keep the same `Namespace` or `Link`, and the models keep their names and
namespace. The unused controllers, `AddOpenApi…` method and security policies
for that document are no longer generated, so check nothing referenced them.

## C# 0.28.1, TypeScript 0.13.1

**Source comments use relative paths.** Comments naming the source of
generated code, such as
`// Generated from schema: ../../schemas/api.yaml#/components/schemas/Foo`, are
relative to the project (C#) or to the folder the CLI runs in (TypeScript)
instead of absolute. They're the same on every machine. Checked-in generated
code gets a one-time diff.

## C# 0.28.0

**Generated action results expose what they return.** `UnderlyingResult` is the
`IActionResult` (an `ObjectResult`, a `StatusCodeResult`, or the one passed to
`Unsafe`), and `Headers` holds the headers that will be added. Tests that read
the private `original` and `headers` fields through reflection can use these
instead. The private fields aren't a supported contract.

## C# 0.27.0, TypeScript 0.13.0

**Path-level parameters apply to every operation.** Parameters declared on a
path, rather than on each operation, were ignored. They now appear in every
operation under that path, so controller methods and client calls for those
operations gain parameters. An operation's own parameter with the same name and
location wins.

## C# 0.26.0, Json.Extensions 0.19.0

**Upgrade `DarkPatterns.OpenApiCodegen.Json.Extensions` to 0.19.0 or later.**

**Optional values are validated.** Optional properties and parameters with
`minLength`, `maxLength`, `pattern`, `minimum` or `maximum` now get normal
model validation errors for invalid values. Before, validation threw, or with
`pattern` checked the wrong text. Tests that expected an exception or a 500 for
such input will see a validation failure.

## C# 0.25.4

**C# client header parameters use their OpenAPI name.** Header parameters were
sent with an empty name, which threw. They now work.
