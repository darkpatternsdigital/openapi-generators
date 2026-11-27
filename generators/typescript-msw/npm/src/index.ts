import type {
	HttpHandler,
	DefaultRequestMultipartBody,
	JsonBodyType,
	StrictResponse,
	ResponseResolverReturnType,
} from 'msw';
import { http, HttpResponse } from 'msw';
import type { ResponseResolverInfo } from 'msw/lib/core/handlers/RequestHandler';
import type {
	AdapterRequestArgs,
	HttpMethod,
	RequestBodies,
	RequestConversion,
	StandardResponse,
	TransformCallType,
} from '@darkpatternsdigital/openapi-codegen-typescript';
import {
	isJsonMediaType,
	toMediaType,
} from '@darkpatternsdigital/openapi-codegen-typescript';

function deepEqual(x: unknown, y: unknown): boolean {
	const tx = typeof x,
		ty = typeof y;
	if (!x || !y || tx !== 'object' || tx !== ty) return x === y;

	const xKeys = Object.keys(x),
		yKeys = Object.keys(y);
	return (
		xKeys.length === yKeys.length &&
		xKeys.every((key) =>
			deepEqual(
				(x as Record<string, unknown>)[key],
				(y as Record<string, unknown>)[key],
			),
		)
	);
}

type DefaultBodyType = DefaultRequestMultipartBody | JsonBodyType;
type SafeStandardResponse = StandardResponse<
	number | 'other',
	string,
	DefaultBodyType
>;
type SafeResponseResolverReturnType<TResponses extends SafeStandardResponse> =
	ResponseResolverReturnType<TResponses['data']>;

export type AsyncResponseResolver<TResponses extends SafeStandardResponse> = (
	info: ResponseResolverInfo<Record<string, unknown>>,
) =>
	| SafeResponseResolverReturnType<TResponses>
	| Promise<SafeResponseResolverReturnType<TResponses>>;

export type MswStandardResponse<
	T extends SafeStandardResponse = SafeStandardResponse,
> = {
	statusCode: T['statusCode'];
	mimeType: T['mimeType'];
	data: T['data'];
	headers?: Record<string, unknown>;
	/** The HTTP status for a default (`'other'`) response; 500 when not given */
	status?: number;
};

export function toMswResponse<
	TStatusCode extends number | 'other' = number | 'other',
	TMimeType extends string = string,
	TBody extends DefaultBodyType = undefined,
>(
	response: MswStandardResponse<
		StandardResponse<TStatusCode, TMimeType, TBody>
	>,
): StrictResponse<TBody> {
	const status =
		typeof response.statusCode === 'number'
			? response.statusCode
			: response.status ?? 500;
	const headers: Record<string, string> = {};
	for (const [key, value] of Object.entries(response.headers ?? {}))
		if (value !== undefined && value !== null) headers[key] = String(value);

	// No content (e.g. 204): an empty body
	if (!response.mimeType)
		return new HttpResponse(null, {
			status,
			headers,
		}) as StrictResponse<TBody>;
	if (isJson(response.mimeType))
		return HttpResponse.json<TBody>(response.data, { status, headers });
	// Any other media type: the body as given (a string, Blob, ...)
	return new HttpResponse(response.data as ResponseBody, {
		status,
		headers: { 'Content-Type': response.mimeType, ...headers },
	}) as StrictResponse<TBody>;
}

type ResponseBody = ConstructorParameters<typeof HttpResponse>[0];

function isJson(mimeType: string) {
	return /^application\/([^;]+\+)?json$/i.test(mimeType.split(';')[0].trim());
}

function buildHandler<
	TBody extends DefaultBodyType,
	TResponses extends StandardResponse<number | 'other', string, TBody>,
>(
	request: AdapterRequestArgs,
	response: AsyncResponseResolver<TResponses> | MswStandardResponse<TResponses>,
	options?: RequestHandlerOptions,
) {
	const method = request.method.toLowerCase() as Lowercase<HttpMethod>;
	// MSW has no http.trace, so TRACE matches every method and handleMswFullRequest checks it
	const methodFunc = method === 'trace' ? http.all : http[method];
	const url = new URL(
		request.path.split('?')[0],
		options?.baseDomain,
	).toString();
	return methodFunc<
		any,
		any,
		TResponses['data'] extends DefaultBodyType
			? TResponses['data']
			: DefaultBodyType
	>(url, (info) => handleMswFullRequest(info, request, response), options);
}

