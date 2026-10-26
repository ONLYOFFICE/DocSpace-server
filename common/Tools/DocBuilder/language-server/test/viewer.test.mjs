// The live viewer: the page and the document the language server serves to Document Server.
// Skipped where no Document Builder (to build a document) or no Document Server is available.

import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LspClient } from './lsp-client.mjs';
import { findBuilder } from '../src/preview.mjs';
import { findDocumentServer, readLocalSecret } from '../src/viewer.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const templates = path.resolve(here, '../../../../../products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates');
const documentServer = await findDocumentServer().catch(() => null);
const skip = (!findBuilder() && 'no Document Builder installed') || (!documentServer && 'no Document Server running');

let client;
let built;

const command = (name, argument) => client.request('workspace/executeCommand', { command: name, arguments: [argument] });

before(async () => {
    client = new LspClient();
    await client.start();
    if (!skip) built = await command('docbuilder.run', { uri: pathToFileURL(path.join(templates, 'RoomIndexExport.docbuilder')).href });
});

after(async () => {
    await client.stop();
});

function configOf(html) {
    return JSON.parse(html.match(/var config=(\{.*?\});\n/s)[1].replace(/\\u003c/g, '<'));
}

test('the viewer page embeds the editor of the running Document Server, read-only and signed', { skip }, async () => {
    const opened = await command('docbuilder.view', { document: built.document });
    assert.equal(opened.error, undefined, opened.error?.message);
    assert.match(opened.url, /^http:\/\/127\.0\.0\.1:\d+\/view\/[0-9a-f]{32}$/);

    const html = await (await fetch(opened.url)).text();
    assert.ok(html.includes(`${documentServer}/web-apps/apps/api/documents/api.js`));
    const config = configOf(html);
    assert.equal(config.documentType, 'cell');
    assert.equal(config.editorConfig.mode, 'view');
    assert.equal(config.document.permissions.edit, false);

    const secret = readLocalSecret();
    if (secret) {
        const [head, payload, signature] = config.token.split('.');
        assert.equal(crypto.createHmac('sha256', secret).update(`${head}.${payload}`).digest('base64url'), signature);
        assert.equal(JSON.parse(Buffer.from(payload, 'base64url').toString()).document.key, config.document.key);
    }

    // What Document Server downloads is the document the run built, byte for byte.
    const file = Buffer.from(await (await fetch(config.document.url)).arrayBuffer());
    assert.ok(file.equals(fs.readFileSync(built.document)));
});

test('each build gets its own document key, so the viewer never shows a cached old one', { skip, timeout: 180000 }, async () => {
    const first = configOf(await (await fetch((await command('docbuilder.view', { document: built.document })).url)).text());
    const again = await command('docbuilder.run', { uri: pathToFileURL(path.join(templates, 'RoomIndexExport.docbuilder')).href });
    const second = configOf(await (await fetch((await command('docbuilder.view', { document: again.document })).url)).text());
    assert.notEqual(first.document.key, second.document.key);
    // A new run clears the folders of older ones: the earlier document is gone now.
    built = again;
});

test('only documents a preview run built are served', { skip }, async () => {
    const outside = await command('docbuilder.view', { document: path.join(templates, 'RoomIndexExport.sample.json') });
    assert.match(outside.error.message, /Only a document a preview run built/);
    const opened = await command('docbuilder.view', { document: built.document });
    const base = opened.url.replace(/\/view\/.*/, '');
    assert.equal((await fetch(`${base}/file/${'0'.repeat(32)}/x.xlsx`)).status, 404);
    assert.equal((await fetch(`${base}/view/../../etc/passwd`)).status, 404);
});
