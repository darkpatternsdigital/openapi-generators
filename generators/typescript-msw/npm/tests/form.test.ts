import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toMswHandler } from '../src';
import operations from './generated/form/operations';

const baseDomain = 'http://localhost/';
const postBasicForm = toMswHandler(operations.postBasicForm, { baseDomain });

describe('typescript-msw form.yaml', () => {
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	function useFormHandler() {
		server.use(
			postBasicForm(
				{
					params: {},
					body: { name: 'Fido', tag: 'dog', hasIdTag: true },
					mimeType: 'application/x-www-form-urlencoded',
				},
				{ statusCode: 200, mimeType: 'application/json', data: 17 },
			),
		);
	}

	function post(body: string) {
		return fetch(new URL('/basic', baseDomain), {
			method: 'POST',
			headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
			body,
		});
	}

	it('matches a form request with the same fields, in any order', async () => {
		useFormHandler();

		const response = await post('hasIdTag=true&tag=dog&name=Fido');

		expect(response.status).toBe(200);
		expect(await response.json()).toBe(17);
	});

	it('does not match a form request with different fields', async () => {
		useFormHandler();

		await expect(post('name=Felix&tag=cat&hasIdTag=false')).rejects.toThrow();
	});
});
