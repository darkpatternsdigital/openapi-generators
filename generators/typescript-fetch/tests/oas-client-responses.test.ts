import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toFetchApi } from '../src';
import type { FetchImplementation } from '../src';
import operations from './generated/oas-client-responses/operations';

const baseDomain = 'http://localhost/';
const fetchImpl: FetchImplementation<unknown> = (url, params) => {
	return fetch(
		new URL(url, baseDomain),
		params as fetch.RequestInit,
	) as unknown as ReturnType<FetchImplementation<unknown>>;
};

describe('typescript-fetch OpenAPI 3.0 responses', () => {
	const wrapped = toFetchApi(operations, fetchImpl);
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	function respondWith(path: string, response: () => Response) {
		server.use(http.get(new URL(path, baseDomain).href, () => response()));
	}

	it('prefers an exact status code over a range', async () => {
		respondWith('/ranges', () => HttpResponse.json({ id: 'i-1' }));

		const result = await wrapped.getRanges();

		expect(result.statusCode).toBe(200);
		expect(result.data).toEqual({ id: 'i-1' });
	});

	it.skip('parses responses in a status code range — OpenAPI 3.0 gap (Response ranges): 2XX and 4XX responses are dropped, so other codes in the range become "other"', async () => {
		respondWith('/ranges', () =>
			HttpResponse.json({ message: 'missing' }, { status: 404 }),
		);

		const result = await wrapped.getRanges();

		expect(result.statusCode).toBe(404);
		expect(result.data).toEqual({ message: 'missing' });
	});

	it('parses default responses as other', async () => {
		respondWith('/with-default', () =>
			HttpResponse.json({ message: 'unavailable' }, { status: 503 }),
		);

		const result = await wrapped.getWithDefault();

		expect(result.statusCode).toBe('other');
		expect(result.response.status).toBe(503);
		expect(result.data).toEqual({ message: 'unavailable' });
	});

	it.skip('parses declared response headers — OpenAPI 3.0 gap (Response headers): declared response headers are not parsed into the response', async () => {
		respondWith('/with-headers', () =>
			HttpResponse.json(
				{ id: 'i-1' },
				{ headers: { 'X-Rate-Limit': '10', 'X-Request-Id': 'r-1' } },
			),
		);

		const result = await wrapped.getWithHeaders();

		// The headers property is illustrative: response headers are not parsed yet
		expect((result as unknown as { headers: unknown }).headers).toEqual({
			'X-Rate-Limit': 10,
			'X-Request-Id': 'r-1',
		});
	});
});
