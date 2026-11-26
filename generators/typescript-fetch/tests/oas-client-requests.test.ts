import { Blob as NodeBlob } from 'node:buffer';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toFetchApi } from '../src';
import type { FetchImplementation } from '../src';
import cookieOperations from './generated/oas-client-cookies/operations';
import methodOperations from './generated/oas-client-methods/operations';
import styleOperations from './generated/oas-client-param-styles/operations';
import bodyOperations from './generated/oas-client-request-bodies/operations';

const baseDomain = 'http://localhost/';
const fetchImpl: FetchImplementation<unknown> = (url, params) => {
	return fetch(
		new URL(url, baseDomain),
		params as fetch.RequestInit,
	) as unknown as ReturnType<FetchImplementation<unknown>>;
};
// node-fetch 2 can't send a native Blob body, so request bodies go through Node's own fetch
const nativeFetchImpl: FetchImplementation<unknown> = (url, params) => {
	return globalThis.fetch(
		new URL(url, baseDomain),
		params as RequestInit,
	) as unknown as ReturnType<FetchImplementation<unknown>>;
};

type Received = {
	method: string;
	url: URL;
	headers: { get(name: string): string | null };
	body: string;
};

describe('typescript-fetch OpenAPI 3.0 requests', () => {
	const methods = toFetchApi(methodOperations, fetchImpl);
	const cookies = toFetchApi(cookieOperations, fetchImpl);
	const styles = toFetchApi(styleOperations, fetchImpl);
	const bodies = toFetchApi(bodyOperations, nativeFetchImpl);
	const server = setupServer();
	let received: Received | undefined;

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => {
		server.resetHandlers();
		received = undefined;
	});
	afterAll(() => server.close());

	function capture(path: string) {
		server.use(
			http.all(new URL(path, baseDomain).href, async ({ request }) => {
				received = {
					method: request.method,
					url: new URL(request.url),
					headers: request.headers,
					body: await request.text(),
				};
				return new HttpResponse(null, { status: 204 });
			}),
		);
	}

	it('sends PUT requests with a JSON body', async () => {
		capture('/items/:id');

		await methods.replaceItem({ params: { id: 'i-1' }, body: { name: 'n' } });

		expect(received?.method).toBe('PUT');
		expect(received?.url.pathname).toBe('/items/i-1');
		expect(received?.headers.get('Content-Type')).toBe('application/json');
		expect(JSON.parse(received?.body ?? '')).toEqual({ name: 'n' });
	});

	it('sends PATCH requests', async () => {
		capture('/items/:id');

		await methods.updateItem({ params: { id: 'i-1' }, body: { name: 'n' } });

		expect(received?.method).toBe('PATCH');
		expect(JSON.parse(received?.body ?? '')).toEqual({ name: 'n' });
	});

	it('sends HEAD and OPTIONS requests', async () => {
		capture('/items/:id');

		await methods.checkItem({ params: { id: 'i-1' } });
		expect(received?.method).toBe('HEAD');

		await methods.itemOptions({ params: { id: 'i-1' } });
		expect(received?.method).toBe('OPTIONS');
	});

	it('substitutes several parameters in one path segment', async () => {
		capture('/files/:file');

		await methods.getFile({ params: { name: 'report', ext: 'pdf' } });

		expect(received?.url.pathname).toBe('/files/report.pdf');
	});

	it.skip('sends cookie parameters in the Cookie header — OpenAPI 3.0 gap (Cookie parameters): cookie parameters are accepted but no Cookie header is sent', async () => {
		capture('/session');

		await cookies.getSession({ params: { session: 'abc', theme: 'dark' } });

		expect(received?.headers.get('Cookie')).toBe('session=abc; theme=dark');
	});

	it('repeats exploded query arrays', async () => {
		capture('/form-explode');

		await styles.getFormExplode({ params: { tags: ['a', 'b'] } });

		expect(received?.url.searchParams.getAll('tags')).toEqual(['a', 'b']);
	});

	it.skip('delimits query arrays that are not exploded — OpenAPI 3.0 gap (Parameter style): explode: false and delimited styles are ignored; arrays are always sent as repeated keys', async () => {
		capture('/form-no-explode');
		await styles.getFormNoExplode({ params: { tags: ['a', 'b'] } });
		expect(received?.url.searchParams.getAll('tags')).toEqual(['a,b']);

		capture('/pipe-delimited');
		await styles.getPipeDelimited({ params: { tags: ['a', 'b'] } });
		expect(received?.url.searchParams.getAll('tags')).toEqual(['a|b']);
	});

	it.skip('sends deepObject query parameters — OpenAPI 3.0 gap (Parameter style): style: deepObject is ignored; the object is sent as [object Object]', async () => {
		capture('/deep-object');

		await styles.getDeepObject({
			params: { filter: { color: 'red', size: 'large' } },
		});

		expect(received?.url.searchParams.get('filter[color]')).toBe('red');
		expect(received?.url.searchParams.get('filter[size]')).toBe('large');
	});

	it('omits an optional JSON request body', async () => {
		capture('/optional');

		await bodies.saveNote({ params: {} });

		expect(received?.method).toBe('PUT');
		expect(received?.headers.get('Content-Type')).toBeNull();
		expect(received?.body).toBe('');
	});

	it('sends application/octet-stream bodies as bytes', async () => {
		capture('/binary');

		await bodies.sendBinary({
			params: {},
			body: new NodeBlob([new Uint8Array([1, 2, 3])]) as unknown as Blob,
			mimeType: 'application/octet-stream',
		});

		expect(received?.headers.get('Content-Type')).toBe(
			'application/octet-stream',
		);
		expect(Array.from(new TextEncoder().encode(received?.body))).toEqual([
			1, 2, 3,
		]);
	});
});
