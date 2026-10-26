#!/usr/bin/env node
// Language server for ONLYOFFICE Document Builder scripts (*.docbuilder), over stdio.
//
// Every folder that holds an opened template is a project of its own (TemplateProject); the editor
// only ever talks about real files, and the masking of ${placeholders} stays inside the project.

import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import {
    createConnection, ProposedFeatures, TextDocuments, TextDocumentSyncKind, DiagnosticSeverity,
    CompletionItemKind, MarkupKind, SymbolKind,
} from 'vscode-languageserver/node.js';
import { TextDocument } from 'vscode-languageserver-textdocument';
import { TemplateProject, displayText, findDeclarations, ts } from './project.mjs';
import fs from 'node:fs';
import { previewCommands } from './preview.mjs';
import { Viewer } from './viewer.mjs';
import { assistantCommands } from './assistant.mjs';

const connection = createConnection(ProposedFeatures.all);
const documents = new TextDocuments(TextDocument);
const projects = new Map();
let declarations;
let settings = {};

const fileOf = uri => fileURLToPath(uri);
const uriOf = file => pathToFileURL(file).href;

function projectOf(file) {
    const folder = path.dirname(file).toLowerCase();
    if (!projects.has(folder)) projects.set(folder, new TemplateProject(path.dirname(file), declarations));
    return projects.get(folder);
}

function context(uri) {
    const file = fileOf(uri);
    return { file, project: projectOf(file), document: documents.get(uri) };
}

// ---------- lifecycle ----------

connection.onInitialize(params => {
    // initializationOptions: { declarations?, builderPath?, samplesFolder?,
    //                          documentServerUrl?, documentServerSecret?, fileHost?,
    //                          portalUrl?, portalApiKey?, profileId? }
    settings = params.initializationOptions ?? {};
    declarations = settings.declarations ?? findDeclarations();
    return {
        capabilities: {
            textDocumentSync: TextDocumentSyncKind.Incremental,
            completionProvider: { triggerCharacters: ['.', '"', "'"], resolveProvider: true },
            hoverProvider: true,
            signatureHelpProvider: { triggerCharacters: ['(', ','] },
            definitionProvider: true,
            referencesProvider: true,
            documentSymbolProvider: true,
            executeCommandProvider: {
                commands: ['docbuilder.run', 'docbuilder.scenarios', 'docbuilder.view', 'docbuilder.edits',
                    'docbuilder.assistant.ask', 'docbuilder.assistant.reset', 'docbuilder.assistant.cancel'],
            },
        },
        serverInfo: { name: 'docbuilder-language-server', version: '0.1.0' },
    };
});

documents.onDidOpen(e => {
    const { file, project } = context(e.document.uri);
    project.open(file, e.document.getText());
    schedule(e.document.uri);
});

documents.onDidChangeContent(e => {
    const { file, project } = context(e.document.uri);
    project.change(file, e.document.getText());
    schedule(e.document.uri);
});

documents.onDidClose(e => {
    const { file, project } = context(e.document.uri);
    project.close(file);
    connection.sendDiagnostics({ uri: e.document.uri, diagnostics: [] });
});

// Another template changing on disk can change what a shared script sees.
connection.onDidChangeWatchedFiles(() => {
    for (const project of projects.values()) project.touch();
    for (const doc of documents.all()) schedule(doc.uri);
});

// ---------- diagnostics ----------

const pending = new Map();

function schedule(uri) {
    clearTimeout(pending.get(uri));
    pending.set(uri, setTimeout(() => publish(uri), 250));
}

function publish(uri) {
    pending.delete(uri);
    const { file, project, document } = context(uri);
    if (!document) return;
    const diagnostics = project.diagnostics(file).map(d => ({
        range: { start: document.positionAt(d.start), end: document.positionAt(d.start + d.length) },
        severity: d.category === ts.DiagnosticCategory.Error ? DiagnosticSeverity.Error
            : d.category === ts.DiagnosticCategory.Warning ? DiagnosticSeverity.Warning : DiagnosticSeverity.Information,
        code: `TS${d.code}`,
        source: 'docbuilder',
        message: d.message,
    }));
    connection.sendDiagnostics({ uri, diagnostics });
}

// ---------- completion ----------

const kinds = {
    method: CompletionItemKind.Method,
    property: CompletionItemKind.Property,
    function: CompletionItemKind.Function,
    var: CompletionItemKind.Variable,
    let: CompletionItemKind.Variable,
    const: CompletionItemKind.Constant,
    'local var': CompletionItemKind.Variable,
    parameter: CompletionItemKind.Variable,
    interface: CompletionItemKind.Interface,
    type: CompletionItemKind.TypeParameter,
    module: CompletionItemKind.Module,
    keyword: CompletionItemKind.Keyword,
    string: CompletionItemKind.Value,
};

connection.onCompletion(params => {
    const { file, project, document } = context(params.textDocument.uri);
    if (!document) return null;
    const offset = document.offsetAt(params.position);
    const result = project.completions(file, offset);
    if (!result) return null;
    return {
        isIncomplete: false,
        items: result.entries.map(e => ({
            label: e.name,
            kind: kinds[e.kind] ?? CompletionItemKind.Text,
            sortText: e.sortText,
            insertText: e.insertText,
            textEdit: e.replacementSpan ? {
                range: { start: document.positionAt(e.replacementSpan.start), end: document.positionAt(e.replacementSpan.start + e.replacementSpan.length) },
                newText: e.insertText ?? e.name,
            } : undefined,
            data: { uri: params.textDocument.uri, offset, name: e.name },
        })),
    };
});

