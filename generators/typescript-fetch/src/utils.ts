import type {
	RequestConversion,
	StandardResponse,
	TransformCallType,
	HttpMethod,
} from '@darkpatternsdigital/openapi-codegen-typescript';

export const applicationJson = 'application/json';

export type AnyObject = any;
export type AnyRequestBodies = any;
export type EmptyObject = Record<never, never>;
export type AnyRequestConversion = RequestConversion<
	HttpMethod,
	AnyObject,
	AnyObject,
	AnyRequestBodies,
	StandardResponse,
	TransformCallType
>;

export type IsKeylessOrUndefined<T> = EmptyObject extends T
	? true
	: undefined extends T
		? true
		: false;
export type IfKeyless<T, TTrue, TFalse> = EmptyObject extends T
	? TTrue
	: TFalse;

type KeylessOrUndefinedProps<T extends object> = {
	[K in keyof T]: IsKeylessOrUndefined<T[K]> extends true ? K : never;
}[keyof T];
export type PropsOptional<T extends object> = {
	[K in Exclude<keyof T, KeylessOrUndefinedProps<T>>]: T[K];
} & {
	[K in KeylessOrUndefinedProps<T>]?: T[K];
};
