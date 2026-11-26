import { Blob as NodeBlob } from 'node:buffer';
import { setupServer } from 'msw/node';
import { describe, beforeAll, afterEach, afterAll, it, expect } from 'vitest';
import { toMswHandler } from '../src';
import operations from './generated/oas-client-request-bodies/operations';

const baseDomain = 'http://localhost/';
const sendText = toMswHandler(operations.sendText, { baseDomain });
const sendBinary = toMswHandler(operations.sendBinary, { baseDomain });
const saveNote = toMswHandler(operations.saveNote, { baseDomain });

// Node's own fetch: node-fetch 2 can't send a native Blob body
function post(path: string, init: RequestInit) {
	return globalThis.fetch(new URL(path, baseDomain), init);
}

describe('typescript-msw oas-client-request-bodies.yaml', () => {
	const server = setupServer();

	beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
	afterEach(() => server.resetHandlers());
	afterAll(() => server.close());

	it('matches text bodies by their text', async () => {
		server.use(
			sendText(
				{ params: {}, body: 'hello', mimeType: 'text/plain' },
				{ statusCode: 204, mimeType: '', data: undefined },
			),
		);

		const response = await post('/text', {
			method: 'POST',
			headers: { 'Content-Type': 'text/plain' },
			body: 'hello',
		});
		const other = post('/text', {
			method: 'POST',
			headers: { 'Content-Type': 'text/plain' },
			body: 'goodbye',
		});

		expect(response.status).toBe(204);
		await expect(other).rejects.toThrow();
	});

	it('matches binary bodies by their bytes', async () => {
		server.use(
			sendBinary(
				{
					params: {},
					body: new NodeBlob([new Uint8Array([1, 2, 3])]) as unknown as Blob,
					mimeType: 'application/octet-stream',
				},
				{ statusCode: 204, mimeType: '', data: undefined },
			),
		);

		const response = await post('/binary', {
			method: 'POST',
			headers: { 'Content-Type': 'application/octet-stream' },
			body: new Uint8Array([1, 2, 3]),
		});
		const other = post('/binary', {
			method: 'POST',
			headers: { 'Content-Type': 'application/octet-stream' },
			body: new Uint8Array([1, 2, 4]),
		});

		expect(response.status).toBe(204);
		await expect(other).rejects.toThrow();
	});

	it('matches requests that leave out an optional body', async () => {
		server.use(
			saveNote(
				{ params: {}, body: undefined },
				{ statusCode: 204, mimeType: '', data: undefined },
			),
		);

		const response = await post('/optional', { method: 'PUT' });

		expect(response.status).toBe(204);
	});
});
