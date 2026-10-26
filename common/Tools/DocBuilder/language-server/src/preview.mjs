// Runs a template through a local Document Builder and returns the document it saves plus a PDF of it.
//
// The template's placeholders are filled from a sample file the way the C# builders fill them, shared
// scripts are pasted in, and every line of the script that runs remembers which template line it came
// from - so an error the builder reports can be shown where it was written.

import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { extension } from './project.mjs';
import { readSample, SampleError } from './samples.mjs';
import { GenerateError } from './generate.mjs';
import { ViewerError, previewRoot, editCopy } from './viewer.mjs';
import { diffWorkbooks, formatDiff, XlsxError } from './xlsx.mjs';

const builderCandidates = [
    'C:/Program Files/ONLYOFFICE/DocumentBuilder/docbuilder.exe',
    'C:/Program Files/ONLYOFFICE/DocumentServer/server/FileConverter/bin/docbuilder.exe',
    '/opt/onlyoffice/documentbuilder/docbuilder',
    '/var/www/onlyoffice/documentserver/server/FileConverter/bin/docbuilder',
    '/Applications/ONLYOFFICE Document Builder.app/Contents/MacOS/docbuilder',
];

export function findBuilder(configured) {
    const candidates = [configured, process.env.DOCBUILDER_PATH, ...builderCandidates];
    return candidates.find(c => c && fs.existsSync(c)) ?? null;
}

// <Name>.sample.json next to the template, where it changes together with the template and the code
// that fills it; or in the samples folder the editor configured.
export function findSample(template, samplesFolder) {
    const name = path.basename(template, path.extname(template));
    const candidates = [
        path.join(path.dirname(template), `${name}.sample.json`),
        samplesFolder && path.join(samplesFolder, `${name}.sample.json`),
        process.env.DOCBUILDER_SAMPLES && path.join(process.env.DOCBUILDER_SAMPLES, `${name}.sample.json`),
    ];
    return candidates.find(c => c && fs.existsSync(c)) ?? null;
}

const isCommand = line => /^\s*builder\./.test(line);
const forward = p => p.replace(/\\/g, '/');