async function handleMswFullRequest<
	TBody extends DefaultBodyType,
	TResponses extends StandardResponse<number | 'other', string, TBody>,
>(
	info: ResponseResolverInfo<Record<string, unknown>>,
	request: AdapterRequestArgs,
	response: AsyncResponseResolver<TResponses> | MswStandardResponse<TResponses>,
): Promise<SafeResponseResolverReturnType<TResponses>> {
	if (info.request.method !== request.method) {
		return;
	}
	if (!queryStringMatch(request, info.request)) {
		return;
	}
	if ('body' in request) {
		if (request.body instanceof URLSearchParams) {
			// application/x-www-form-urlencoded
			const currentBody = new URLSearchParams(
				await info.request.clone().text(),
			);
			if (!formFieldsEqual(request.body, currentBody)) {
				return;
			}
		} else if (!(await bodyMatches(request, info.request))) {
			return;
		}
	} else if (info.request.bodyUsed) {
		return;
	}
	// TODO: request headers aren't checked

	if (typeof response === 'function') return response(info);
	return toMswResponse(response);
}

/** No Content-Type means no body; JSON is compared as values, a Blob by its bytes, and anything else as text */
async function bodyMatches(
	request: AdapterRequestArgs,
	actual: Request,
): Promise<boolean> {
	const contentType = request.headers?.['Content-Type'];
	if (!contentType) return (await actual.clone().text()) === '';
	if (isJsonMediaType(toMediaType(contentType)))
		return deepEqual(request.body, await actual.clone().json());
	// A Blob (checked by shape, since the global Blob type may be replaced)
	const blob = request.body as { arrayBuffer?: () => Promise<ArrayBuffer> };
	if (typeof blob?.arrayBuffer === 'function') {
		const expected = new Uint8Array(await blob.arrayBuffer());
		const received = new Uint8Array(await actual.clone().arrayBuffer());
		return (
			expected.length === received.length &&
			expected.every((byte, index) => byte === received[index])
		);
	}
	return String(request.body) === (await actual.clone().text());
}

function formFieldsEqual(x: URLSearchParams, y: URLSearchParams): boolean {
	const fields = (p: URLSearchParams) => {
		const result: string[] = [];
		p.forEach((value, key) => result.push(`${key}=${value}`));
		return result.sort().join('&');
	};
	return fields(x) === fields(y);
}

function queryStringMatch(thisRequest: AdapterRequestArgs, request: Request) {
	return (
		new URL(request.url).searchParams.toString() ==
		(thisRequest.path.split('?')[1] ?? '')
	);
}

type RequestHandlerOptions = {
	once?: boolean;
	baseDomain?: string;
};

type MatchedRequest<
	TRequestParams extends {} = {},
	TRequestBodies extends RequestBodies = RequestBodies,
	TCallType extends TransformCallType = TransformCallType,
> =
	| (TCallType extends 'no-body' | 'optional'
			? { params: TRequestParams; body?: never; mimeType?: never }
			: never)
	| (TCallType extends 'body' | 'optional'
			? {
					[K in keyof TRequestBodies]: {
						params: TRequestParams;
						body?: TRequestBodies[K];
						mimeType?: K;
					};
				}[keyof TRequestBodies]
			: never);

export function toMswHandler<
	TMethod extends HttpMethod,
	TUrlParams extends {},
	TRequestParams extends TUrlParams,
	TRequestBodies extends RequestBodies,
	TBody extends DefaultBodyType,
	TResponses extends StandardResponse<number | 'other', string, TBody>,
	TCallType extends TransformCallType,
>(
	conversion: RequestConversion<
		TMethod,
		TUrlParams,
		TRequestParams,
		TRequestBodies,
		TResponses,
		TCallType
	>,
	defaultOptions?: RequestHandlerOptions,
) {
	type MyRequest = MatchedRequest<TRequestParams, TRequestBodies, TCallType>;

	return function (
		request: MyRequest,
		response:
			| AsyncResponseResolver<TResponses>
			| MswStandardResponse<TResponses>,
		options?: RequestHandlerOptions,
	): HttpHandler {
		const standardRequest: AdapterRequestArgs =
			'body' in request
				? conversion.request(
						request.params,
						request.body as TRequestBodies[keyof TRequestBodies],
						request.mimeType as keyof TRequestBodies,
					)
				: {
						method: conversion.method,
						path: conversion.url(request.params),
						// TODO: headers support?
					};
		return buildHandler(standardRequest, response, {
			...defaultOptions,
			...options,
		});
	};
}
