import type {
	RequestBodies,
	RequestConversion,
	StandardResponse,
	TransformCallType,
	AdapterRequestArgs,
	HttpMethod,
	AdapterResponseArgs,
} from '@darkpatternsdigital/openapi-codegen-typescript';
import {
	isJsonMediaType,
	isTextMediaType,
	toMediaType,
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

	body: URLSearchParams | string;
};

export type FetchResponse = {
	status: number;
	headers: Headers;
	json(): Promise<unknown>;
	text(): Promise<string>;
	blob(): Promise<unknown>;
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
					? (requestOpts.body as URLSearchParams)
					: JSON.stringify(requestOpts.body),
		},
	];
}

async function createResponseArgs(
	fetchResponse: Awaited<ReturnType<BaseFetchImplementation>>,
): Promise<AdapterResponseArgs> {
	const response = fetchResponse;
	const mediaType = toMediaType(response.headers.get('Content-Type'));
	return {
		status: response.status,
		// JSON is parsed, text/* is a string, anything else is a Blob; no Content-Type means no body
		response: !mediaType
			? undefined
			: isJsonMediaType(mediaType)
				? parseJson(await response.text())
				: isTextMediaType(mediaType)
					? await response.text()
					: await response.blob(),
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
			mimeType?: keyof TBodies;
			extra?: TExtra;
		};
		const {
			params = {} as TParams,
			body = undefined as Temp['body'],
			mimeType,
			extra,
		} = (param ?? {}) as Temp;
		const requestArgs: AdapterRequestArgs = conversion.request(
			params,
			body,
			(mimeType ??
				(body
					? applicationJson
					: undefined)) as keyof AnyRequestBodies as keyof RequestBodies,
		);
		const fetchArgs = createRequestArgs(requestArgs);
		const fetchResponse = await fetchImpl(...fetchArgs, extra as TExtra);
		const adapterResponseArgs = await createResponseArgs(fetchResponse);
		return conversion.response(adapterResponseArgs) as TResponse;
	};
}

// One option per request body type: `mimeType` may be left out for JSON, and is required
// otherwise (e.g. 'application/x-www-form-urlencoded'). Matches the rxjs adapter.
type BodyPartInner<
	TBodies extends RequestBodies,
	TMime extends keyof TBodies,
> = TMime extends 'application/json'
	? { body: TBodies['application/json']; mimeType?: 'application/json' }
	: { body: TBodies[TMime]; mimeType: TMime };
type BodyPart<
	TCallType extends TransformCallType,
	TBodies extends RequestBodies,
> = TCallType extends 'no-body'
	? { body: undefined }
	: TCallType extends 'optional'
		? BodyPartInner<TBodies, keyof TBodies> | { body: undefined }
		: BodyPartInner<TBodies, keyof TBodies>;

type BaseRequestParam<
	TCallType extends TransformCallType,
	TParams,
	TBodies extends RequestBodies,
	TExtra,
> = {
	params: TParams;
	extra: TExtra;
} & BodyPart<TCallType, TBodies>;

// PropsOptional maps over the keys every union member shares, so apply it to each member
type DistributePropsOptional<T> = T extends object ? PropsOptional<T> : never;

export type RequestParam<
	TCallType extends TransformCallType,
	TParams,
	TBodies extends RequestBodies,
	TExtra,
> = DistributePropsOptional<
	BaseRequestParam<TCallType, TParams, TBodies, TExtra>
>;

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

/** An empty body (e.g. a 204 that still sends a JSON Content-Type) is no body, not invalid JSON */
function parseJson(text: string): unknown {
	return text ? JSON.parse(text) : undefined;
}
