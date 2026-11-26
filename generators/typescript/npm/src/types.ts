import type {
	HttpMethod,
	AdapterRequestArgs,
	AdapterResponseArgs,
} from './inputs-outputs';

export type RequestBodies = {
	[mimeType: string]: any;
};

export type Responses = {
	other?: {
		[mimeType: string]: any;
	};
	[statusCode: number]: {
		[mimeType: string]: any;
	};
};

export type StandardResponse<
	TStatusCode extends number | 'other' = number | 'other',
	TMimeType extends string = string,
	TBody = unknown,
> = {
	statusCode: TStatusCode;
	mimeType: TMimeType;
	data: TBody;
	response: AdapterResponseArgs;
};

type Digit = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
/** Every status code in a range, such as 400 through 499 for `StatusCodeRange<4>` (the `4XX` range) */
export type StatusCodeRange<TStatusCodeClass extends 1 | 2 | 3 | 4 | 5> =
	`${TStatusCodeClass}${Digit}${Digit}` extends `${infer TStatusCode extends number}`
		? TStatusCode
		: never;

export type TransformRequestNoBody<TRequestParams extends {}, TResult> = (
	params: TRequestParams,
) => TResult;
export type TransformRequestWithBody<
	TRequestParams extends {},
	TRequestBodies extends RequestBodies,
	TResult,
> = <TMimeType extends keyof TRequestBodies>(
	params: TRequestParams,
	body: TRequestBodies[TMimeType],
	mimeType: TMimeType,
) => TResult;
export type TransformRequestWithOptionalBody<
	TRequestParams extends {},
	TRequestBodies extends RequestBodies,
	TResult,
> = TransformRequestNoBody<TRequestParams, TResult> &
	TransformRequestWithBody<TRequestParams, TRequestBodies, TResult>;

export type TransformCallType = 'no-body' | 'body' | 'optional';

export type TransformRequest<
	TRequestParams extends {},
	TRequestBodies extends RequestBodies,
	TCallType extends TransformCallType,
	TResult,
> = TCallType extends 'no-body'
	? TransformRequestNoBody<TRequestParams, TResult>
	: TCallType extends 'body'
		? TransformRequestWithBody<TRequestParams, TRequestBodies, TResult>
		: TCallType extends 'optional'
			? TransformRequestWithOptionalBody<
					TRequestParams,
					TRequestBodies,
					TResult
				>
			: never;

export type TransformResponse<TResponses extends StandardResponse> = (
	args: AdapterResponseArgs,
) => TResponses;

export type RequestConversion<
	TMethod extends HttpMethod,
	TUrlParams extends {},
	TRequestParams extends TUrlParams,
	TRequestBodies extends RequestBodies,
	TResponses extends StandardResponse,
	TCallType extends TransformCallType,
> = {
	name: string;
	method: TMethod;
	url: (params: TUrlParams) => string;
	callType: TCallType;
	request: TransformRequest<
		TRequestParams,
		TRequestBodies,
		TCallType,
		AdapterRequestArgs
	>;
	response: TransformResponse<TResponses>;
} & (TCallType extends 'no-body'
	? {}
	: {
			bodies: {
				[K in keyof TRequestBodies]: (input: TRequestBodies[K]) => unknown;
			};
		});

export type RequestConversions = Record<
	string,
	RequestConversion<
		HttpMethod,
		any,
		any,
		any,
		StandardResponse,
		TransformCallType
	>
>;
