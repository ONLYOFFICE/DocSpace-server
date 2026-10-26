// Drives the language server the way an IDE does, over the repository's own templates.

import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { LspClient } from './lsp-client.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const templates = path.resolve(here, '../../../../../products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates');
const auditFile = path.join(templates, 'AuditReport.docbuilder');
const uri = pathToFileURL(auditFile).href;
const original = fs.readFileSync(auditFile, 'utf8');
const lines = original.split('\n');

let client;
let version = 1;

before(async () => {
    client = new LspClient();
    const init = await client.start();
    assert.ok(init.capabilities.completionProvider);
    client.notify('textDocument/didOpen', { textDocument: { uri, languageId: 'docbuilder', version, text: original } });
});

after(async () => {
    await client.stop();
});

function positionOf(needle, occurrence = 0) {
    let seen = 0;
    for (let line = 0; line < lines.length; line++) {
        let from = 0;
        for (;;) {
            const character = lines[line].indexOf(needle, from);
            if (character < 0) break;
            if (seen++ === occurrence) return { line, character };
            from = character + 1;
        }
    }
    throw new Error(`${needle} not found`);
}

function change(text) {
    client.notify('textDocument/didChange', {
        textDocument: { uri, version: ++version },
        contentChanges: [{ text }],
    });
}

test('a template in production has no errors, placeholders included', async () => {
    const diagnostics = await client.diagnosticsFor(uri);
    assert.deepEqual(diagnostics.filter(d => d.severity === 1), []);
});

test('completion after Api. offers the spreadsheet API with its documentation', async () => {
    const at = positionOf('Api.CreateColorFromRGB');
    const result = await client.request('textDocument/completion', { textDocument: { uri }, position: { line: at.line, character: at.character + 4 } });
    const item = result.items.find(i => i.label === 'GetActiveSheet');
    assert.ok(item, 'GetActiveSheet offered');
    const resolved = await client.request('completionItem/resolve', item);
    assert.match(resolved.detail, /GetActiveSheet\(\): .*ApiWorksheet/);
    assert.match(resolved.documentation.value, /api\.onlyoffice\.com/);
});

test('hover shows the signature and the reference text', async () => {
    const at = positionOf('GetRange(');
    const hover = await client.request('textDocument/hover', { textDocument: { uri }, position: { line: at.line, character: at.character + 2 } });
    assert.match(hover.contents.value, /GetRange\(Range1: string \| SpreadsheetApi\.ApiRange/);
});

test('signature help names the parameter being typed', async () => {
    const at = positionOf('CreateColorFromRGB(');
    const help = await client.request('textDocument/signatureHelp', { textDocument: { uri }, position: { line: at.line, character: at.character + 'CreateColorFromRGB('.length } });
    assert.ok(help.signatures.length);
    assert.equal(help.activeParameter, 0);
    assert.match(help.signatures[help.activeSignature].parameters[0].label, /^r/);
});

test('go to definition of an API method lands in the declarations', async () => {
    const at = positionOf('GetActiveSheet');
    const locations = await client.request('textDocument/definition', { textDocument: { uri }, position: { line: at.line, character: at.character + 1 } });
    assert.ok(locations.some(l => l.uri.endsWith('onlyoffice-docbuilder.d.ts')));
});

test('a typo, a wrong argument count and builder inside code are reported where they are written', async () => {
    const broken = original
        .replace('oWorksheet.GetRange("A1").Select();', 'oWorksheet.GetRnge("A1").Select();\nif (true) { builder.CloseFile(); }');
    change(broken);
    const diagnostics = await client.diagnosticsFor(uri);
    const brokenLines = broken.split('\n');
    const typo = diagnostics.find(d => /GetRnge/.test(d.message));
    assert.ok(typo, 'typo reported');
    assert.equal(brokenLines[typo.range.start.line].slice(typo.range.start.character, typo.range.end.character), 'GetRnge');
    assert.ok(diagnostics.some(d => d.code === 'TS90001'), 'builder inside code reported');
});

test('fixing the text clears the errors', async () => {
    change(original);
    const diagnostics = await client.diagnosticsFor(uri);
    assert.deepEqual(diagnostics.filter(d => d.severity === 1), []);
});

test('the outline lists the template functions', async () => {
    const symbols = await client.request('textDocument/documentSymbol', { textDocument: { uri } });
    assert.ok(symbols.some(s => s.name === 'toColumnName'));
});
