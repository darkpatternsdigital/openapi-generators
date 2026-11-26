import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toMswHandler } from '../src';
import operations from './generated/oas-client-methods/operations';

const baseDomain = 'http://localhost/';
const replaceItem = toMswHandler(operations.replaceItem, { baseDomain });
const updateItem = toMswHandler(operations.updateItem, { baseDomain });
const checkItem = toMswHandler(operations.checkItem, { baseDomain });
const itemOptions = toMswHandler(operations.itemOptions, { baseDomain });

describe('typescript-msw oas-client-methods.yaml', () => {
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	it('mocks PUT requests with a body', async () => {
		server.use(
			replaceItem(
				{
					params: { id: 'i-1' },
					body: { name: 'n' },
					mimeType: 'application/json',
				},
				{ statusCode: 200, mimeType: 'application/json', data: { name: 'n' } },
			),
		);

		const response = await fetch(new URL('/items/i-1', baseDomain), {
			method: 'PUT',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ name: 'n' }),
		});

		expect(response.status).toBe(200);
		expect(await response.json()).toEqual({ name: 'n' });
	});

	it('mocks PATCH requests without an optional body', async () => {
		server.use(
			updateItem(
				{ params: { id: 'i-1' } },
				{ statusCode: 204, mimeType: '', data: undefined },
			),
		);

		const response = await fetch(new URL('/items/i-1', baseDomain), {
			method: 'PATCH',
		});

		expect(response.status).toBe(204);
	});

	it('mocks HEAD and OPTIONS requests', async () => {
		server.use(
			checkItem(
				{ params: { id: 'i-1' } },
				{ statusCode: 404, mimeType: '', data: undefined },
			),
			itemOptions(
				{ params: { id: 'i-1' } },
				{ statusCode: 204, mimeType: '', data: undefined },
			),
		);

		const head = await fetch(new URL('/items/i-1', baseDomain), {
			method: 'HEAD',
		});
		const options = await fetch(new URL('/items/i-1', baseDomain), {
			method: 'OPTIONS',
		});

		expect(head.status).toBe(404);
		expect(options.status).toBe(204);
	});
});
