// Runs every scenario of every repository sample through a real Document Builder via the server's
// docbuilder.run command. Skipped where no builder is installed.

import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LspClient } from './lsp-client.mjs';
import { findBuilder } from '../src/preview.mjs';
import { readSample } from '../src/samples.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const templates = path.resolve(here, '../../../../../products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates');
const builder = findBuilder();
const skip = !builder && 'no Document Builder installed';

const runs = fs.readdirSync(templates)
    .filter(f => f.endsWith('.sample.json'))
    .flatMap(f => readSample(path.join(templates, f)).scenarios.map(s => ({ report: f.replace('.sample.json', ''), scenario: s.name })));

let client;

before(async () => {
    client = new LspClient();
    await client.start();
});

after(async () => {
    await client.stop();
});

const uriOf = report => pathToFileURL(path.join(templates, `${report}.docbuilder`)).href;
const run = (report, scenario) => client.request('workspace/executeCommand', { command: 'docbuilder.run', arguments: [{ uri: uriOf(report), scenario }] });

test('the scenario list of a template comes from its sample', async () => {
    const list = await client.request('workspace/executeCommand', { command: 'docbuilder.scenarios', arguments: [{ uri: uriOf('AuditReport') }] });
    assert.equal(list.default, 'audit');
    assert.ok(list.scenarios.some(s => s.name === 'ru-RU' && s.description));
});

for (const { report, scenario } of runs) {
    test(`${report} / ${scenario} builds its document and a PDF of it`, { skip, timeout: 180000 }, async () => {
        const result = await run(report, scenario);
        assert.equal(result.error?.message, undefined, `${result.error?.message}\n${result.stderr}`);
        assert.ok(result.ok);
        assert.equal(result.scenario, scenario);
        assert.ok(fs.statSync(result.document).size > 0, 'document saved');
        assert.ok(fs.statSync(result.pdf).size > 0, 'PDF saved');
    });
}

test('an unknown scenario is reported, not run', { skip }, async () => {
    const result = await run('AuditReport', 'nope');
    assert.equal(result.ok, false);
    assert.match(result.error.message, /no scenario "nope"/);
});

test('an error is reported at the template line that caused it', { skip, timeout: 180000 }, async () => {
    const file = path.join(templates, 'AuditReport.docbuilder');
    const uri = pathToFileURL(file).href;
    const lines = fs.readFileSync(file, 'utf8').split('\n');
    const target = lines.findIndex(l => l.includes('oWorksheet.GetRange("A1").Select();'));
    lines[target] = lines[target].replace('GetRange("A1")', 'GetRnge("A1")');

    // The unsaved text in the editor is what runs.
    client.notify('textDocument/didOpen', { textDocument: { uri, languageId: 'docbuilder', version: 1, text: lines.join('\n') } });
    const result = await run('AuditReport');
    client.notify('textDocument/didClose', { textDocument: { uri } });

    assert.equal(result.ok, false);
    assert.match(result.error.message, /GetRnge/);
    assert.equal(path.resolve(result.error.file), path.resolve(file));
    assert.equal(result.error.line, target);
});
