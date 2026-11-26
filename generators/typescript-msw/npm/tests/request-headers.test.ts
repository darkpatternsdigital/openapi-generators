import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toMswHandler } from '../src';
import operations from './generated/headers/operations';

const baseDomain = 'http://localhost/';
const getInfo = toMswHandler(operations.getInfo, { baseDomain });

describe('typescript-msw request headers', () => {
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	it.skip('does not match a request with a different header parameter — OpenAPI 3.0 gap (Parameter style): msw handlers ignore header parameters when matching', async () => {
		server.use(
			getInfo(
				{ params: { xData: 'expected' } },
				{ statusCode: 204, mimeType: '', data: undefined },
			),
		);

		await expect(
			fetch(new URL('/info', baseDomain), {
				headers: { 'X-Data': 'other' },
			}),
		).rejects.toThrow();
	});
});
