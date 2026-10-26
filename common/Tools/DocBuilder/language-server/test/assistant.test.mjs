// The assistant's loop against a stand-in DocSpace portal whose "model" follows a script: what goes to
// the portal, how the tools answer, and that only a proposal that checks and runs reaches the user.

import { test, before, after, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LspClient } from './lsp-client.mjs';
import { findBuilder } from '../src/preview.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const templates = path.resolve(here, '../../../../../products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates');
const template = path.join(templates, 'AuditReport.docbuilder');
const uri = pathToFileURL(template).href;
const original = fs.readFileSync(template, 'utf8');
const skipRun = !findBuilder() && 'no Document Builder installed';

let portal;
let client;
let requests;
let script; // one function per model answer: (requestBody) => message
let profiles;
let refuse;

before(async () => {
    portal = http.createServer((request, response) => {
        let body = '';
        request.on('data', d => (body += d));
        request.on('end', () => {
            const entry = { method: request.method, url: request.url, authorization: request.headers.authorization, body: body ? JSON.parse(body) : null };
            requests.push(entry);
            const send = (status, value) => response.writeHead(status, { 'Content-Type': 'application/json' }).end(JSON.stringify(value));
            if (refuse) return send(401, { error: 'Unauthorized' });
            if (request.url === '/api/2.0/ai/profiles/list') return send(200, profiles);
            const match = request.url.match(/^\/api\/2\.0\/ai\/openai\/([^/]+)\/v1\/chat\/completions$/);
            if (match) {
                const step = script.shift();
                if (!step) return send(500, { error: 'the script ran out' });
                return send(200, { choices: [{ message: step(entry.body) }] });
            }
            return send(404, {});
        });
    });
    await new Promise(resolve => portal.listen(0, '127.0.0.1', resolve));
    client = new LspClient({ portalUrl: `http://127.0.0.1:${portal.address().port}/`, portalApiKey: 'test-key' });
    await client.start();
});

after(async () => {
    await client.stop();
    portal.close();
});

beforeEach(async () => {
    requests = [];
    refuse = false;
    profiles = [
        { id: 'own', name: 'OpenAI', providerType: 'openai', modelId: 'gpt', canUseTool: true },
        { id: 'oo-1', name: 'ONLYOFFICE', providerType: 'onlyoffice', modelId: 'z-ai/glm-5.3', canUseTool: true },
    ];
    client.notifications.length = 0;
    await client.request('workspace/executeCommand', { command: 'docbuilder.assistant.reset', arguments: [{ uri }] });
});

const ask = (prompt, extra = {}) => client.request('workspace/executeCommand', { command: 'docbuilder.assistant.ask', arguments: [{ uri, prompt, ...extra }] });
const call = (name, args, id = name) => ({ role: 'assistant', content: null, tool_calls: [{ id, type: 'function', function: { name, arguments: JSON.stringify(args) } }] });
const lastTool = body => body.messages.filter(m => m.role === 'tool').at(-1)?.content ?? '';

test('a question is answered through the onlyoffice profile of the portal, with the API key as a bearer token', async () => {
    script = [body => {
        assert.equal(body.model, 'z-ai/glm-5.3');
        assert.ok(body.tools.some(t => t.function.name === 'docbuilder_propose'));
        assert.match(body.messages.at(-1).content, /Request: what does this template build\?/);
        assert.match(body.messages.at(-1).content, /\s+1\| builder\.CreateFile\("xlsx"\);/);
        return { role: 'assistant', content: 'An audit trail workbook.' };
    }];
    const result = await ask('what does this template build?');
    assert.equal(result.reply, 'An audit trail workbook.');
    assert.equal(result.proposal, undefined);
    assert.ok(requests.every(r => r.authorization === 'Bearer test-key'));
    assert.ok(requests.some(r => r.url === '/api/2.0/ai/openai/oo-1/v1/chat/completions'));
});