connection.onCompletionResolve(item => {
    const { uri, offset, name } = item.data ?? {};
    if (!uri) return item;
    const { file, project } = context(uri);
    const details = project.completionDetails(file, offset, name);
    if (details) {
        item.detail = displayText(details.displayParts);
        item.documentation = { kind: MarkupKind.Markdown, value: documentation(details.documentation, details.tags) };
    }
    return item;
});

// ---------- hover, signature help ----------

connection.onHover(params => {
    const { file, project, document } = context(params.textDocument.uri);
    if (!document) return null;
    const info = project.quickInfo(file, document.offsetAt(params.position));
    if (!info) return null;
    return {
        contents: {
            kind: MarkupKind.Markdown,
            value: ['```typescript', displayText(info.displayParts), '```', documentation(info.documentation, info.tags)].join('\n'),
        },
        range: { start: document.positionAt(info.textSpan.start), end: document.positionAt(info.textSpan.start + info.textSpan.length) },
    };
});

connection.onSignatureHelp(params => {
    const { file, project, document } = context(params.textDocument.uri);
    if (!document) return null;
    const help = project.signatureHelp(file, document.offsetAt(params.position));
    if (!help) return null;
    return {
        activeSignature: help.selectedItemIndex,
        activeParameter: help.argumentIndex,
        signatures: help.items.map(item => ({
            label: displayText([...item.prefixDisplayParts, ...joinParameters(item), ...item.suffixDisplayParts]),
            documentation: { kind: MarkupKind.Markdown, value: documentation(item.documentation, item.tags) },
            parameters: item.parameters.map(p => ({
                label: displayText(p.displayParts),
                documentation: { kind: MarkupKind.Markdown, value: displayText(p.documentation) },
            })),
        })),
    };
});

function joinParameters(item) {
    return item.parameters.flatMap((p, i) => (i ? [...item.separatorDisplayParts, ...p.displayParts] : p.displayParts));
}

// The declarations carry the reference pages' Markdown; tags come back as @param/@returns/@see lines.
function documentation(parts, tags) {
    const lines = [displayText(parts)];
    for (const tag of tags ?? []) {
        const text = displayText(tag.text);
        if (tag.name === 'see') lines.push(`[Reference](${text.trim()})`);
        else if (tag.name === 'param') lines.push(`*@param* ${text.replace(/^(\S+)/, '`$1` —')}`);
        else if (tag.name === 'returns') lines.push(`*@returns* ${text}`);
        else lines.push(`*@${tag.name}* ${text}`);
    }
    return lines.filter(Boolean).join('\n\n');
}

// ---------- navigation ----------

function toLocations(entries) {
    return entries.map(e => {
        const doc = TextDocument.create(uriOf(e.file), 'docbuilder', 0, e.text ?? '');
        return { uri: uriOf(e.file), range: { start: doc.positionAt(e.start), end: doc.positionAt(e.start + e.length) } };
    });
}

connection.onDefinition(params => {
    const { file, project, document } = context(params.textDocument.uri);
    return document ? toLocations(project.definitions(file, document.offsetAt(params.position))) : null;
});

connection.onReferences(params => {
    const { file, project, document } = context(params.textDocument.uri);
    return document ? toLocations(project.references(file, document.offsetAt(params.position))) : null;
});

const symbolKinds = { function: SymbolKind.Function, var: SymbolKind.Variable, let: SymbolKind.Variable, const: SymbolKind.Constant, property: SymbolKind.Property, method: SymbolKind.Method };

connection.onDocumentSymbol(params => {
    const { file, project, document } = context(params.textDocument.uri);
    if (!document) return null;
    const convert = item => {
        const span = item.spans[0];
        const range = { start: document.positionAt(span.start), end: document.positionAt(span.start + span.length) };
        return {
            name: item.text,
            kind: symbolKinds[item.kind] ?? SymbolKind.Variable,
            range,
            selectionRange: item.nameSpan
                ? { start: document.positionAt(item.nameSpan.start), end: document.positionAt(item.nameSpan.start + item.nameSpan.length) }
                : range,
            children: (item.childItems ?? []).map(convert),
        };
    };
    return (project.outline(file).childItems ?? []).map(convert);
});

// ---------- commands ----------

// A file's text as the editor holds it, unsaved changes included.
function readText(file) {
    const open = documents.all().find(d => path.resolve(fileOf(d.uri)).toLowerCase() === path.resolve(file).toLowerCase());
    if (open) return open.getText();
    try {
        return fs.readFileSync(file, 'utf8');
    } catch {
        return undefined;
    }
}

const getSettings = () => settings;
// One viewer for both: the assistant reads the hand edits of a document the editor has open.
const viewer = new Viewer();
connection.onShutdown(() => viewer.close());
const commandHandlers = [
    previewCommands({ readText, getSettings, viewer }),
    assistantCommands({ readText, getSettings, viewer, sendProgress: progress => connection.sendNotification('docbuilder/assistantProgress', progress) }),
];

connection.onExecuteCommand(async params => {
    for (const handler of commandHandlers) {
        const result = await handler(params.command, params.arguments?.[0]);
        if (result !== undefined) return result;
    }
    return null;
});

documents.listen(connection);
connection.listen();