// Builds the script the builder runs: lines plus, for each, the template file and line it came from.
export function buildScript({ template, readText, sample, outputBase, tmpFolder }) {
    const lines = [];
    const origin = [];
    const outputs = [];
    const missing = [];
    const folder = path.dirname(template);

    const shared = new Map();
    for (const f of fs.readdirSync(folder).filter(f => f.toLowerCase().endsWith(extension))) {
        shared.set(path.basename(f, path.extname(f)).toLowerCase(), path.join(folder, f));
    }

    const paste = (file, depth) => {
        const text = readText(file) ?? '';
        text.replace(/\r\n/g, '\n').split('\n').forEach((line, index) => {
            const alone = line.match(/^\s*\$\{(\w+)\}\s*;?\s*$/);
            const pasted = alone && shared.get(alone[1].toLowerCase());
            if (pasted && depth < 5) {
                paste(pasted, depth + 1);
                return;
            }
            lines.push(fill(line, file));
            origin.push({ file, line: index });
        });
    };

    const fill = (line, file) => line.replace(/(\[)?\$\{(\w+)\}(\])?/g, (match, open, name, close, offset) => {
        if (name === 'tempFileName') {
            const type = line.match(/SaveFile\(\s*["'](\w+)["']/)?.[1] ?? 'docx';
            const out = `${outputBase}.${type}`;
            outputs.push({ type, path: out });
            return `${open ?? ''}${forward(out)}${close ?? ''}`;
        }
        const values = sample?.placeholders ?? {};
        if (!(name in values)) {
            missing.push({ name, file });
            return match;
        }
        const value = values[name];
        if (open && close && Array.isArray(value)) return `[${value.map(v => JSON.stringify(v)).join(',')}]`;
        const quoted = /["']$/.test(line.slice(0, offset + (open ? 1 : 0)));
        const text = quoted && typeof value === 'string' ? value : JSON.stringify(typeof value === 'string' ? forward(value) : value);
        return `${open ?? ''}${text}${close ?? ''}`;
    });

    // Builders running at the same time share one temporary folder unless told otherwise, and then fail
    // to open or save files ("save file error (80)") - the editor and the assistant may run at once.
    if (tmpFolder) {
        lines.push(`builder.SetTmpFolder("${forward(tmpFolder)}");`);
        origin.push(null);
    }
    paste(template, 0);

    // The document the template saves, opened once more and saved as PDF for the preview; the saved
    // document itself is not touched. A sheet goes onto the largest landscape paper there is (E sheet,
    // 44 x 34 in) at its own size, so the preview shows it as a spreadsheet does at 100 % and a wide
    // report stays readable; only a sheet wider still is scaled down to that page's width, rather than
    // its right-hand columns landing on pages of their own.
    const document = outputs.find(o => ['xlsx', 'docx', 'pptx', 'pdf'].includes(o.type));
    if (document && document.type !== 'pdf') {
        const pdf = `${outputBase}.preview.pdf`;
        const fit = document.type === 'xlsx'
            ? ['Api.GetSheets().forEach(function (sheet) { var setup = sheet.GetPageSetup(); setup.SetPaperSize("xlPaperEsheet"); setup.SetOrientation("xlLandscape"); setup.SetFitToPagesWide(1); setup.SetFitToPagesTall(0); });']
            : [];
        // The active sheet as CSV too: an agent cannot look at a PDF, but it can read what the cells hold.
        const csv = document.type === 'xlsx' ? `${outputBase}.preview.csv` : null;
        const saves = [`builder.SaveFile("pdf", "${forward(pdf)}");`, ...(csv ? [`builder.SaveFile("csv", "${forward(csv)}");`] : [])];
        for (const line of [`builder.OpenFile("${forward(document.path)}");`, ...fit, ...saves, 'builder.CloseFile();']) {
            lines.push(line);
            origin.push(null);
        }
        outputs.push({ type: 'pdf', path: pdf, preview: true });
        if (csv) outputs.push({ type: 'csv', path: csv, preview: true });
    }

    return { text: lines.join('\n') + '\n', origin, outputs, missing };
}

// The builder runs the lines between two commands as one piece of JavaScript, dropping the blank lines,
// and numbers an error's line inside what is left of that piece. The piece is not named, so the line
// goes to the piece whose code mentions what the message talks about.
export function locateError(stderr, script) {
    const message = stderr.split(/\r?\n/).find(l => l.trim())?.trim() ?? '';
    const at = stderr.match(/<anonymous>:(\d+):(\d+)/);
    if (!at) return { message };
    const [line, column] = [Number(at[1]), Number(at[2])];
    const lines = script.text.split('\n');

    const pieces = [[]];
    lines.forEach((text, index) => {
        if (isCommand(text)) pieces.push([]);
        else if (text.trim()) pieces[pieces.length - 1].push(index);
    });
    const candidates = pieces.map(piece => piece[line - 1]).filter(index => index !== undefined);
    const words = (message.match(/[A-Za-z_$][\w$]{2,}/g) ?? []).filter(w => !/^(TypeError|ReferenceError|SyntaxError|Error|Cannot|read|property|not|function|defined|undefined|null|of)$/.test(w));
    const best = candidates.find(i => words.some(w => lines[i].includes(w))) ?? candidates[0];
    const source = best === undefined ? null : script.origin[best];
    return source ? { message, file: source.file, line: source.line, column: column - 1 } : { message };
}

// The scenarios a template's sample offers, for the editor's scenario list.
export function listScenarios({ template, samplesFolder }) {
    const samplePath = findSample(template, samplesFolder);
    if (!samplePath) return { sample: null, default: null, scenarios: [] };
    try {
        const sample = readSample(samplePath);
        return { sample: samplePath, description: sample.description, default: sample.default, scenarios: sample.scenarios };
    } catch (e) {
        return { sample: samplePath, default: null, scenarios: [], error: { message: e.message } };
    }
}

// origin names who asked for the run ("editor", "mcp"); it goes into the run.json every run leaves in its
// folder, which is how the IDE learns about a run an agent made.
export async function runTemplate({ template, readText, samplesFolder, builderPath, scenario, origin = 'editor', timeoutMs = 120000 }) {
    const builder = findBuilder(builderPath);
    if (!builder) {
        return { ok: false, error: { message: 'Document Builder not found: install ONLYOFFICE Document Builder or set its path (DOCBUILDER_PATH).' } };
    }

    const samplePath = findSample(template, samplesFolder);
    let sample = null;
    if (samplePath) {
        try {
            sample = readSample(samplePath).resolve(scenario || undefined);
        } catch (e) {
            if (!(e instanceof SampleError || e instanceof GenerateError)) throw e;
            return { ok: false, sample: samplePath, scenario, error: { message: e.message } };
        }
    }

    // A run of its own folder: the previous PDF may still be open in the preview window, which keeps
    // it locked on Windows. Older runs go when nothing holds them any more - finished ones only (they
    // have their run.json), since the editor and the assistant may be running the same template now.
    const name = path.basename(template, path.extname(template));
    const runs = path.join(previewRoot(), name);
    fs.mkdirSync(runs, { recursive: true });
    for (const old of fs.readdirSync(runs)) {
        const folder = path.join(runs, old);
        try {
            const stale = Date.now() - fs.statSync(folder).mtimeMs > 10 * 60 * 1000;
            if (fs.existsSync(path.join(folder, 'run.json')) || stale) fs.rmSync(folder, { recursive: true, force: true });
        } catch { /* still open */ }
    }
    const work = fs.mkdtempSync(path.join(runs, `${Date.now()}-`));

    const finish = result => {
        const record = { ...result, template: path.resolve(template), origin, finishedAt: new Date().toISOString() };
        delete record.stdout;
        fs.writeFileSync(path.join(work, 'run.json'), JSON.stringify(record, null, 2));
        return { ...result, run: path.join(work, 'run.json') };
    };

    const tmpFolder = path.join(work, 'tmp');
    fs.mkdirSync(tmpFolder);
    const script = buildScript({ template, readText, sample, outputBase: path.join(work, name), tmpFolder });
    if (script.missing.length) {
        const names = [...new Set(script.missing.map(m => '${' + m.name + '}'))].join(', ');
        return finish({
            ok: false,
            sample: samplePath,
            scenario: sample?.scenario ?? null,
            error: {
                message: samplePath
                    ? `No sample value for ${names} in scenario "${sample.scenario}": add it under "placeholders" in ${samplePath}.`
                    : `No sample data for ${names}: create ${name}.sample.json next to the template or in the samples folder.`,
            },
        });
    }

    const scriptPath = path.join(work, 'script.docbuilder');
    fs.writeFileSync(scriptPath, script.text);

    const started = Date.now();
    const { code, stdout, stderr } = await run(builder, [scriptPath], timeoutMs);
    const outputs = script.outputs.filter(o => fs.existsSync(o.path));
    const result = {
        ok: code === 0 && !stderr.trim(),
        durationMs: Date.now() - started,
        builder,
        sample: samplePath,
        scenario: sample?.scenario ?? null,
        script: scriptPath,
        document: outputs.find(o => !o.preview)?.path ?? null,
        pdf: outputs.find(o => o.preview && o.type === 'pdf')?.path ?? null,
        csv: outputs.find(o => o.preview && o.type === 'csv')?.path ?? null,
        stdout,
        stderr,
    };
    if (!result.ok) result.error = code === null ? { message: `Document Builder did not finish in ${timeoutMs / 1000} s.` } : locateError(stderr || stdout, script);
    return finish(result);
}

function run(file, args, timeoutMs) {
    return new Promise(resolve => {
        const child = spawn(file, args, { windowsHide: true });
        let stdout = '';
        let stderr = '';
        child.stdout.on('data', d => (stdout += d));
        child.stderr.on('data', d => (stderr += d));
        const timer = setTimeout(() => child.kill(), timeoutMs);
        child.on('close', code => {
            clearTimeout(timer);
            resolve({ code: child.killed ? null : code, stdout, stderr });
        });
        child.on('error', e => {
            clearTimeout(timer);
            resolve({ code: -1, stdout, stderr: e.message });
        });
    });
}

// The hand edits of a document open in the editor: Document Server saves the editing session first,
// then the edited copy is compared with the document the run built.
// { edited, original, template, scenario, saved, diff: { changes, format, data, layout }, text }
export async function collectEdits(viewer, document) {
    const copy = editCopy(document, { create: false });
    if (!copy) throw new ViewerError('Open the document in the editor (preview mode "Editor") and change it there first.');
    if (path.extname(copy.edited).toLowerCase() !== '.xlsx') throw new ViewerError('Only spreadsheets can be compared so far.');
    const save = await viewer.save(copy.edited);
    let run = {};
    try {
        run = JSON.parse(fs.readFileSync(path.join(copy.folder, 'run.json'), 'utf8'));
    } catch { /* an older run without its record */ }
    let diff;
    try {
        diff = diffWorkbooks(copy.original, copy.edited);
    } catch (e) {
        if (!(e instanceof XlsxError)) throw e;
        throw new ViewerError(e.message);
    }
    return { ...copy, template: run.template ?? null, scenario: run.scenario ?? null, saved: save.saved, diff, text: formatDiff(diff) };
}

// workspace/executeCommand:
//   "docbuilder.scenarios" with [{ uri }]            - the scenarios of the template's sample;
//   "docbuilder.run"       with [{ uri, scenario? }]  - runs it; the open document's unsaved text is what runs;
//   "docbuilder.view"      with [{ document, edit? }] - a page that shows a document the run built in a
//                                                       Document Server editor: { url, edited? } or { error };
//                                                       with edit, an editable copy (edited) is what it shows;
//   "docbuilder.edits"     with [{ document }]        - what the user changed in that copy by hand:
//                                                       { text, format, data, layout, changes, ... } or { error }.
// The preview commands, as a handler the server's single executeCommand dispatcher asks first; it
// answers undefined for a command that is not its own. readText gives a file's text as the editor
// holds it.
export function previewCommands({ readText, getSettings, viewer }) {
    return async (command, argument) => {
        const settings = getSettings();
        if (command === 'docbuilder.view' || command === 'docbuilder.edits') {
            try {
                if (command === 'docbuilder.view') return await viewer.open(argument?.document ?? '', settings, { edit: !!argument?.edit });
                const edits = await collectEdits(viewer, argument?.document ?? '');
                const { diff, ...rest } = edits;
                return { ...rest, format: diff.format, data: diff.data, layout: diff.layout, changes: diff.changes };
            } catch (e) {
                if (!(e instanceof ViewerError)) throw e;
                return { error: { message: e.message } };
            }
        }
        if (command !== 'docbuilder.scenarios' && command !== 'docbuilder.run') return undefined;
        const { uri, scenario } = argument ?? {};
        if (!uri) return null;
        const template = fileURLToPath(uri);
        if (command === 'docbuilder.scenarios') return listScenarios({ template, samplesFolder: settings.samplesFolder });
        return runTemplate({ template, readText, samplesFolder: settings.samplesFolder, builderPath: settings.builderPath, scenario });
    };
}
