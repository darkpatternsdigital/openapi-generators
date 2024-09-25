import type {
	RequestBodies,
	RequestConversion,
	StandardResponse,
	TransformCallType,
	AdapterRequestArgs,
	HttpMethod,
	AdapterResponseArgs,
} from '@darkpatternsdigital/openapi-codegen-typescript';
import type {
	AnyObject,
	AnyRequestBodies,
	IsKeylessOrUndefined,
	PropsOptional,
} from './utils';
import { applicationJson } from './utils';

export type FetchRequest = {
	method: HttpMethod;
	headers: Record<string, string> | undefined;

	body: FormData | string;
};

export type FetchResponse = {
	status: number;
	headers: Headers;
	json(): Promise<unknown>;
	body: unknown; // ReadableStream, but may be different for Node vs DOM
};

export type BaseFetchImplementation = (
	url: string | URL,
	requestInit: FetchRequest,
) => Promise<FetchResponse>;
export type FetchImplementation<TExtra> = (
	...params: [...Parameters<BaseFetchImplementation>, TExtra]
) => Promise<FetchResponse>;

function createRequestArgs(
	requestOpts: AdapterRequestArgs,
): Parameters<BaseFetchImplementation> {
	const url = requestOpts.path;

	return [
		url,
		{
			method: requestOpts.method,
			headers: requestOpts.headers
				? Object.fromEntries(
						Object.entries(requestOpts.headers).filter(
							(t): t is [string, string] => t[1] !== null,
						),
					)
				: undefined,
			body:
				requestOpts.headers &&
				requestOpts.headers['Content-Type'] ===
					'application/x-www-form-urlencoded'
					? (requestOpts.body as FormData)
					: JSON.stringify(requestOpts.body),
		},
	];
}

async function createResponseArgs(
	fetchResponse: Awaited<ReturnType<BaseFetchImplementation>>,
): Promise<AdapterResponseArgs> {
	const response = fetchResponse;
	const contentType = response.headers.get('Content-Type') ?? '';
	return {
		status: response.status,
		response:
			contentType.split(';')[0] === applicationJson
				? await response.json()
				: response.body,
		getResponseHeader(header) {
			return response.headers.get(header);
		},
	};
}

export function toFetchOperation<
	TParams extends object,
	TBodies extends RequestBodies,
	TResponses extends StandardResponse,
	TCallType extends TransformCallType,
	TExtra,
>(
	fetchImpl: FetchImplementation<TExtra>,
	conversion: RequestConversion<
		HttpMethod,
		AnyObject,
		TParams,
		TBodies,
		TResponses,
		TCallType
	>,
): FetchOperation<TParams, TBodies, TResponses, TCallType, TExtra> {
	type Result = FetchOperation<TParams, TBodies, TResponses, TCallType, TExtra>;
	type TParameters = Parameters<Result>;
	type TResponse = Awaited<ReturnType<Result>>;
	return async function transform(...[param]: TParameters): Promise<TResponse> {
		type Temp = {
			params?: TParams | undefined;
			body: TBodies[keyof TBodies];
			extra?: TExtra;
		};
		const {
			params = {} as TParams,
			body = undefined as Temp['body'],
			extra,
		} = (param ?? {}) as Temp;
		const requestArgs: AdapterRequestArgs = conversion.request(
			params,
			body,
			(body
				? applicationJson
				: undefined) as keyof AnyRequestBodies as keyof RequestBodies,
		);
		const fetchArgs = createRequestArgs(requestArgs);
		const fetchResponse = await fetchImpl(...fetchArgs, extra as TExtra);
		const adapterResponseArgs = await createResponseArgs(fetchResponse);
		return conversion.response(adapterResponseArgs) as TResponse;
	};
}

type BaseRequestParam<
	TCallType extends TransformCallType,
	TParams,
	TBodies extends RequestBodies,
	TExtra,
> = {
	params: TParams;
	body: TCallType extends 'no-body'
		? undefined
		: TCallType extends 'optional'
			? TBodies['application/json'] | undefined
			: TBodies['application/json'];
	extra: TExtra;
};

export type RequestParam<
	TCallType extends TransformCallType,
	TParams,
	TBodies extends RequestBodies,
	TExtra,
> = PropsOptional<BaseRequestParam<TCallType, TParams, TBodies, TExtra>>;

type FetchOperationParamList<
	TCallType extends TransformCallType,
	TParams,
	TBodies extends RequestBodies,
	TExtra,
> =
	IsKeylessOrUndefined<
		RequestParam<TCallType, TParams, TBodies, TExtra>
	> extends true
		? [req?: RequestParam<TCallType, TParams, TBodies, TExtra>]
		: [req: RequestParam<TCallType, TParams, TBodies, TExtra>];

export type FetchOperation<
	TRequestParams extends object,
	TRequestBodies extends RequestBodies,
	TResponses extends StandardResponse,
	TCallType extends TransformCallType,
	TExtra,
> = (
	...args: FetchOperationParamList<
		TCallType,
		TRequestParams,
		TRequestBodies,
		TExtra
	>
) => Promise<TResponses>;
