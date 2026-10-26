// A TypeScript language service over the *.docbuilder templates of one folder.
//
// A template is JavaScript with ${name} placeholders the C# builders replace before the script runs.
// Outside a string such a placeholder is a syntax error, so every one is masked as an identifier of
// the same length - ${inputData} becomes $_inputData_ - declared as `any`. Offsets do not move, which
// lets every answer of the language service go back to the editor unchanged.
//
// Each template is checked as a module of its own, except a shared script: a template whose name is
// itself a placeholder (ReportTableStyle.docbuilder for ${reportTableStyle}) is pasted into others,
// so it is checked as a global script and sees the top-level variables of the templates that paste it.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
// A static import, so the bundle for the IDE plugins carries TypeScript inside it.
import ts from 'typescript';

const here = path.dirname(fileURLToPath(import.meta.url));

export const extension = '.docbuilder';

// Errors that say nothing about the Document Builder API. JavaScript infers an array's element type
// from its first element, so a table whose header row sets more cell properties than its data rows
// reports every data row as "missing properties". Such a mismatch always targets an anonymous object
// type the script itself produced; one against an API type is named and stays.
const shapeCodes = new Set([2322, 2345, 2739, 2741]);
const isOwnShapeMismatch = (code, message) => shapeCodes.has(code) && /\bto type '\{|\bfrom type '\{|\bin type '\{/.test(message);

const placeholderPattern = /\$\{(\w+)\}/g;

export function maskPlaceholders(text) {
    return text.replace(placeholderPattern, (_, name) => `$_${name}_`);
}

export function placeholdersOf(text) {
    return [...text.matchAll(placeholderPattern)].map(m => m[1]);
}

export function findDeclarations() {
    const candidates = [
        process.env.DOCBUILDER_DECLARATIONS,
        path.join(here, 'onlyoffice-docbuilder.d.ts'),
        path.join(here, '..', 'onlyoffice-docbuilder.d.ts'),
        path.join(here, '..', '..', 'typings', 'onlyoffice-docbuilder.d.ts'),
    ];
    const found = candidates.find(c => c && fs.existsSync(c));
    if (!found) throw new Error('onlyoffice-docbuilder.d.ts not found; set DOCBUILDER_DECLARATIONS');
    return found;
}

export class TemplateProject {
    constructor(folder, declarations = findDeclarations()) {
        this.folder = path.resolve(folder);
        this.declarations = path.resolve(declarations);
        this.overlays = new Map(); // template path -> text an editor holds that is not saved yet
        this.versions = new Map();
        this.version = 0;
        this.disk = new Map(); // template path -> { mtime, text }

        this.options = {
            allowJs: true,
            checkJs: true,
            noEmit: true,
            strict: false,
            target: ts.ScriptTarget.ES2020,
            lib: ['lib.es2020.d.ts'],
            types: [],
            skipLibCheck: true,
        };

        const host = {
            getCompilationSettings: () => this.options,
            getScriptFileNames: () => this.scriptFileNames(),
            getScriptVersion: file => this.scriptVersion(file),
            getScriptSnapshot: file => {
                const text = this.scriptText(file);
                return text === undefined ? undefined : ts.ScriptSnapshot.fromString(text);
            },
            getScriptKind: file => (file.endsWith('.d.ts') ? ts.ScriptKind.TS : ts.ScriptKind.JS),
            getCurrentDirectory: () => this.folder,
            getDefaultLibFileName: options => ts.getDefaultLibFilePath(options),
            fileExists: file => this.scriptText(file) !== undefined,
            readFile: file => this.scriptText(file),
            directoryExists: dir => ts.sys.directoryExists(dir),
            getDirectories: dir => ts.sys.getDirectories(dir),
            useCaseSensitiveFileNames: () => false,
        };
        this.service = ts.createLanguageService(host, ts.createDocumentRegistry(false));
    }

    // ---------- files ----------

    templates() {
        const names = fs.existsSync(this.folder)
            ? fs.readdirSync(this.folder).filter(f => f.toLowerCase().endsWith(extension)).map(f => path.join(this.folder, f))
            : [];
        for (const file of this.overlays.keys()) if (!names.some(n => same(n, file))) names.push(file);
        return names.sort();
    }

    // The language service sees a template as <template>.js and the host declarations as one more file.
    scriptName(template) {
        return path.resolve(template) + '.js';
    }

    templateOf(scriptName) {
        const resolved = path.resolve(scriptName);
        return resolved.toLowerCase().endsWith(extension + '.js') ? resolved.slice(0, -3) : null;
    }

    hostName() {
        return path.join(this.folder, '__docbuilder_host__.d.ts');
    }

    scriptFileNames() {
        return [this.declarations, this.hostName(), ...this.templates().map(t => this.scriptName(t))];
    }

    sourceText(template) {
        for (const [file, text] of this.overlays) if (same(file, template)) return text;
        try {
            const stat = fs.statSync(template);
            const cached = this.disk.get(template);
            if (cached && cached.mtime === stat.mtimeMs) return cached.text;
            const text = fs.readFileSync(template, 'utf8');
            this.disk.set(template, { mtime: stat.mtimeMs, text });
            return text;
        } catch {
            return undefined;
        }
    }

    isShared(template) {
        const name = path.basename(template, path.extname(template)).toLowerCase();
        return this.sharedNames().has(name);
    }

    sharedNames() {
        const names = new Set();
        for (const t of this.templates()) for (const p of placeholdersOf(this.sourceText(t) ?? '')) names.add(p.toLowerCase());
        return names;
    }

    scriptText(file) {
        if (same(file, this.hostName())) return this.hostText();
        const template = this.templateOf(file);
        if (template) {
            const text = this.sourceText(template);
            if (text === undefined) return undefined;
            return this.isShared(template) ? maskPlaceholders(text) : `${maskPlaceholders(text)}\nexport {};\n`;
        }
        return ts.sys.readFile(file);
    }

    scriptVersion(file) {
        if (same(file, this.hostName())) return String(this.version);
        const template = this.templateOf(file);
        if (template) {
            for (const [f, v] of this.versions) if (same(f, template)) return `o${v}`;
            try {
                return `d${fs.statSync(template).mtimeMs}`;
            } catch {
                return '0';
            }
        }
        return '0';
    }

    // Placeholders, what shared scripts take from the templates that paste them, and parseInt over a
    // number - the templates' way to truncate, which the TypeScript lib types for strings only.
    hostText() {
        const templates = this.templates();
        const placeholders = new Set();
        const shared = [];
        const hostVariables = new Set();
        for (const t of templates) {
            const text = this.sourceText(t) ?? '';
            for (const p of placeholdersOf(text)) placeholders.add(p);
            if (this.isShared(t)) shared.push(text);
            else for (const m of text.matchAll(/^(?:var|let|const)\s+([A-Za-z_$][\w$]*)/gm)) hostVariables.add(m[1]);
        }
        const used = [...hostVariables].filter(v => shared.some(s => new RegExp(`\\b${v.replace(/\$/g, '\\$')}\\b`).test(s)));
        return [
            '// Generated for the language service: placeholders and what shared scripts get from their hosts.',
            ...[...placeholders].sort().map(p => `declare var $_${p}_: any;`),
            ...used.sort().map(v => `declare var ${v}: any;`),
            'declare function parseInt(value: number, radix?: number): number;',
            '',
        ].join('\n');
    }

    // ---------- editor state ----------

    open(template, text) {
        this.overlays.set(path.resolve(template), text);
        this.versions.set(path.resolve(template), ++this.version);
    }

    change(template, text) {
        this.open(template, text);
    }

    close(template) {
        for (const f of [...this.overlays.keys()]) if (same(f, template)) this.overlays.delete(f);
        for (const f of [...this.versions.keys()]) if (same(f, template)) this.versions.delete(f);
        this.version++;
    }

    touch() {
        this.version++;
    }

    // ---------- queries ----------

    diagnostics(template) {
        const name = this.scriptName(template);
        const text = this.sourceText(template) ?? '';
        const length = text.length;
        const syntactic = this.service.getSyntacticDiagnostics(name);
        const builder = syntactic.length ? [] : this.builderRules(name, maskPlaceholders(text));
        return [...syntactic, ...this.service.getSemanticDiagnostics(name)]
            .filter(d => d.start !== undefined && d.start < length)
            .map(d => ({
                start: d.start,
                length: d.length ?? 0,
                code: d.code,
                category: d.category,
                message: ts.flattenDiagnosticMessageText(d.messageText, '\n'),
            }))
            .filter(d => !isOwnShapeMismatch(d.code, d.message))
            .concat(builder)
            .map(d => (d.code === 2339 && this.existsOnSomeMember(name, d.start) ? {
                ...d,
                category: ts.DiagnosticCategory.Warning,
                message: `${d.message}\nIt exists on some of the types the value can have; make sure this one is among them.`,
            } : d));
    }

    // Document Builder reads a line that starts with `builder.` as a command of its own and runs the
    // lines between commands as JavaScript, where no `builder` exists. A command's arguments are
    // parsed by the builder, not evaluated, so a variable there is passed as its name.
    builderRules(scriptName, text) {
        const source = this.service.getProgram()?.getSourceFile(scriptName);
        if (!source) return [];
        const found = [];
        const lineStart = pos => text.lastIndexOf('\n', pos - 1) + 1;
        const visit = node => {
            if (ts.isPropertyAccessExpression(node) && ts.isIdentifier(node.expression)
                && (node.expression.text === 'builder' || node.expression.text === 'builderJS')) {
                const start = node.getStart(source);
                const statement = node.parent && ts.isCallExpression(node.parent) && node.parent.parent;
                const command = statement && ts.isExpressionStatement(statement) && statement.getStart(source) === start
                    && /^\s*$/.test(text.slice(lineStart(start), start));
                if (!command) {
                    found.push({
                        start, length: node.getEnd() - start, code: 90001, category: ts.DiagnosticCategory.Error,
                        message: '`builder` exists only in a command line of its own: a line starting with `builder.`. Inside JavaScript code it is not defined.',
                    });
                } else {
                    for (const arg of node.parent.arguments) {
                        const literal = ts.isStringLiteralLike(arg) || ts.isNumericLiteral(arg)
                            || (ts.isIdentifier(arg) && /^\$_\w+_$/.test(arg.text));
                        if (!literal) {
                            found.push({
                                start: arg.getStart(source), length: arg.getEnd() - arg.getStart(source), code: 90002,
                                category: ts.DiagnosticCategory.Error,
                                message: 'A builder command takes literal arguments only: the builder parses this line, it does not evaluate it. Pass a value through GlobalVariable or write it as "jsValue(name)".',
                            });
                        }
                    }
                }
            }
            ts.forEachChild(node, visit);
        };
        visit(source);
        return found;
    }

    // GetElement(0) may return a paragraph or a table: a member only some of them have is a question
    // about the document at hand, not a mistake in the script.
    existsOnSomeMember(scriptName, start) {
        const program = this.service.getProgram();
        const source = program?.getSourceFile(scriptName);
        if (!source) return false;
        let node = source;
        for (;;) {
            const child = node.getChildren(source).find(c => c.getStart(source) <= start && start < c.getEnd());
            if (!child) break;
            node = child;
        }
        const access = node.parent;
        if (!access || !ts.isPropertyAccessExpression(access) || access.name !== node) return false;
        const checker = program.getTypeChecker();
        const type = checker.getTypeAtLocation(access.expression);
        return type.isUnion() && type.types.some(t => checker.getPropertyOfType(t, node.text));
    }

    completions(template, offset) {
        return this.service.getCompletionsAtPosition(this.scriptName(template), offset, {
            includeCompletionsWithInsertText: true,
            includeCompletionsForModuleExports: false,
        });
    }

    completionDetails(template, offset, name) {
        return this.service.getCompletionEntryDetails(this.scriptName(template), offset, name, undefined, undefined, undefined, undefined);
    }

    quickInfo(template, offset) {
        return this.service.getQuickInfoAtPosition(this.scriptName(template), offset);
    }

    signatureHelp(template, offset) {
        return this.service.getSignatureHelpItems(this.scriptName(template), offset, undefined);
    }

    definitions(template, offset) {
        return this.locations(this.service.getDefinitionAtPosition(this.scriptName(template), offset));
    }

    references(template, offset) {
        return this.locations(this.service.getReferencesAtPosition(this.scriptName(template), offset));
    }

    outline(template) {
        return this.service.getNavigationTree(this.scriptName(template));
    }

    // Spans in another template or in the declarations, with that file's text to turn offsets into
    // lines; the host file is internal and never shown.
    locations(entries) {
        return (entries ?? [])
            .filter(e => !same(e.fileName, this.hostName()))
            .map(e => {
                const template = this.templateOf(e.fileName);
                const file = template ?? path.resolve(e.fileName);
                return { file, start: e.textSpan.start, length: e.textSpan.length, text: template ? this.sourceText(template) : ts.sys.readFile(file) };
            });
    }
}

export function displayText(parts) {
    return ts.displayPartsToString(parts ?? []);
}

export { ts };

function same(a, b) {
    return path.resolve(a).toLowerCase() === path.resolve(b).toLowerCase();
}
