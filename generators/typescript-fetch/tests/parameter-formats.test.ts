import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toFetchOperation } from '../src';
import type { FetchImplementation } from '../src';
import operations from './generated/parameter-formats/operations';

const baseDomain = 'http://localhost/';
const fetchImpl: FetchImplementation<unknown> = (url, params) => {
	return fetch(
		new URL(url, baseDomain),
		params as fetch.RequestInit,
	) as unknown as ReturnType<FetchImplementation<unknown>>;
};

type Received = { url: URL; contentType: string | null; body: string };

describe('typescript-fetch parameter-formats.yaml', () => {
	const server = setupServer();
	let received: Received | undefined;

	beforeAll(() => server.listen());
	afterEach(() => {
		server.resetHandlers();
		received = undefined;
	});
	afterAll(() => server.close());

	function captureRequests(method: 'get' | 'post') {
		server.use(
			http[method](
				new URL('/formats/:name', baseDomain).href,
				async ({ request }) => {
					received = {
						url: new URL(request.url),
						contentType: request.headers.get('Content-Type'),
						body: await request.text(),
					};
					return new HttpResponse(null, { status: 204 });
				},
			),
		);
	}

	it('sends form fields url-encoded', async () => {
		captureRequests('post');
		const postFormats = toFetchOperation(fetchImpl, operations.postFormats);

		const response = await postFormats({
			params: { name: 'n' },
			body: {
				size: 'large',
				when: '2026-11-22T16:45:00-06:00',
				flag: false,
				tags: ['a', 'b'],
				note: null,
			},
			mimeType: 'application/x-www-form-urlencoded',
		});

		expect(response.statusCode).toBe(204);
		expect(received?.contentType).toBe('application/x-www-form-urlencoded');
		const form = new URLSearchParams(received?.body);
		expect(form.get('size')).toBe('large');
		expect(form.get('when')).toBe('2026-11-22T16:45:00-06:00');
		expect(form.get('flag')).toBe('false');
		expect(form.getAll('tags')).toEqual(['a', 'b']);
		// A nullable field that is present with a null value is sent empty, not as "null"
		expect(form.get('note')).toBe('');
	});

	it('omits form fields that are not set', async () => {
		captureRequests('post');
		const postFormats = toFetchOperation(fetchImpl, operations.postFormats);

		await postFormats({
			params: { name: 'n' },
			body: { size: 'small' },
			mimeType: 'application/x-www-form-urlencoded',
		});

		expect(received?.body).toBe('size=small');
	});

	it('encodes path and query values', async () => {
		captureRequests('get');
		const getFormats = toFetchOperation(fetchImpl, operations.getFormats);

		await getFormats({
			params: {
				name: 'a b/c',
				when: '2026-11-22T16:45:00-06:00',
				amount: 1.5,
				flag: true,
				size: 'small',
				data: 'AQID',
				xSize: 'large',
			},
		});

		expect(received?.url.pathname).toBe('/formats/a%20b%2Fc');
		expect(received?.url.searchParams.get('when')).toBe(
			'2026-11-22T16:45:00-06:00',
		);
		expect(received?.url.searchParams.get('amount')).toBe('1.5');
		expect(received?.url.searchParams.get('flag')).toBe('true');
		expect(received?.url.searchParams.get('size')).toBe('small');
		expect(received?.url.searchParams.get('data')).toBe('AQID');
	});
});
