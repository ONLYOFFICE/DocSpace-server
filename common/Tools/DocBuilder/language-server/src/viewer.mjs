// The live viewer: a document a preview run built, opened in a Document Server editor - read-only, or
// editable, so the user can change it by hand and the difference can be carried back into the template.
//
// Document Server downloads the document itself, by URL, so the language server serves it - and the
// page that embeds the editor - over a small HTTP server of its own. Every document gets a random
// token in its path; only files under the preview folder can be served. The editor configuration
// is signed with the server's JWT secret, read from its local.json when it is installed here.
//
// An editable document is a copy kept apart from the run folders (a new run clears those): the
// built document stays as it was, next to it, and Document Server saves the edited one through the
// callback this server answers.

import crypto from 'node:crypto';
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';

export const previewRoot = () => path.join(os.tmpdir(), 'docbuilder-preview');
export const editsRoot = () => path.join(previewRoot(), '_edits');

const documentTypes = { xlsx: 'cell', csv: 'cell', docx: 'word', txt: 'word', pptx: 'slide', pdf: 'pdf' };
const editableTypes = ['xlsx', 'docx', 'pptx'];
const contentTypes = {
    xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    pptx: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
    pdf: 'application/pdf',
    csv: 'text/csv',
    txt: 'text/plain',
};

const serverCandidates = ['http://localhost:8085', 'http://localhost', 'http://localhost:8080'];
const configCandidates = [
    'C:/Program Files/ONLYOFFICE/DocumentServer/config/local.json',
    '/etc/onlyoffice/documentserver/local.json',
];
const keptEdits = 5;

export class ViewerError extends Error {}

// The editable copy of a document a run built: { folder, original, edited }. The copy is made once;
// opening the same run again goes on with the edits already saved. Without create, null when there is
// no copy yet.
export function editCopy(document, { create = true } = {}) {
    const resolved = path.resolve(document);
    if (isInside(resolved, editsRoot())) {
        const folder = path.dirname(resolved);
        const copy = { folder, original: path.join(folder, 'original' + path.extname(resolved)), edited: resolved };
        return fs.existsSync(copy.original) ? copy : null;
    }
    const run = path.dirname(resolved);
    const folder = path.join(editsRoot(), path.basename(path.dirname(run)), path.basename(run));
    const copy = { folder, original: path.join(folder, 'original' + path.extname(resolved)), edited: path.join(folder, path.basename(resolved)) };
    if (!fs.existsSync(copy.edited)) {
        if (!create) return null;
        fs.mkdirSync(folder, { recursive: true });
        fs.copyFileSync(resolved, copy.original);
        fs.copyFileSync(resolved, copy.edited);
        if (fs.existsSync(path.join(run, 'run.json'))) fs.copyFileSync(path.join(run, 'run.json'), path.join(folder, 'run.json'));
        const siblings = fs.readdirSync(path.dirname(folder)).sort().reverse();
        for (const old of siblings.slice(keptEdits)) {
            try {
                fs.rmSync(path.join(path.dirname(folder), old), { recursive: true, force: true });
            } catch { /* still open */ }
        }
    }
    return copy;
}

export class Viewer {
    constructor() {
        this.documents = new Map(); // token -> entry
        this.server = null;
        this.port = 0;
        this.listening = null;
    }

