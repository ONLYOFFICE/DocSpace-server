// The editor mode and the way back: an editable copy in a Document Server editor, the hand edits it
// saves through the callback, the list of them, and the assistant held to them. Document Server and the
// portal are stand-ins here, so only Document Builder has to be installed.

import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LspClient } from './lsp-client.mjs';
import { findBuilder, runTemplate } from '../src/preview.mjs';
import { sign, verify } from '../src/viewer.mjs';
import { rectangles, diffWorkbooks } from '../src/xlsx.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const templates = path.resolve(here, '../../../../../products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates');
const template = path.join(templates, 'FormFillingReport.docbuilder');
const uri = pathToFileURL(template).href;
const original = fs.readFileSync(template, 'utf8');
const skip = !findBuilder() && 'no Document Builder installed';
const secret = 'stand-in-secret';

// The hand edits the tests make: what the user would do in the editor, written as template code so
// that a proposal can carry them exactly.
const save = 'builder.SaveFile("xlsx", "${tempFileName}");';
const carried = original.replace(save, 'Api.GetActiveSheet().GetRange("A2:B3").SetBold(true);\nApi.GetActiveSheet().GetRange("C1").SetFillColor(Api.CreateColorFromRGB(255, 230, 153));\n' + save);

let documentServer;
let portal;
let client;
let editedFile; // what the stand-in Document Server hands back on the next save
let callbackUrl;
let documentKey;
let script = [];

const command = (name, argument) => client.request('workspace/executeCommand', { command: name, arguments: [argument] });
const configOf = html => JSON.parse(html.match(/var config=(\{.*?\});\n/s)[1].replace(/\\u003c/g, '<'));
const call = (name, args) => ({ role: 'assistant', content: null, tool_calls: [{ id: name, type: 'function', function: { name, arguments: JSON.stringify(args) } }] });
const lastTool = body => body.messages.filter(m => m.role === 'tool').at(-1)?.content ?? '';

function json(response, status, value) {
    response.writeHead(status, { 'Content-Type': 'application/json' }).end(JSON.stringify(value));
}
function body(request) {
    return new Promise(resolve => {
        let text = '';
        request.on('data', d => (text += d));
        request.on('end', () => resolve(text ? JSON.parse(text) : null));
    });
}

before(async () => {
    // Document Server: a health check, the command service (forcesave answers through the callback, as
    // the real one does) and the cache the saved file is downloaded from.
    documentServer = http.createServer(async (request, response) => {
        if (request.url === '/healthcheck') return response.end('true');
        if (request.url === '/cache/edited.xlsx') return response.end(fs.readFileSync(editedFile));
        if (request.url === '/command' && request.method === 'POST') {
            const payload = verify((await body(request)).token, secret);
            if (!payload) return json(response, 200, { error: 6 });
            if (payload.c !== 'forcesave' || payload.key !== documentKey) return json(response, 200, { error: 1 });
            if (!editedFile) return json(response, 200, { error: 4 });
            json(response, 200, { error: 0 });
            const saved = { key: documentKey, status: 6, url: `http://127.0.0.1:${documentServer.address().port}/cache/edited.xlsx` };
            await fetch(callbackUrl, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ token: sign(saved, secret) }) });
            return undefined;
        }
        return json(response, 404, {});
    });
    portal = http.createServer(async (request, response) => {
        const payload = await body(request);
        if (request.url === '/api/2.0/ai/profiles/list') return json(response, 200, [{ id: 'oo', providerType: 'onlyoffice', modelId: 'm', canUseTool: true }]);
        const step = script.shift();
        return step ? json(response, 200, { choices: [{ message: step(payload) }] }) : json(response, 500, {});
    });
    await new Promise(resolve => documentServer.listen(0, '127.0.0.1', resolve));
    await new Promise(resolve => portal.listen(0, '127.0.0.1', resolve));
    client = new LspClient({
        documentServerUrl: `http://127.0.0.1:${documentServer.address().port}`,
        documentServerSecret: secret,
        portalUrl: `http://127.0.0.1:${portal.address().port}`,
        portalApiKey: 'key',
    });
    await client.start();
});

after(async () => {
    await client.stop();
    documentServer.close();
    portal.close();
});

// Builds the template, opens its document in the editor and plays Document Server loading the page.
async function openEditor() {
    const built = await command('docbuilder.run', { uri });
    assert.equal(built.ok, true, built.error?.message);
    const opened = await command('docbuilder.view', { document: built.document, edit: true });
    assert.equal(opened.error, undefined, opened.error?.message);
    const config = configOf(await (await fetch(opened.url)).text());
    callbackUrl = config.editorConfig.callbackUrl;
    documentKey = config.document.key;
    return { built, opened, config };
}

