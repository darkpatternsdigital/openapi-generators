import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { lastValueFrom } from 'rxjs';
import { request as ajax } from 'universal-rxjs-ajax';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toRxjsApi } from '../src';
import operations from './generated/oas-client-methods/operations';

const baseDomain = 'http://localhost/';

describe('typescript-rxjs oas-client-methods.yaml', () => {
	const wrapped = toRxjsApi(operations, baseDomain, ajax);
	const server = setupServer();
	let received: { method: string; body: string } | undefined;

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => {
		server.resetHandlers();
		received = undefined;
	});
	afterAll(() => server.close());

	function capture(status: number) {
		server.use(
			http.all(new URL('/items/:id', baseDomain).href, async ({ request }) => {
				received = { method: request.method, body: await request.text() };
				return new HttpResponse(null, { status });
			}),
		);
	}

	it('sends PUT requests with a JSON body', async () => {
		capture(204);

		await lastValueFrom(
			wrapped.replaceItem({ params: { id: 'i-1' }, body: { name: 'n' } }),
		);

		expect(received?.method).toBe('PUT');
		expect(JSON.parse(received?.body ?? '')).toEqual({ name: 'n' });
	});

	it('sends PATCH requests without an optional body', async () => {
		capture(204);

		const result = await lastValueFrom(
			wrapped.updateItem({ params: { id: 'i-1' } }),
		);

		expect(received?.method).toBe('PATCH');
		expect(result.statusCode).toBe(204);
	});

	it('sends HEAD and OPTIONS requests', async () => {
		capture(404);
		const head = await lastValueFrom(
			wrapped.checkItem({ params: { id: 'i-1' } }),
		);
		expect(received?.method).toBe('HEAD');
		expect(head.statusCode).toBe(404);

		capture(204);
		await lastValueFrom(wrapped.itemOptions({ params: { id: 'i-1' } }));
		expect(received?.method).toBe('OPTIONS');
	});
});