    // { documentServerUrl?, documentServerSecret?, fileHost? }, { edit? } -> { url, documentServer, edited? }
    async open(file, settings = {}, { edit = false } = {}) {
        let resolved = path.resolve(file);
        if (!isInside(resolved, previewRoot())) throw new ViewerError('Only a document a preview run built can be opened in the viewer.');
        if (!fs.existsSync(resolved)) throw new ViewerError(`${resolved} no longer exists: run the preview again.`);
        const type = path.extname(resolved).slice(1).toLowerCase();
        if (!documentTypes[type]) throw new ViewerError(`The viewer does not open .${type} files.`);
        if (edit && !editableTypes.includes(type)) throw new ViewerError(`The editor does not edit .${type} files.`);
        if (edit) {
            const copy = editCopy(resolved);
            if (!copy) throw new ViewerError(`${resolved} is not an edited copy.`);
            resolved = copy.edited;
        }

        const documentServer = await findDocumentServer(settings.documentServerUrl);
        await this.listen(settings.fileHost);

        const token = crypto.randomBytes(16).toString('hex');
        const stat = fs.statSync(resolved);
        this.documents.set(token, {
            file: resolved,
            type,
            title: path.basename(resolved),
            edit,
            // A new key for every version of the file: Document Server caches a document by its key,
            // and an editing session lives under it until everyone leaves.
            key: crypto.createHash('sha256').update(`${resolved}|${stat.mtimeMs}|${stat.size}|${edit}`).digest('hex').slice(0, 40),
            documentServer,
            secret: settings.documentServerSecret || readLocalSecret('browser'),
            commandSecret: settings.documentServerSecret || readLocalSecret('inbox'),
            callbackSecrets: [settings.documentServerSecret, readLocalSecret('outbox'), readLocalSecret('browser')].filter(Boolean),
            fileHost: settings.fileHost || '127.0.0.1',
            saves: 0,
            waiters: [],
        });
        // Old runs are gone from disk soon anyway; keep the map small.
        while (this.documents.size > 50) this.documents.delete(this.documents.keys().next().value);

        return { url: `http://127.0.0.1:${this.port}/view/${token}`, documentServer, ...(edit ? { edited: resolved } : {}) };
    }

    // Asks Document Server to save the editing session of an edited copy now, and waits until the
    // saved file is on disk. { saved: true } | { saved: false, reason } - false when there is nothing
    // to save, which still means the file on disk is current.
    async save(edited, { timeoutMs = 30000 } = {}) {
        const resolved = path.resolve(edited);
        const entries = [...this.documents.values()].filter(e => e.edit && e.file === resolved);
        const entry = entries[entries.length - 1];
        if (!entry) return { saved: false, reason: 'not open in the editor' };

        const saves = entry.saves;
        const body = { c: 'forcesave', key: entry.key };
        const answer = await this.command(entry, body);
        // 4: no changes since the last save; 1: no session under this key (the editor was closed, and
        // Document Server saved, or is about to save, the document on its own).
        if (answer.error === 4) return { saved: false, reason: 'no changes since the last save' };
        if (answer.error === 1) {
            await this.waitForSave(entry, saves, 15000);
            return { saved: entry.saves > saves, reason: entry.saves > saves ? undefined : 'the editor is closed' };
        }
        if (answer.error !== 0) throw new ViewerError(`Document Server refused to save the document (error ${answer.error}).`);
        await this.waitForSave(entry, saves, timeoutMs);
        if (entry.saves === saves) throw new ViewerError(`Document Server did not save the document in ${timeoutMs / 1000} s.`);
        return { saved: true };
    }