test('the tools answer the model: API search, sample data and check', async () => {
    const broken = original.replace('oWorksheet.GetRange("A1").Select();', 'oWorksheet.GetRnge("A1").Select();');
    script = [
        () => call('docbuilder_search_api', { query: 'merge cells', editor: 'spreadsheet' }),
        body => {
            assert.match(lastTool(body), /ApiRange\.Merge\(isAcross: boolean\)/);
            return call('docbuilder_get_sample', { scenario: 'loginHistory' });
        },
        body => {
            assert.match(lastTool(body), /Scenario loginHistory/);
            assert.match(lastTool(body), /items in all/);
            return call('docbuilder_check', { text: broken });
        },
        body => {
            assert.match(lastTool(body), /line \d+:\d+ error TS2551: Property 'GetRnge' does not exist/);
            return { role: 'assistant', content: 'Found it.' };
        },
    ];
    const result = await ask('look around');
    assert.equal(result.reply, 'Found it.');
    assert.ok(client.notifications.some(n => n.method === 'docbuilder/assistantProgress' && /Searching the API: merge cells/.test(n.params.text)));
});

test('a proposal that fails the check goes back to the model; the fixed one reaches the user, built', { skip: skipRun, timeout: 180000 }, async () => {
    const changed = original.replace('var MAX_COLUMN_WIDTH = 100;', 'var MAX_COLUMN_WIDTH = 80;');
    assert.notEqual(changed, original);
    const broken = changed.replace('oWorksheet.GetRange("A1").Select();', 'oWorksheet.GetRnge("A1").Select();');
    script = [
        () => call('docbuilder_propose', { text: broken, summary: 'Narrower columns.' }, 'p1'),
        body => {
            assert.match(lastTool(body), /^Rejected, the check fails:[\s\S]*GetRnge/);
            return call('docbuilder_propose', { text: changed, summary: 'Narrower columns.' }, 'p2');
        },
    ];
    const result = await ask('make the columns narrower', { scenario: 'audit' });
    assert.equal(result.proposal.summary, 'Narrower columns.');
    assert.equal(result.proposal.text, changed);
    assert.equal(result.proposal.run.ok, true);
    assert.equal(result.proposal.run.scenario, 'audit');
    assert.ok(fs.statSync(result.proposal.run.pdf).size > 0);
    // Nothing touched the template itself.
    assert.equal(fs.readFileSync(template, 'utf8'), original);
});

test('a proposal that fails at run time goes back to the model with the template line', { skip: skipRun, timeout: 180000 }, async () => {
    // Type-correct but wrong at run time: the builder has no such sheet.
    const failing = original.replace('var oWorksheet = Api.GetActiveSheet();', 'var oWorksheet = Api.GetSheet("no such sheet");');
    script = [
        () => call('docbuilder_propose', { text: failing, summary: 'x' }),
        body => {
            assert.match(lastTool(body), /^Rejected, the run fails:/);
            return { role: 'assistant', content: 'I give up.' };
        },
    ];
    const result = await ask('break it');
    assert.equal(result.proposal, undefined);
    assert.equal(result.reply, 'I give up.');
});

test('the conversation continues: the next request carries the earlier turns', async () => {
    script = [() => ({ role: 'assistant', content: 'first' })];
    await ask('one');
    script = [body => {
        assert.equal(body.messages.filter(m => m.role === 'user').length, 2);
        return { role: 'assistant', content: 'second' };
    }];
    assert.equal((await ask('two')).reply, 'second');
});

test('a refused key and a portal without AI are reported in words', async () => {
    refuse = true;
    assert.match((await ask('hi')).error.message, /refused the API key \(401\)/);
    refuse = false;
    profiles = [{ id: 'own', providerType: 'openai', modelId: 'gpt' }];
    assert.match((await ask('hi')).error.message, /no ONLYOFFICE AI profile/);
    profiles = [{ id: 'oo', providerType: 'onlyoffice', modelId: 'small', canUseTool: false }];
    assert.match((await ask('hi')).error.message, /cannot call tools/);
});