// A document as the user's edits would leave it, kept outside the run folders a new run clears.
async function editedDocument(text) {
    const run = await runTemplate({ template, readText: f => (path.resolve(f) === path.resolve(template) ? text : fs.readFileSync(f, 'utf8')) });
    assert.equal(run.ok, true, run.error?.message);
    const copy = path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'docbuilder-edited-')), 'edited.xlsx');
    fs.copyFileSync(run.document, copy);
    return copy;
}

test('areas of changed cells come back as the fewest rectangles', () => {
    assert.deepEqual(rectangles([[6, 1], [7, 1], [8, 1], [6, 2], [7, 2], [8, 2], [0, 0]]), ['A1', 'B7:C9']);
    assert.deepEqual(rectangles([[0, 0], [2, 0]]), ['A1', 'A3']);
});

test('the editor opens an editable copy that saves through the callback; the built document stays', { skip, timeout: 120000 }, async () => {
    const { built, opened, config } = await openEditor();
    assert.equal(config.editorConfig.mode, 'edit');
    assert.equal(config.document.permissions.edit, true);
    assert.equal(config.editorConfig.customization.forcesave, true);
    assert.match(config.editorConfig.callbackUrl, /^http:\/\/127\.0\.0\.1:\d+\/callback\/[0-9a-f]{32}$/);
    assert.match(opened.edited, /[\\/]_edits[\\/]FormFillingReport[\\/]\d+-\w+[\\/]FormFillingReport\.xlsx$/);
    assert.ok(fs.readFileSync(opened.edited).equals(fs.readFileSync(built.document)));

    // Nothing changed yet: Document Server has nothing to save, and there is nothing to list.
    editedFile = null;
    const none = await command('docbuilder.edits', { document: opened.edited });
    assert.equal(none.saved, false);
    assert.equal(none.text, 'No differences.');
});

test('the hand edits are saved, then listed by property and area', { skip, timeout: 180000 }, async () => {
    editedFile = await editedDocument(carried);
    const { opened, built } = await openEditor();
    const edits = await command('docbuilder.edits', { document: opened.edited });
    assert.equal(edits.error, undefined, edits.error?.message);
    assert.equal(edits.saved, true);
    // Each area names what its first cell holds, which is how the code that writes it is found.
    assert.match(edits.text, /A2:B3 \(first "[^"]+"\) font\.bold: false -> true/);
    assert.match(edits.text, /C1( \("[^"]*"\))? fill: "none" -> "#FFE699"/);
    assert.equal(edits.format, 5);
    assert.equal(edits.data, 0);
    // The saved file replaced the copy; the document the run built is untouched.
    assert.ok(fs.readFileSync(opened.edited).equals(fs.readFileSync(editedFile)));
    assert.equal(diffWorkbooks(built.document, edits.original).changes.length, 0);
});

test('a callback without a valid token, or with a file from elsewhere, is refused', { skip, timeout: 120000 }, async () => {
    await openEditor();
    const post = value => fetch(callbackUrl, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) });
    const saved = { key: documentKey, status: 6, url: `http://127.0.0.1:${documentServer.address().port}/cache/edited.xlsx` };
    assert.equal((await post({ token: sign(saved, 'another secret') })).status, 403);
    assert.equal((await post(saved)).status, 403);
    assert.equal((await post({ token: sign({ ...saved, url: 'http://example.invalid/x.xlsx' }, secret) })).status, 400);
    assert.deepEqual(await (await post({ token: sign({ key: documentKey, status: 1 }, secret) })).json(), { error: 0 });
});

test('the assistant carries the hand edits: a proposal that leaves some is sent back with them', { skip, timeout: 240000 }, async () => {
    editedFile = await editedDocument(carried);
    const { opened } = await openEditor();
    script = [
        body => {
            assert.match(body.messages.at(-1).content, /Hand edits of the document built on scenario/);
            assert.match(body.messages.at(-1).content, /A2:B3 \(first "[^"]+"\) font\.bold: false -> true/);
            return call('docbuilder_propose', { text: original, summary: 'nothing' });
        },
        body => {
            assert.match(lastTool(body), /^Rejected, the document your text builds still differs from the edited one/);
            assert.match(lastTool(body), /C1( \("[^"]*"\))? fill/);
            return call('docbuilder_propose', { text: carried, summary: 'Bold names, highlighted C1.' });
        },
    ];
    const answer = await command('docbuilder.assistant.ask', { uri, prompt: 'carry my edits', edits: opened.edited });
    assert.equal(answer.error, undefined, answer.error?.message);
    assert.equal(answer.proposal.text, carried);
    assert.equal(answer.proposal.remaining, 0);
    assert.equal(fs.readFileSync(template, 'utf8'), original);
});
