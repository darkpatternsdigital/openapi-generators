import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { lastValueFrom } from 'rxjs';
import { request as ajax } from 'universal-rxjs-ajax';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toRxjsApi } from '../src';
import operations from './generated/response-media-types/operations';

const baseDomain = 'http://localhost/';

describe('typescript-rxjs response-media-types.yaml', () => {
	const wrapped = toRxjsApi(operations, baseDomain, ajax);
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

		const result = await lastValueFrom(wrapped.getReport());

		expect(result.statusCode).toBe(200);
		expect(result.mimeType).toBe('application/json');
		expect(result.data).toEqual({ name: 'quarterly' });
	});

	it('reads text as a string', async () => {
		respondWith(() => HttpResponse.text('hello'));

		const result = await lastValueFrom(wrapped.getReport());

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

		const result = await lastValueFrom(wrapped.getReport());

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

		const result = await lastValueFrom(wrapped.getReport());

		expect(result.statusCode).toBe(204);
		expect(result.data).toBeUndefined();
	});

	it('returns no data for a response without content', async () => {
		respondWith(() => new HttpResponse(null, { status: 204 }));

		const result = await lastValueFrom(wrapped.getReport());

		expect(result.statusCode).toBe(204);
		expect(result.data).toBeUndefined();
	});
});