    async command(entry, body) {
        const payload = entry.commandSecret ? { ...body, token: sign(body, entry.commandSecret) } : body;
        for (const route of ['/command', '/coauthoring/CommandService.ashx']) {
            const response = await fetch(entry.documentServer + route, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
                signal: AbortSignal.timeout(10000),
            });
            if (response.status === 404) continue;
            return response.json();
        }
        throw new ViewerError('Document Server has no command service.');
    }

    waitForSave(entry, saves, timeoutMs) {
        if (entry.saves > saves) return Promise.resolve();
        return new Promise(resolve => {
            const timer = setTimeout(resolve, timeoutMs);
            entry.waiters.push(() => {
                clearTimeout(timer);
                resolve();
            });
        });
    }

    async listen(fileHost) {
        // Document Server in a container reaches the host by its own name, not by the loopback address.
        const host = fileHost && fileHost !== '127.0.0.1' && fileHost !== 'localhost' ? '0.0.0.0' : '127.0.0.1';
        if (this.listening && this.host === host) return this.listening;
        if (this.server) this.server.close();
        this.host = host;
        this.server = http.createServer((request, response) => {
            this.handle(request, response).catch(() => {
                if (!response.headersSent) response.writeHead(500);
                response.end();
            });
        });
        this.listening = new Promise((resolve, reject) => {
            this.server.once('error', reject);
            this.server.listen(0, host, () => {
                this.port = this.server.address().port;
                resolve();
            });
        });
        return this.listening;
    }

    async handle(request, response) {
        const [, kind, token] = request.url.split('?')[0].split('/');
        const entry = this.documents.get(token);
        if (entry && kind === 'callback' && request.method === 'POST' && entry.edit) {
            await this.callback(request, response, entry);
            return;
        }
        if (!entry || request.method !== 'GET') {
            response.writeHead(404).end();
            return;
        }
        if (kind === 'file') {
            response.writeHead(200, { 'Content-Type': contentTypes[entry.type], 'Cache-Control': 'no-store' });
            fs.createReadStream(entry.file).on('error', () => response.destroy()).pipe(response);
            return;
        }
        if (kind === 'view') {
            response.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' });
            response.end(page(entry, this.config(token, entry)));
            return;
        }
        response.writeHead(404).end();
    }

    // Document Server reports the editing session here; status 2 (everyone left) and 6 (saved on
    // request) carry the URL of the saved document, which replaces the edited copy.
    async callback(request, response, entry) {
        const answer = (code, value) => response.writeHead(code, { 'Content-Type': 'application/json' }).end(JSON.stringify(value));
        let data;
        try {
            data = JSON.parse(await readBody(request, 1 << 20));
        } catch {
            answer(400, { error: 1 });
            return;
        }
        if (entry.callbackSecrets.length) {
            const header = request.headers.authorizationjwt ?? request.headers.authorization;
            const token = data.token ?? header?.replace(/^Bearer\s+/i, '');
            const payload = token && entry.callbackSecrets.map(s => verify(token, s)).find(Boolean);
            if (!payload) {
                answer(403, { error: 1 });
                return;
            }
            data = payload.payload ?? payload;
        }
        if ((data.status === 2 || data.status === 6) && data.url) {
            if (!sameServer(data.url, entry.documentServer)) {
                answer(400, { error: 1 });
                return;
            }
            const file = await fetch(data.url, { signal: AbortSignal.timeout(30000) });
            if (!file.ok) {
                answer(500, { error: 1 });
                return;
            }
            const temporary = `${entry.file}.${process.pid}.saving`;
            fs.writeFileSync(temporary, Buffer.from(await file.arrayBuffer()));
            fs.renameSync(temporary, entry.file);
            entry.saves++;
            for (const wake of entry.waiters.splice(0)) wake();
        }
        answer(200, { error: 0 });
    }

    config(token, entry) {
        const base = `http://${entry.fileHost}:${this.port}`;
        const config = {
            document: {
                fileType: entry.type,
                key: entry.key,
                title: entry.title,
                url: `${base}/file/${token}/${encodeURIComponent(entry.title)}`,
                permissions: { edit: entry.edit, download: true, print: true, comment: false, review: false },
            },
            documentType: documentTypes[entry.type],
            editorConfig: {
                mode: entry.edit ? 'edit' : 'view',
                lang: 'en',
                // A user of its own, so the editor does not ask for a name to show to co-editors.
                user: { id: 'docbuilder-preview', name: 'DocBuilder Preview' },
                customization: {
                    anonymous: { request: false },
                    compactHeader: true,
                    feedback: false,
                    help: false,
                    hideRightMenu: true,
                    plugins: false,
                    // Ctrl+S saves through the callback at once, rather than when the editor closes.
                    ...(entry.edit ? { forcesave: true } : {}),
                },
                ...(entry.edit ? { callbackUrl: `${base}/callback/${token}` } : {}),
            },
            type: 'desktop',
            width: '100%',
            height: '100%',
        };
        if (entry.secret) config.token = sign(config, entry.secret);
        return config;
    }

    close() {
        this.server?.close();
    }
}

