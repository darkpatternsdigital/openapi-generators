import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toMswHandler, toMswResponse } from '../src';
import headersOperations from './generated/headers/operations';
import formatsOperations from './generated/parameter-formats/operations';

const baseDomain = 'http://localhost/';
const getInfo = toMswHandler(headersOperations.getInfo, { baseDomain });
const postFormats = toMswHandler(formatsOperations.postFormats, { baseDomain });

describe('typescript-msw responses', () => {
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	it('mocks a response without a body', async () => {
		server.use(
			postFormats(
				{
					params: { name: 'n' },
					body: { size: 'small' },
					mimeType: 'application/x-www-form-urlencoded',
				},
				{ statusCode: 204, mimeType: '', data: undefined },
			),
		);

		const response = await fetch(new URL('/formats/n', baseDomain), {
			method: 'POST',
			headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
			body: 'size=small',
		});

		expect(response.status).toBe(204);
		expect(await response.text()).toBe('');
	});

	it('sets response headers on a response without a body', async () => {
		server.use(
			getInfo(
				{ params: { xData: 'abc' } },
				{
					statusCode: 204,
					mimeType: '',
					data: undefined,
					headers: { 'X-Data': 'from-mock' },
				},
			),
		);

		const response = await fetch(new URL('/info', baseDomain));

		expect(response.status).toBe(204);
		expect(response.headers.get('X-Data')).toBe('from-mock');
	});

	it('uses the given status for a default (other) response', () => {
		const withStatus = toMswResponse({
			statusCode: 'other',
			status: 418,
			mimeType: 'application/json',
			data: { message: 'teapot' },
		});
		const withoutStatus = toMswResponse({
			statusCode: 'other',
			mimeType: 'application/json',
			data: { message: 'error' },
		});

		expect(withStatus.status).toBe(418);
		expect(withoutStatus.status).toBe(500);
	});

	it('sends other media types as given', async () => {
		const response = toMswResponse({
			statusCode: 200,
			mimeType: 'text/plain',
			data: 'hello',
		});

		expect(response.status).toBe(200);
		expect(response.headers.get('Content-Type')).toBe('text/plain');
		expect(await response.text()).toBe('hello');
	});

	it('sets response headers on a JSON response', async () => {
		server.use(
			getInfo(
				{ params: { xData: 'abc' } },
				{
					statusCode: 200,
					mimeType: 'application/json',
					data: { key: 'value' },
					headers: { 'X-Data': 'from-mock' },
				},
			),
		);

		const response = await fetch(new URL('/info', baseDomain));

		expect(response.status).toBe(200);
		expect(response.headers.get('Content-Type')).toBe('application/json');
		expect(response.headers.get('X-Data')).toBe('from-mock');
		expect(await response.json()).toEqual({ key: 'value' });
	});
});
