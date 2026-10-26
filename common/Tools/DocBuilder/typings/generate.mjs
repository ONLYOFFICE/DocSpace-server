// Generates TypeScript declarations for ONLYOFFICE Document Builder scripts (*.docbuilder) from the
// Markdown reference of the api.onlyoffice.com site (site/docs/office-api/usage-api).
//
//   node generate.mjs <path to api.onlyoffice.com checkout> [--ref <git ref>] [output .d.ts]
//
// --ref reads the pages of that commit (origin/master is what the site publishes) without touching
// the checkout's working tree; without it the working tree is read as it is.
//
// Every editor (document, spreadsheet, presentation, pdf, form) gets its own namespace, because the
// editors share class names with different members (ApiRange, ApiDocument...). The global `Api` is the
// intersection of all editors' Api classes, so completion offers everything and a call is rejected
// only when no editor knows it.

import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const refIndex = args.indexOf('--ref');
const ref = refIndex >= 0 ? args.splice(refIndex, 2)[1] : null;
const [checkout, outputArg] = args;
const output = outputArg ?? path.join(here, 'onlyoffice-docbuilder.d.ts');

if (!checkout) {
    console.error('usage: node generate.mjs <api.onlyoffice.com checkout> [--ref <git ref>] [output .d.ts]');
    process.exit(1);
}

let siteRoot = checkout;
if (ref) {
    siteRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'docbuilder-site-'));
    const archive = execFileSync('git', ['-C', checkout, 'archive', '--format=tar', ref, 'site/docs/office-api/usage-api'],
        { maxBuffer: 1024 * 1024 * 1024 });
    execFileSync('tar', ['-x', '-C', siteRoot], { input: archive });
    process.on('exit', () => fs.rmSync(siteRoot, { recursive: true, force: true }));
}

const usageApi = path.join(siteRoot, 'site', 'docs', 'office-api', 'usage-api');
const siteUrl = 'https://api.onlyoffice.com/docs/office-api/usage-api/';

const editors = [
    { dir: 'document-api', ns: 'DocumentApi' },
    { dir: 'spreadsheet-api', ns: 'SpreadsheetApi' },
    { dir: 'presentation-api', ns: 'PresentationApi' },
    { dir: 'pdf-api', ns: 'PdfApi' },
    { dir: 'form-api', ns: 'FormApi' },
];

const reserved = new Set(('break case catch class const continue debugger default delete do else enum export extends false ' +
    'finally for function if import in instanceof new null return super switch this throw true try typeof var void ' +
    'while with as implements interface let package private protected public static yield any boolean number string ' +
    'symbol type from of').split(' '));

const builtinTypes = new Set(['any', 'unknown', 'void', 'undefined', 'null', 'never', 'boolean', 'number', 'string',
    'object', 'Date', 'Function', 'true', 'false']);

const typeWords = new Map([
    ['Number', 'number'], ['String', 'string'], ['Boolean', 'boolean'], ['bool', 'boolean'],
    ['Object', 'any'], ['object', 'any'], ['Array', 'any[]'], ['array', 'any[]'], ['function', 'Function'],
    ['None', 'void'], ['Any', 'any'], ['*', 'any'],
]);

// Signatures the reference pages get wrong, checked against the editors' behaviour, and public
// sdkjs methods the pages leave out (cell/apiBuilder.js and its siblings). Several entries for one
// method become overloads.
const overrides = {
    // The getter behind the documented TableStyles property; exported, but without a page.
    'SpreadsheetApi.ApiWorkbook.GetTableStyles': [
        'GetTableStyles(): ApiTableStyles;',
    ],
    // The page marks Range2 as required; GetRange("A1") is the everyday call.
    'SpreadsheetApi.ApiWorksheet.GetRange': [
        'GetRange(Range1: string | ApiRange, Range2?: string | ApiRange): ApiRange | null;',
    ],
    // One return type for two calls: all fields without an argument, a single field with one.
    'SpreadsheetApi.ApiPivotTable.GetPivotFields': [
        'GetPivotFields(): ApiPivotField[];',
        'GetPivotFields(field: string | number): ApiPivotField | ApiPivotDataField | null;',
    ],
};