function page(entry, config) {
    const api = `${entry.documentServer}/web-apps/apps/api/documents/api.js`;
    return `<!doctype html>
<html><head><meta charset="utf-8"><title>${escapeHtml(entry.title)}</title>
<style>html,body,#editor{margin:0;height:100%;overflow:hidden;font:13px Segoe UI,sans-serif}
#message{display:none;padding:12px;background:#fde7e9;color:#8b0a14}</style></head>
<body><div id="message"></div><div id="editor"></div>
<script>
function fail(text){var m=document.getElementById('message');m.textContent=text;m.style.display='block';}
var config=${JSON.stringify(config).replace(/</g, '\\u003c')};
config.events={onError:function(e){fail('Document Server: '+(e&&e.data&&(e.data.errorDescription||e.data.errorCode)||'error'));}};
var script=document.createElement('script');
script.src=${JSON.stringify(api)};
script.onload=function(){new DocsAPI.DocEditor('editor',config);};
script.onerror=function(){fail('Document Server is not reachable at ${escapeHtml(entry.documentServer)}.');};
document.head.appendChild(script);
</script></body></html>`;
}

// The configured server, or the first local one that answers its health check.
export async function findDocumentServer(configured) {
    const candidates = configured ? [configured] : serverCandidates;
    for (const candidate of candidates) {
        const base = candidate.replace(/\/+$/, '');
        try {
            const response = await fetch(`${base}/healthcheck`, { signal: AbortSignal.timeout(3000) });
            if (response.ok && (await response.text()).trim() === 'true') return base;
        } catch { /* next */ }
    }
    throw new ViewerError(configured
        ? `Document Server at ${configured} does not answer its health check.`
        : `No Document Server answers at ${serverCandidates.join(', ')}: start one or set its address.`);
}

// The JWT secret of a Document Server installed here: "browser" signs editor configurations, "inbox"
// requests to it, "outbox" its requests to us. Each falls back to the others.
export function readLocalSecret(kind = 'browser') {
    for (const file of configCandidates) {
        try {
            const config = JSON.parse(fs.readFileSync(file, 'utf8'));
            const secret = config.services?.CoAuthoring?.secret;
            const value = secret?.[kind]?.string || secret?.browser?.string || secret?.inbox?.string;
            if (value) return value;
        } catch { /* not installed here, or not readable */ }
    }
    return null;
}

export function sign(payload, secret) {
    const part = value => Buffer.from(JSON.stringify(value)).toString('base64url');
    const head = `${part({ alg: 'HS256', typ: 'JWT' })}.${part(payload)}`;
    return `${head}.${crypto.createHmac('sha256', secret).update(head).digest('base64url')}`;
}

// The payload of an HS256 token signed with `secret`, or null.
export function verify(token, secret) {
    const [head, payload, signature] = String(token).split('.');
    if (!head || !payload || !signature) return null;
    const expected = crypto.createHmac('sha256', secret).update(`${head}.${payload}`).digest();
    const given = Buffer.from(signature, 'base64url');
    if (given.length !== expected.length || !crypto.timingSafeEqual(given, expected)) return null;
    try {
        return JSON.parse(Buffer.from(payload, 'base64url').toString('utf8'));
    } catch {
        return null;
    }
}

// The saved document is fetched only from the Document Server the editor runs on (by host name; a
// local one may call itself localhost or 127.0.0.1).
function sameServer(url, documentServer) {
    try {
        const host = new URL(url).hostname;
        const expected = new URL(documentServer).hostname;
        const loopback = h => h === 'localhost' || h === '127.0.0.1' || h === '[::1]';
        return host === expected || (loopback(host) && loopback(expected));
    } catch {
        return false;
    }
}

function readBody(request, limit) {
    return new Promise((resolve, reject) => {
        const chunks = [];
        let size = 0;
        request.on('data', chunk => {
            size += chunk.length;
            if (size > limit) {
                reject(new Error('too large'));
                request.destroy();
                return;
            }
            chunks.push(chunk);
        });
        request.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
        request.on('error', reject);
    });
}

function isInside(file, folder) {
    const relative = path.relative(folder, file);
    return relative && !relative.startsWith('..') && !path.isAbsolute(relative);
}

function escapeHtml(text) {
    return String(text).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
}
