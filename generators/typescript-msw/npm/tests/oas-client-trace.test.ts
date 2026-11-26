import { setupServer } from 'msw/node';
import fetch from 'node-fetch';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toMswHandler } from '../src';
import operations from './generated/oas-client-trace/operations';

const baseDomain = 'http://localhost/';
const traceEcho = toMswHandler(operations.traceEcho, { baseDomain });

// MSW intercepts requests as Fetch API Requests, which can't use TRACE, so a TRACE handler never matches
describe('typescript-msw oas-client-trace.yaml', () => {
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	it('does not mock other methods with a TRACE handler', async () => {
		server.use(
			traceEcho(
				{ params: {} },
				{
					statusCode: 200,
					mimeType: 'message/http',
					data: {} as Blob, // never sent: the handler never matches
				},
			),
		);

		await expect(
			fetch(new URL('/echo', baseDomain), { method: 'GET' }),
		).rejects.toThrow();
	});
});