const stats = { classes: 0, methods: 0, properties: 0, typedefs: 0, fallbacks: [] };

// ---------- Markdown helpers ----------

const read = file => fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n');

function sections(text) {
    const result = { title: '', intro: '', parts: new Map() };
    const title = text.match(/^# (.+)$/m);
    result.title = title ? title[1].trim() : '';
    const body = title ? text.slice(title.index + title[0].length) : text;
    const chunks = body.split(/^## /m);
    result.intro = chunks[0].trim();
    for (const chunk of chunks.slice(1)) {
        const nl = chunk.indexOf('\n');
        const name = (nl < 0 ? chunk : chunk.slice(0, nl)).trim();
        result.parts.set(name, nl < 0 ? '' : chunk.slice(nl + 1).trim());
    }
    return result;
}

function tableRows(text) {
    if (!text) return [];
    return text.split('\n')
        .filter(l => l.trim().startsWith('|'))
        .slice(2)
        .map(l => l.trim().replace(/^\||\|$/g, '').split(/(?<!\\)\|/).map(c => c.trim()));
}

// Relative .md links become absolute site URLs, admonitions become plain bold lines, and nothing may
// close the JSDoc comment early.
function docText(text, file) {
    const dir = path.dirname(file);
    return text
        .replace(/\[([^\]]+)\]\(([^)]+)\)/g, (m, label, href) => {
            if (/^[a-z]+:/i.test(href) || href.startsWith('#')) return m;
            const [target, hash] = href.split('#');
            const rel = path.relative(usageApi, path.resolve(dir, target)).replace(/\\/g, '/').replace(/\.md$/, '/');
            return rel.startsWith('..') ? label : `[${label}](${siteUrl}${rel}${hash ? '#' + hash : ''})`;
        })
        .replace(/^:::(\w+)\[([^\]]+)\]\s*$/gm, '**$2:**')
        .replace(/^:::(\w+)\s*$/gm, (m, kind) => `**${kind[0].toUpperCase()}${kind.slice(1)}:**`)
        .replace(/^:::\s*$/gm, '')
        .replace(/\\\|/g, '|')
        .replace(/\*\//g, '*\\/')
        .replace(/\n{3,}/g, '\n\n')
        .trim();
}

function jsdoc(lines, indent) {
    const text = lines.filter(l => l !== undefined && l !== null).join('\n').trim();
    if (!text) return '';
    return `${indent}/**\n${text.split('\n').map(l => `${indent} *${l ? ' ' + l : ''}`).join('\n')}\n${indent} */\n`;
}

const pageUrl = file => siteUrl + path.relative(usageApi, file).replace(/\\/g, '/').replace(/\.md$/, '/');

// ---------- Types ----------

// The site writes types almost as TypeScript already: links around type names, escaped pipes,
// JSDoc spellings (Number, Array.<T>) and the odd prose sentence for "returns nothing".
function convertType(raw, referenced, context) {
    let t = raw.trim();
    if (!t || /doesn't return|does not return|^none$/i.test(t)) return 'void';

    // String literals (number formats, error values) may hold any character, Markdown escapes and
    // link-like brackets included: set them aside before anything else touches the text.
    const literals = [];
    t = t.replace(/"((?:[^"\\]|\\.)*)"|'((?:[^'\\]|\\.)*)'/g, (m, dq, sq) => {
        literals.push(JSON.stringify((dq ?? sq).replace(/\\([[\]\\*_|])/g, '$1')));
        return `\u0000${literals.length - 1}\u0000`;
    });
    t = t.replace(/\[([^\]]+)\]\([^)]*\)/g, '$1').replace(/\\\|/g, '|').replace(/\*\*/g, '').trim();
    t = t.replace(/Array\.?<([^<>]+)>/g, '($1)[]').replace(/\bArray\.(\w+)/g, '$1[]');
    if (!/^[\w\s|()[\]\u0000,<>*-]+$/.test(t)) {
        stats.fallbacks.push(`${context}: ${raw}`);
        return 'any';
    }
    t = t.replace(/[A-Za-z_$*][\w$]*/g, word => {
        if (typeWords.has(word)) return typeWords.get(word);
        if (!builtinTypes.has(word)) referenced.add(word);
        return word;
    });
    return t.replace(/\u0000(\d+)\u0000/g, (m, i) => literals[i]).replace(/\s*\|\s*/g, ' | ');
}

