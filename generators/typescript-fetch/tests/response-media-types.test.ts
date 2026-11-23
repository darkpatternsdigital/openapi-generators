import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toFetchOperation } from '../src';
import type { FetchImplementation } from '../src';
import operations from './generated/response-media-types/operations';

const baseDomain = 'http://localhost/';
const fetchImpl: FetchImplementation<unknown> = (url, params) => {
	return fetch(
		new URL(url, baseDomain),
		params as fetch.RequestInit,
	) as unknown as ReturnType<FetchImplementation<unknown>>;
};
const getReport = toFetchOperation(fetchImpl, operations.getReport);

describe('typescript-fetch response-media-types.yaml', () => {
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	function respondWith(response: () => Response) {
		server.use(http.get(new URL('/report', baseDomain).href, () => response()));
	}

	it('parses JSON, ignoring media type parameters and case', async () => {
		respondWith(
			() =>
				new HttpResponse('{"name":"quarterly"}', {
					headers: { 'Content-Type': 'Application/JSON; charset=utf-8' },
				}),
		);

		const result = await getReport();

		expect(result.statusCode).toBe(200);
		expect(result.mimeType).toBe('application/json');
		expect(result.data).toEqual({ name: 'quarterly' });
	});

	it('reads text as a string', async () => {
		respondWith(() => HttpResponse.text('hello'));

		const result = await getReport();

		expect(result.mimeType).toBe('text/plain');
		expect(result.data).toBe('hello');
	});

	it('returns other media types as a Blob', async () => {
		respondWith(
			() =>
				new HttpResponse(new Uint8Array([1, 2, 3]), {
					headers: { 'Content-Type': 'application/octet-stream' },
				}),
		);

		const result = await getReport();

		expect(result.mimeType).toBe('application/octet-stream');
		const data = result.data as unknown as {
			arrayBuffer(): Promise<ArrayBuffer>;
		};
		expect(Array.from(new Uint8Array(await data.arrayBuffer()))).toEqual([
			1, 2, 3,
		]);
	});

	it('returns no data for an empty body with a JSON media type', async () => {
		respondWith(
			() =>
				new HttpResponse(null, {
					status: 204,
					headers: { 'Content-Type': 'application/json' },
				}),
		);

		const result = await getReport();

		expect(result.statusCode).toBe(204);
		expect(result.data).toBeUndefined();
	});

	it('returns no data for a response without content', async () => {
		respondWith(() => new HttpResponse(null, { status: 204 }));

		const result = await getReport();

		expect(result.statusCode).toBe(204);
		expect(result.data).toBeUndefined();
	});
});
