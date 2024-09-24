import type {
	TransformCallType,
	RequestConversions,
	RequestConversion,
	HttpMethod,
} from '@darkpatternsdigital/openapi-codegen-typescript';
import type { FetchOperation, FetchImplementation } from './convert-single';
import { toFetchOperation } from './convert-single';
import type {
	AnyObject,
	AnyRequestBodies,
	AnyRequestConversion,
} from './utils';

type AnyFetchOperation<TExtra> = FetchOperation<
	AnyObject,
	AnyObject,
	AnyRequestBodies,
	TransformCallType,
	TExtra
>;

type TransformOperations<TMethods extends RequestConversions, TExtra> = {
	[K in keyof TMethods]: TMethods[K] extends RequestConversion<
		HttpMethod,
		AnyObject,
		infer TParams,
		infer TBodies,
		infer TResponse,
		infer TCallType
	>
		? FetchOperation<TParams, TBodies, TResponse, TCallType, TExtra>
		: never;
};

function applyTransform<TMethods extends RequestConversions, TExtra>(
	methods: TMethods,
	transform: (input: AnyRequestConversion) => AnyFetchOperation<TExtra>,
): TransformOperations<TMethods, RequestConversions> {
	return Object.fromEntries(
		Object.entries(methods).map(([operationId, conversion]) => [
			operationId,
			transform(conversion),
		]),
	) as TransformOperations<TMethods, RequestConversions>;
}

export function toFetchApi<TMethods extends RequestConversions, TExtra>(
	api: TMethods,
	fetchImpl: FetchImplementation<TExtra>,
) {
	return applyTransform<TMethods, TExtra>(api, (conversion) =>
		toFetchOperation(fetchImpl, conversion),
	);
}