const safeName = name => (reserved.has(name) ? `${name}_` : name).replace(/[^\w$]/g, '_');

const isIdentifier = name => /^[A-Za-z_$][\w$]*$/.test(name);

// ---------- Readers ----------

function readMethod(file, referenced, owner) {
    const s = sections(read(file));
    const name = s.title;

    // An inherited method's page carries only its own description and a link to the base class page,
    // which holds the signature.
    const inherited = s.intro.match(/Inherited from \[[^\]]+\]\(([^)#]+\.md)\)/);
    if (inherited && !s.parts.has('Syntax')) {
        const base = path.resolve(path.dirname(file), inherited[1]);
        if (base !== file && fs.existsSync(base)) {
            const m = readMethod(base, referenced, owner);
            m.doc[0] = docText(s.intro, file);
            m.doc[m.doc.length - 1] = `@see ${pageUrl(file)}`;
            return m;
        }
    }

    const rows = tableRows(s.parts.get('Parameters'));
    const syntax = (s.parts.get('Syntax') || '').match(/expression\.\w+\(([^)]*)\)/);
    const order = syntax ? syntax[1].split(',').map(p => p.trim()).filter(Boolean) : rows.map(r => r[0]);
    const byName = new Map(rows.map(r => [r[0], r]));

    let optionalSeen = false;
    const params = [];
    const used = new Set();
    for (const pName of order) {
        const row = byName.get(pName) ?? [pName, 'Optional', 'any', '', ''];
        const optional = /optional/i.test(row[1]) || optionalSeen;
        optionalSeen ||= optional;
        // A few pages repeat a parameter name in the syntax line.
        let name = safeName(pName);
        for (let i = 2; used.has(name); i++) name = `${safeName(pName)}${i}`;
        used.add(name);
        params.push({
            name,
            optional,
            type: convertType(row[2], referenced, `${owner}.${name}(${pName})`),
            def: row[3],
            description: row[4],
        });
    }

    const returnsText = s.parts.get('Returns') || '';
    const returnType = convertType(returnsText.split('\n')[0], referenced, `${owner}.${name} returns`);

    const doc = [docText(s.intro, file), ''];
    for (const p of params) {
        const label = p.optional ? `[${p.name}${p.def ? '=' + p.def.replace(/\\\|/g, '|') : ''}]` : p.name;
        doc.push(`@param ${label} ${docText(p.description, file)}`);
    }
    if (returnType !== 'void') doc.push(`@returns ${docText(returnsText.split('\n')[0], file)}`);
    doc.push(`@see ${pageUrl(file)}`);

    const signature = params.map(p => `${p.name}${p.optional ? '?' : ''}: ${p.type}`).join(', ');
    return { name, doc, signature, returnType };
}

function readClass(dir, name, ns, referenced) {
    const file = path.join(dir, `${name}.md`);
    if (!fs.existsSync(file)) return null;
    const s = sections(read(file));
    const members = [];
    const names = new Set();

    const methodsDir = path.join(dir, 'Methods');
    if (fs.existsSync(methodsDir)) {
        for (const f of fs.readdirSync(methodsDir).filter(f => f.endsWith('.md')).sort()) {
            const m = readMethod(path.join(methodsDir, f), referenced, name);
            if (!isIdentifier(m.name) || names.has(m.name)) continue;
            names.add(m.name);
            const signatures = overrides[`${ns}.${name}.${m.name}`] ?? [`${m.name}(${m.signature}): ${m.returnType};`];
            members.push(signatures.map(sig => jsdoc(m.doc, '        ') + `        ${sig}\n`).join(''));
            stats.methods++;
        }
    }

    const prefix = `${ns}.${name}.`;
    for (const [key, signatures] of Object.entries(overrides)) {
        const method = key.slice(prefix.length);
        if (!key.startsWith(prefix) || names.has(method)) continue;
        names.add(method);
        members.push(signatures.map(sig => jsdoc(['Not on the reference pages: a public sdkjs method.'], '        ')
            + `        ${sig}\n`).join(''));
        stats.methods++;
    }

    for (const [pName, pType, pDesc] of tableRows(s.parts.get('Properties'))) {
        if (!isIdentifier(pName) || names.has(pName)) continue;
        names.add(pName);
        const type = convertType(pType, referenced, `${name}.${pName}`);
        members.push(jsdoc([docText(pDesc, file)], '        ') + `        ${pName}: ${type};\n`);
        stats.properties++;
    }

    stats.classes++;
    return jsdoc([docText(s.intro, file), '', `@see ${pageUrl(file)}`], '    ')
        + `    interface ${name} {\n${members.join('')}    }\n`;
}

function readTypedef(file, referenced, taken) {
    const s = sections(read(file));
    const name = s.title;
    if (!isIdentifier(name) || taken.has(name)) return null;
    const kind = (s.parts.get('Type') || '').split('\n')[0].trim();
    const doc = jsdoc([docText(s.intro, file), '', `@see ${pageUrl(file)}`], '    ');
    stats.typedefs++;

    if (/^object$/i.test(kind) && s.parts.has('Properties')) {
        // Every property is optional: the same shape describes what a method returns and what a
        // caller builds, and the pages mark fields as "present only for ..." in prose only.
        const props = tableRows(s.parts.get('Properties'))
            .filter(([p]) => isIdentifier(p))
            .map(([p, t, d]) => jsdoc([docText(d, file)], '        ')
                + `        ${p}?: ${convertType(t, referenced, `${name}.${p}`)};\n`);
        return doc + `    interface ${name} {\n${props.join('')}        [key: string]: any;\n    }\n`;
    }

    if (/^enumeration$/i.test(kind)) {
        const values = (s.parts.get('Values') || '').split('\n')
            .map(l => l.match(/^\s*-\s+(.+)$/)).filter(Boolean)
            .map(m => convertType(m[1].trim(), referenced, `${name} value`));
        return doc + `    type ${name} = ${values.length ? [...new Set(values)].join(' | ') : 'any'};\n`;
    }

    return doc + `    type ${name} = ${convertType(kind, referenced, name)};\n`;
}

const definedBy = new Map();

function readEditor({ dir, ns }) {
    const root = path.join(usageApi, dir);
    const referenced = new Set();
    const classNames = fs.readdirSync(root, { withFileTypes: true })
        .filter(e => e.isDirectory() && e.name !== 'Enumeration')
        .map(e => e.name)
        .sort();

    const blocks = [];
    const defined = new Set();
    for (const c of classNames) {
        const block = readClass(path.join(root, c), c, ns, referenced);
        if (block) {
            blocks.push(block);
            defined.add(c);
        }
    }

    const enumDir = path.join(root, 'Enumeration');
    if (fs.existsSync(enumDir)) {
        for (const f of fs.readdirSync(enumDir).filter(f => f.endsWith('.md')).sort()) {
            const block = readTypedef(path.join(enumDir, f), referenced, defined);
            if (block) {
                blocks.push(block);
                defined.add(path.basename(f, '.md'));
            }
        }
    }

    definedBy.set(ns, defined);
    return { ns, blocks, defined, referenced };
}

// Types an editor mentions but does not describe come from an editor that does (the form editor
// leans on the document editor's classes); the rest are left untyped.
function renderEditor({ ns, blocks, defined, referenced }) {
    const missing = [...referenced].filter(r => !defined.has(r)).sort();
    if (missing.length) {
        blocks.push(`    // Referenced by the reference pages but not described there.\n`
            + missing.map(m => {
                const owner = editors.find(e => e.ns !== ns && definedBy.get(e.ns)?.has(m));
                return `    type ${m} = ${owner ? `${owner.ns}.${m}` : 'any'};\n`;
            }).join(''));
    }

    return `declare namespace ${ns} {\n${blocks.join('\n')}}\n`;
}

// ---------- Output ----------

let revision = 'unknown revision';
try {
    revision = execFileSync('git', ['-C', checkout, 'log', '-1', '--format=%h %cs', ref ?? 'HEAD'], { encoding: 'utf8' }).trim()
        + (ref ? `, ${ref}` : ', working tree');
} catch { /* not a git checkout */ }

const header = `// ONLYOFFICE Document Builder declarations for *.docbuilder scripts.
// Generated by common/Tools/DocBuilder/typings/generate.mjs from api.onlyoffice.com (${revision}).
// Do not edit by hand: regenerate instead.

/**
 * The Document Builder instance a .docbuilder script runs in.
 * @see https://api.onlyoffice.com/docs/document-builder/builder-framework/CDocBuilder/
 */
interface DocBuilder {
    /**
     * Creates a new file. The type of the file which will be created needs to be set.
     * @param type The file extension (\`docx\`, \`xlsx\`, \`pptx\`, \`pdf\`...) or an OFFICESTUDIO_FILE_XXX value.
     * @see https://api.onlyoffice.com/docs/document-builder/builder-framework/CDocBuilder/CreateFile/
     */
    CreateFile(type: "docx" | "xlsx" | "pptx" | "pdf" | "docxf" | "oform" | string | number): boolean;
    /**
     * Opens the document file which will be edited and saved afterwards.
     * @param path The path or URL of the file to be opened.
     * @param params The parameters needed for the correct file opening (encoding and delimiter for txt/csv).
     * @see https://api.onlyoffice.com/docs/document-builder/builder-framework/CDocBuilder/OpenFile/
     */
    OpenFile(path: string, params?: string): boolean;
    /**
     * Saves the file after all the changes are made.
     * @param type The file type (\`docx\`, \`odt\`, \`rtf\`, \`txt\`, \`pptx\`, \`xlsx\`, \`ods\`, \`csv\`, \`pdf\`...).
     * @param path The path to the file to be saved together with its name and extension.
     * @param params The parameters needed for the correct file saving.
     * @see https://api.onlyoffice.com/docs/document-builder/builder-framework/CDocBuilder/SaveFile/
     */
    SaveFile(type: string | number, path: string, params?: string): boolean;
    /**
     * Closes the file to stop working with it. All script variables are cleared; use GlobalVariable to keep data.
     * @see https://api.onlyoffice.com/docs/document-builder/builder-framework/CDocBuilder/CloseFile/
     */
    CloseFile(): void;
    /**
     * Sets the folder where the program temporarily saves the files it needs.
     * @see https://api.onlyoffice.com/docs/document-builder/builder-framework/CDocBuilder/SetTmpFolder/
     */
    SetTmpFolder(folder: string): void;
}

declare const builder: DocBuilder;
declare const builderJS: DocBuilder;

/**
 * Survives CloseFile: the only way to pass data from one opened file to the next.
 * @see https://api.onlyoffice.com/docs/document-builder/using-cli/sharing-data-between-files/
 */
declare var GlobalVariable: { [key: string]: any };

/**
 * The editor API of the opened file. The editor depends on the file a script creates or opens, so this
 * is every editor's Api at once.
 */
declare const Api: ${editors.map(e => `${e.ns}.Api`).join(' & ')};

`;

const body = editors.map(readEditor).map(renderEditor).join('\n');
fs.writeFileSync(output, header + body);

console.log(`${output}: ${stats.classes} classes, ${stats.methods} methods, ${stats.properties} properties, ${stats.typedefs} typedefs, ${(fs.statSync(output).size / 1024 / 1024).toFixed(1)} MB`);
if (stats.fallbacks.length) {
    console.log(`${stats.fallbacks.length} type expressions fell back to any:`);
    for (const f of stats.fallbacks.slice(0, 50)) console.log('  ' + f);
}
