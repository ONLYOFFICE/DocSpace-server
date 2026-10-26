// A minimal LSP client over stdio: what an IDE does, without the IDE.

import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));

export class LspClient {
    constructor(initializationOptions = {}) {
        this.child = spawn(process.execPath, [process.env.DOCBUILDER_SERVER ?? path.join(here, '..', 'src', 'server.mjs'), '--stdio'], { stdio: ['pipe', 'pipe', 'inherit'] });
        this.buffer = Buffer.alloc(0);
        this.nextId = 1;
        this.waiting = new Map();
        this.diagnostics = new Map();
        this.notifications = [];
        this.diagnosticWaiters = [];
        this.child.stdout.on('data', chunk => this.receive(chunk));
        this.initializationOptions = initializationOptions;
    }

    async start() {
        const result = await this.request('initialize', { processId: process.pid, rootUri: null, capabilities: {}, initializationOptions: this.initializationOptions });
        this.notify('initialized', {});
        return result;
    }

    receive(chunk) {
        this.buffer = Buffer.concat([this.buffer, chunk]);
        for (;;) {
            const headerEnd = this.buffer.indexOf('\r\n\r\n');
            if (headerEnd < 0) return;
            const length = Number(this.buffer.subarray(0, headerEnd).toString().match(/Content-Length: (\d+)/i)[1]);
            if (this.buffer.length < headerEnd + 4 + length) return;
            const message = JSON.parse(this.buffer.subarray(headerEnd + 4, headerEnd + 4 + length).toString('utf8'));
            this.buffer = this.buffer.subarray(headerEnd + 4 + length);
            this.dispatch(message);
        }
    }

    dispatch(message) {
        if (message.id !== undefined && this.waiting.has(message.id)) {
            const { resolve, reject } = this.waiting.get(message.id);
            this.waiting.delete(message.id);
            message.error ? reject(new Error(message.error.message)) : resolve(message.result);
        } else if (message.method === 'textDocument/publishDiagnostics') {
            this.diagnostics.set(message.params.uri, message.params.diagnostics);
            for (const w of this.diagnosticWaiters.splice(0)) w();
        } else if (message.method && message.id === undefined) {
            this.notifications.push(message);
        } else if (message.id !== undefined && message.method) {
            this.send({ jsonrpc: '2.0', id: message.id, result: null });
        }
    }

    send(message) {
        const body = Buffer.from(JSON.stringify(message), 'utf8');
        this.child.stdin.write(`Content-Length: ${body.length}\r\n\r\n`);
        this.child.stdin.write(body);
    }

    request(method, params) {
        const id = this.nextId++;
        this.send({ jsonrpc: '2.0', id, method, params });
        return new Promise((resolve, reject) => this.waiting.set(id, { resolve, reject }));
    }

    notify(method, params) {
        this.send({ jsonrpc: '2.0', method, params });
    }

    async diagnosticsFor(uri, timeoutMs = 20000) {
        this.diagnostics.delete(uri);
        const deadline = Date.now() + timeoutMs;
        while (!this.diagnostics.has(uri)) {
            if (Date.now() > deadline) throw new Error(`no diagnostics for ${uri}`);
            await new Promise(r => {
                this.diagnosticWaiters.push(r);
                setTimeout(r, 200);
            });
        }
        return this.diagnostics.get(uri);
    }

    async stop() {
        await this.request('shutdown', null);
        this.notify('exit', null);
        this.child.kill();
    }
}
