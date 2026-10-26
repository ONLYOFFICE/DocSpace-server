// A searchable index of the Document Builder declarations: every method and property of every class
// of every editor, with its signature and the reference text - what an agent needs to write a call
// without reading 4 MB of declarations.

import fs from 'node:fs';
import ts from 'typescript';
import { findDeclarations } from './project.mjs';

export const editors = {
    DocumentApi: 'document',
    SpreadsheetApi: 'spreadsheet',
    PresentationApi: 'presentation',
    PdfApi: 'pdf',
    FormApi: 'form',
};

let cached;

export function apiIndex(declarations = findDeclarations()) {
    if (cached?.file === declarations) return cached.entries;
    const source = ts.createSourceFile(declarations, fs.readFileSync(declarations, 'utf8'), ts.ScriptTarget.Latest, true);
    const entries = [];

    const docOf = node => {
        const jsdoc = ts.getJSDocCommentsAndTags(node).find(ts.isJSDoc);
        if (!jsdoc) return { text: '', see: null, params: [] };
        const text = typeof jsdoc.comment === 'string' ? jsdoc.comment : ts.getTextOfJSDocComment(jsdoc.comment) ?? '';
        const tags = jsdoc.tags ?? [];
        // From the raw text: the JSDoc parser reads the "https" of a @see URL as a link name.
        const see = jsdoc.getText(source).match(/@see\s+(\S+)/);
        return {
            text: text.trim(),
            see: see ? see[1] : null,
            params: tags.filter(ts.isJSDocParameterTag).map(t => `${t.name.getText(source)}: ${(ts.getTextOfJSDocComment(t.comment) ?? '').trim()}`),
            returns: (ts.getTextOfJSDocComment(tags.find(t => t.tagName.text === 'returns')?.comment) ?? '').trim(),
        };
    };

    const addMembers = (editor, typeName, members) => {
        for (const member of members) {
            if (!member.name) continue;
            const name = member.name.getText(source);
            entries.push({
                editor,
                type: typeName,
                name,
                kind: ts.isMethodSignature(member) ? 'method' : 'property',
                signature: member.getText(source).replace(/\s+/g, ' ').trim(),
                ...docOf(member),
            });
        }
    };

    for (const statement of source.statements) {
        if (ts.isInterfaceDeclaration(statement)) {
            addMembers('builder', statement.name.text, statement.members);
        } else if (ts.isVariableStatement(statement)) {
            for (const declaration of statement.declarationList.declarations) {
                entries.push({
                    editor: 'builder', type: '(global)', name: declaration.name.getText(source), kind: 'global',
                    signature: declaration.getText(source).replace(/\s+/g, ' '), ...docOf(statement),
                });
            }
        } else if (ts.isModuleDeclaration(statement) && statement.body && ts.isModuleBlock(statement.body)) {
            const editor = editors[statement.name.text] ?? statement.name.text;
            for (const inner of statement.body.statements) {
                if (ts.isInterfaceDeclaration(inner)) {
                    entries.push({ editor, type: inner.name.text, name: inner.name.text, kind: 'class', signature: `interface ${inner.name.text}`, ...docOf(inner) });
                    addMembers(editor, inner.name.text, inner.members);
                } else if (ts.isTypeAliasDeclaration(inner)) {
                    entries.push({ editor, type: inner.name.text, name: inner.name.text, kind: 'type', signature: inner.getText(source).replace(/\s+/g, ' '), ...docOf(inner) });
                }
            }
        }
    }

    cached = { file: declarations, entries };
    return entries;
}

// Plural to singular, so "cells" finds "cell range" and "Merge" outranks "UnMerge".
const words = text => (text.toLowerCase().match(/[a-z0-9]+/g) ?? [])
    .filter(w => w.length > 1)
    .map(w => (w.length > 3 && w.endsWith('s') && !w.endsWith('ss') ? w.slice(0, -1) : w));

// "ApiRange.SetFillColor", "SetFillColor", "fill color of a range", "spreadsheet merge cells": a name
// match outranks a description match, and the editor narrows the field.
export function searchApi(query, { editor, limit = 15 } = {}) {
    const entries = apiIndex().filter(e => !editor || e.editor === editor || e.editor === 'builder');
    const q = query.trim();
    const dotted = q.match(/^(?:(\w+)\.)?(\w+)\.(\w+)$/) ?? q.match(/^()(\w+)\.(\w+)$/);
    if (dotted) {
        const [, ns, type, name] = dotted;
        const exact = entries.filter(e => e.type === type && e.name === name && (!ns || editors[ns] === e.editor || ns === e.editor));
        if (exact.length) return exact.slice(0, limit);
    }

    const terms = words(q);
    const scored = [];
    for (const entry of entries) {
        const name = entry.name.toLowerCase();
        const type = entry.type.toLowerCase();
        const doc = entry.text.toLowerCase();
        let score = 0;
        if (name === q.toLowerCase()) score += 50;
        for (const term of terms) {
            if (name === term) score += 12;
            else if (name.includes(term)) score += 6;
            if (type.includes(term)) score += 2;
            if (doc.includes(term)) score += 1;
        }
        // Every term found somewhere beats a single strong hit.
        if (terms.length > 1 && terms.every(t => name.includes(t) || type.includes(t) || doc.includes(t))) score += 8;
        if (score) scored.push({ entry, score });
    }
    return scored.sort((a, b) => b.score - a.score || a.entry.name.localeCompare(b.entry.name)).slice(0, limit).map(s => s.entry);
}

export function formatEntry(entry, { full = false } = {}) {
    const lines = [`[${entry.editor}] ${entry.type === '(global)' || entry.kind === 'class' || entry.kind === 'type' ? '' : entry.type + '.'}${entry.signature}`];
    if (entry.text) lines.push(full ? entry.text : entry.text.split('\n')[0]);
    if (full && entry.params?.length) lines.push(...entry.params.map(p => `  @param ${p}`));
    if (full && entry.returns) lines.push(`  @returns ${entry.returns}`);
    if (entry.see) lines.push(`  ${entry.see}`);
    return lines.join('\n');
}
