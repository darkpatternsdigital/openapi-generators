import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toMswHandler } from '@darkpatternsdigital/openapi-codegen-typescript-msw';
import { toFetchApi, toFetchOperation } from '../src';
import type { FetchImplementation } from '../src';
import operations from './generated/multi-path-variables/operations';

const baseDomain = 'http://localhost/';
const fetchImpl: FetchImplementation<unknown> = (url, params) =>
	fetch(
		new URL(url, baseDomain).href,
		params as fetch.RequestInit,
	) as unknown as ReturnType<FetchImplementation<unknown>>;
const fetchApi = toFetchApi(operations, fetchImpl);
const getPhotoMeta = toMswHandler(operations.getPhotoMeta, {
	baseDomain,
});

describe('typescript-fetch multi-path-variables.yaml', () => {
	const server = setupServer();

	beforeAll(() => server.listen());
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	it('can wrap a get as a single operation', async () => {
		const getPhotoMetaOperation = toFetchOperation(
			fetchImpl,
			operations.getPhotoMeta,
		);
		const outputMeta = 'unknown data';
		server.use(
			getPhotoMeta(
				{ params: { id: '123', key: 'author' } },
				{ statusCode: 200, data: outputMeta, mimeType: 'application/json' },
			),
		);
		const response = await getPhotoMetaOperation({
			params: { id: '123', key: 'author' },
		});
		expect(response.statusCode).toBe(200);
		expect(response.response.getResponseHeader('Content-Type')).toBe(
			'application/json',
		);
		expect(response.data).toEqual(outputMeta);
	});

	it('can wrap a get', async () => {
		const outputMeta = 'unknown data';
		server.use(
			getPhotoMeta(
				{ params: { id: '123', key: 'author' } },
				{ statusCode: 200, data: outputMeta, mimeType: 'application/json' },
			),
		);
		const response = await fetchApi.getPhotoMeta({
			params: { id: '123', key: 'author' },
		});
		expect(response.statusCode).toBe(200);
		expect(response.response.getResponseHeader('Content-Type')).toBe(
			'application/json',
		);
		expect(response.data).toEqual(outputMeta);
	});
});
