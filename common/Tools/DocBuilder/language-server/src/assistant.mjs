// The template assistant: a model of the DocSpace portal (its "onlyoffice" AI profile, paid from the
// portal's subscription) with tools that run here - the API search, the checker, a real Document
// Builder run on the sample data.
//
// The model may only hand a template back through docbuilder_propose, and a proposal is checked and
// run before it counts: one that fails goes back to the model with the errors, never to the user.
// The portal is reached at /api/2.0/ai/openai/{profileId}/v1/chat/completions, the OpenAI-compatible
// entry of ASC.AI.Chat, with the user's API key as a bearer token.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { TemplateProject, ts } from './project.mjs';
import { runTemplate, findSample, listScenarios, collectEdits } from './preview.mjs';
import { ViewerError } from './viewer.mjs';
import { diffWorkbooks, formatDiff } from './xlsx.mjs';
import { readSample } from './samples.mjs';
import { searchApi, formatEntry, editors } from './api-index.mjs';

export class AssistantError extends Error {}

const maxRounds = 16;
const maxProposals = 3;

const systemPrompt = `You change ONLYOFFICE Document Builder scripts (*.docbuilder) that DocSpace uses to build reports.

How these scripts work:
- A line that starts with \`builder.\` is a Document Builder command, not JavaScript. Its arguments are parsed literally: never pass a variable there. Inside JavaScript code \`builder\` does not exist.
- The JavaScript runs in an old V8: keep to ES5 style (var, function); no arrow functions, classes, template literals, optional chaining.
- \${name} is a placeholder the C# code fills before the script runs (JSON data, row lists, a shared script). Keep every placeholder exactly where and how it is.
- \`Api\` is the editor API of the file the script creates (CreateFile("xlsx") means the spreadsheet API).

How to work:
1. Use docbuilder_search_api before you call a method you have not seen in the template: invented methods fail.
2. Use docbuilder_get_sample to see the data the placeholders carry.
3. Change only what the request needs; keep the rest of the template as it is, comments included.
4. Check your full new text with docbuilder_check, then run it with docbuilder_run, and read the CSV of what the cells hold.
5. Hand the full new template text back with docbuilder_propose. It is checked and run again; if it fails you get the errors and try again.
If the request needs no change to the template (a question), answer in text and do not propose.

When the request carries the user's hand edits of the built document (a list of differences between the
document the template built and the one the user edited), change the template so that it builds the
edited document:
- Find the code that writes each changed area. Do not count rows: which row a value lands in depends on
  the data. Each area comes with the text its first cell holds, e.g. B2 ("Audit Trail Report"); find
  that text in the sample data (docbuilder_get_sample) to learn which field it is, then the code that
  writes that field. A change over a whole column of data rows belongs to the loop that writes those
  rows, not to fixed cells; a change of the header belongs to the entry of the header that holds it.
- Keep the data where it comes from: a value that comes from a placeholder is sample data, not the
  template's - leave it, and say so. Carry a changed value only when the template writes that text itself.
- docbuilder_propose compares what your text builds with the edited document and rejects a proposal
  that leaves formatting differences, listing them. If some cannot be carried (the API cannot express
  them), pass "unresolved" with the reason.
Answer in the language of the user's request, briefly.`;

const tools = [
    {
        name: 'docbuilder_search_api',
        description: 'Search the Document Builder API (every class, method and property of the document, spreadsheet, presentation, pdf and form editors, plus the builder commands) by name or by words. Returns signatures and descriptions. Use an exact "ApiRange.SetFillColor" for one member.',
        parameters: {
            type: 'object',
            properties: {
                query: { type: 'string', description: 'A member name, "Class.Member", or a few words such as "merge cells".' },
                editor: { type: 'string', enum: Object.values(editors), description: 'Limit to one editor; a script that creates an xlsx file uses "spreadsheet".' },
            },
            required: ['query'],
        },
    },
    {
        name: 'docbuilder_get_sample',
        description: 'The sample data of a scenario: what each ${placeholder} of the template is filled with. Long lists are cut to their first items with the total count.',
        parameters: {
            type: 'object',
            properties: { scenario: { type: 'string', description: 'Scenario name; the selected one when omitted.' } },
        },
    },
    {
        name: 'docbuilder_check',
        description: 'Type-check a full template text against the Document Builder API and the builder-command rules. Returns the problems with line numbers, or that there are none.',
        parameters: {
            type: 'object',
            properties: { text: { type: 'string', description: 'The full template text.' } },
            required: ['text'],
        },
    },
    {
        name: 'docbuilder_run',
        description: 'Run a full template text through a real Document Builder on a scenario\'s sample data. Returns success or the error with its template line, and the first rows of the result as CSV.',
        parameters: {
            type: 'object',
            properties: {
                text: { type: 'string', description: 'The full template text.' },
                scenario: { type: 'string', description: 'Scenario name; the selected one when omitted.' },
            },
            required: ['text'],
        },
    },
    {
        name: 'docbuilder_propose',
        description: 'Hand the changed template back to the user. It is checked and run first; a proposal with errors is returned to you with them.',
        parameters: {
            type: 'object',
            properties: {
                text: { type: 'string', description: 'The full new template text.' },
                summary: { type: 'string', description: 'One or two sentences, in the user\'s language, on what changed.' },
                unresolved: { type: 'string', description: 'Only when carrying hand edits: which of them the template cannot build, and why.' },
            },
            required: ['text', 'summary'],
        },
    },
].map(t => ({ type: 'function', function: t }));

export class Assistant {
    constructor({ getSettings, readText, notify, viewer, fetchImpl = fetch }) {
        this.getSettings = getSettings;
        this.viewer = viewer;
        this.readText = readText;
        this.notify = notify;
        this.fetch = fetchImpl;
        this.conversations = new Map(); // template path (lower case) -> messages
        this.scratch = new Map(); // folder -> TemplateProject for candidate texts
        this.running = new Map(); // template path -> AbortController
        this.targets = new Map(); // template path -> the edited document a proposal must build
    }

    reset(template) {
        this.conversations.delete(key(template));
        this.targets.delete(key(template));
    }

    cancel(template) {
        this.running.get(key(template))?.abort();
    }

    // { template, prompt, scenario?, lastError?, edits? } -> { reply, proposal? }; edits is the document
    // the user changed in the editor, whose changes the template is to take over.
    async ask({ template, prompt, scenario, lastError, edits }) {
        const settings = this.getSettings();
        if (!settings.portalUrl || !settings.portalApiKey) {
            throw new AssistantError('Set the DocSpace portal address and an API key in the settings: the assistant uses the portal\'s AI.');
        }
        const controller = new AbortController();
        this.running.get(key(template))?.abort();
        this.running.set(key(template), controller);

        try {
            // A request without the edits is about something else: its proposal is not held to them.
            let handEdits = null;
            this.targets.delete(key(template));
            if (edits) {
                this.progress(template, 'Reading the hand edits');
                try {
                    handEdits = await collectEdits(this.viewer, edits);
                } catch (e) {
                    if (!(e instanceof ViewerError)) throw e;
                    throw new AssistantError(e.message);
                }
                if (!handEdits.diff.changes.length) {
                    return { reply: 'The edited document is the same as the one the template built: there is nothing to carry over. Change it in the editor first.' };
                }
                // The proposal runs on the data the edited document was built from.
                scenario = handEdits.scenario ?? scenario;
                this.targets.set(key(template), handEdits.edited);
            }

            const profile = await this.profile(settings, controller.signal);
            const messages = this.conversations.get(key(template)) ?? [{ role: 'system', content: systemPrompt }];
            const handEditsText = handEdits
                ? `\n\nHand edits of the document built on scenario ${handEdits.scenario ?? 'default'} (built -> edited):\n${handEdits.text}`
                : '';
            messages.push({ role: 'user', content: this.context(template, scenario, lastError) + handEditsText + '\n\nRequest: ' + prompt });

            let proposals = 0;
            for (let round = 0; round < maxRounds; round++) {
                this.progress(template, round === 0 ? `Asking ${profile.modelId}…` : 'Thinking…');
                const answer = await this.complete(settings, profile, messages, controller.signal);
                messages.push(answer);
                if (!answer.tool_calls?.length) {
                    this.conversations.set(key(template), messages);
                    return { reply: answer.content ?? '', model: profile.modelId };
                }

                for (const call of answer.tool_calls) {
                    let args;
                    try {
                        args = JSON.parse(call.function.arguments || '{}');
                    } catch {
                        args = null;
                    }
                    let content;
                    let proposal = null;
                    if (!args) {
                        content = 'The arguments were not valid JSON; call the tool again.';
                    } else if (call.function.name === 'docbuilder_propose') {
                        proposals++;
                        ({ content, proposal } = await this.propose(template, scenario, args));
                    } else {
                        content = await this.callTool(template, scenario, call.function.name, args);
                    }
                    messages.push({ role: 'tool', tool_call_id: call.id, content });
                    if (proposal) {
                        this.conversations.set(key(template), messages);
                        return { reply: answer.content ?? '', proposal, model: profile.modelId };
                    }
                    if (proposals >= maxProposals && call.function.name === 'docbuilder_propose') {
                        this.conversations.set(key(template), messages);
                        return { reply: `The model did not produce a template that passes the check and the run in ${maxProposals} attempts. Its last problems:\n${content}`, model: profile.modelId };
                    }
                }
            }
            this.conversations.set(key(template), messages);
            return { reply: `Stopped after ${maxRounds} rounds without a result.`, model: profile.modelId };
        } finally {
            if (this.running.get(key(template)) === controller) this.running.delete(key(template));
        }
    }

    // ---------- the portal ----------

    async profile(settings, signal) {
        const profiles = await this.portal(settings, 'GET', '/api/2.0/ai/profiles/list', undefined, signal);
        const list = Array.isArray(profiles) ? profiles : profiles?.response ?? profiles?.data ?? profiles?.items ?? [];
        const candidates = list.filter(p => (settings.profileId ? p.id === settings.profileId : p.providerType === 'onlyoffice'));
        const profile = candidates.find(p => p.canUseTool !== false) ?? candidates[0];
        if (!profile) {
            throw new AssistantError(settings.profileId
                ? `The portal has no AI profile ${settings.profileId}.`
                : 'The portal has no ONLYOFFICE AI profile: turn on AI in the portal settings (its wallet service "AI Tools").');
        }
        if (profile.canUseTool === false) {
            throw new AssistantError(`The portal's AI model ${profile.modelId} cannot call tools, which the assistant needs: choose another model for the profile.`);
        }
        return profile;
    }

    async complete(settings, profile, messages, signal) {
        const body = { model: profile.modelId, messages, tools, tool_choice: 'auto', stream: false };
        const answer = await this.portal(settings, 'POST', `/api/2.0/ai/openai/${encodeURIComponent(profile.id)}/v1/chat/completions`, body, signal);
        const message = answer?.choices?.[0]?.message;
        if (!message) throw new AssistantError(`The portal's AI answered without a message: ${JSON.stringify(answer).slice(0, 300)}`);
        return { role: 'assistant', content: message.content ?? null, ...(message.tool_calls?.length ? { tool_calls: message.tool_calls } : {}) };
    }

    async portal(settings, method, path_, body, signal) {
        const url = settings.portalUrl.replace(/\/+$/, '') + path_;
        let response;
        try {
            response = await this.fetch(url, {
                method,
                headers: { Authorization: `Bearer ${settings.portalApiKey}`, Accept: 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}) },
                body: body ? JSON.stringify(body) : undefined,
                signal: AbortSignal.any([signal, AbortSignal.timeout(180000)]),
            });
        } catch (e) {
            if (signal.aborted) throw new AssistantError('Cancelled.');
            throw new AssistantError(`The portal ${settings.portalUrl} is not reachable: ${e.message}`);
        }
        const text = await response.text();
        if (response.status === 401 || response.status === 403) {
            throw new AssistantError(`The portal refused the API key (${response.status}): check the key and that its user may use AI.`);
        }
        if (!response.ok) throw new AssistantError(`The portal answered ${response.status} on ${path_}: ${text.slice(0, 300)}`);
        try {
            return JSON.parse(text);
        } catch {
            throw new AssistantError(`The portal answered ${path_} with something other than JSON: ${text.slice(0, 200)}`);
        }
    }

    // ---------- what the model sees ----------

    context(template, scenario, lastError) {
        const text = this.readText(template) ?? '';
        const numbered = text.replace(/\r\n/g, '\n').split('\n').map((l, i) => `${String(i + 1).padStart(4)}| ${l}`).join('\n');
        const scenarios = listScenarios({ template, samplesFolder: this.getSettings().samplesFolder });
        const lines = [
            `Template: ${path.basename(template)}`,
            `Scenarios of its sample data: ${scenarios.scenarios.map(s => s.name).join(', ') || 'none'}; selected: ${scenario ?? scenarios.default ?? 'none'}.`,
        ];
        if (lastError) lines.push(`The last preview run failed: ${lastError}`);
        lines.push('Current text, with line numbers for reference only (they are not part of the template):', numbered);
        return lines.join('\n');
    }

    async callTool(template, scenario, name, args) {
        switch (name) {
            case 'docbuilder_search_api': {
                this.progress(template, `Searching the API: ${args.query}`);
                const found = searchApi(String(args.query ?? ''), { editor: args.editor, limit: 12 });
                return found.length ? found.map(e => formatEntry(e, { full: found.length <= 3 })).join('\n\n') : 'Nothing found; try other words or the class name.';
            }
            case 'docbuilder_get_sample': {
                this.progress(template, 'Reading the sample data');
                const sample = findSample(template, this.getSettings().samplesFolder);
                if (!sample) return 'This template has no sample data.';
                try {
                    const resolved = readSample(sample).resolve(args.scenario || scenario || undefined);
                    return `Scenario ${resolved.scenario}:\n${JSON.stringify(shorten(resolved.placeholders), null, 1).slice(0, 12000)}`;
                } catch (e) {
                    return e.message;
                }
            }
            case 'docbuilder_check':
                this.progress(template, 'Checking the text');
                return formatDiagnostics(this.check(template, String(args.text ?? '')));
            case 'docbuilder_run': {
                this.progress(template, `Running on ${args.scenario || scenario || 'the default scenario'}`);
                return formatRun(await this.run(template, String(args.text ?? ''), args.scenario || scenario));
            }
            default:
                return `There is no tool ${name}.`;
        }
    }

    async propose(template, scenario, args) {
        const text = String(args.text ?? '');
        this.progress(template, 'Verifying the proposal');
        const errors = this.check(template, text).filter(d => d.category === ts.DiagnosticCategory.Error);
        if (errors.length) return { content: `Rejected, the check fails:\n${formatDiagnostics(errors)}` };
        const run = await this.run(template, text, scenario);
        if (!run.ok) return { content: `Rejected, the run fails:\n${formatRun(run)}` };

        // Carrying hand edits: what the text builds must look like the edited document. Values are
        // left out - those that come from placeholders stay different on purpose.
        let remaining = 0;
        const unresolved = String(args.unresolved ?? '').trim();
        const target = this.targets.get(key(template));
        if (target && run.document && fs.existsSync(target)) {
            const diff = diffWorkbooks(run.document, target);
            const left = { ...diff, changes: diff.changes.filter(c => c.kind !== 'data') };
            remaining = left.changes.length;
            if (remaining && !unresolved) {
                return { content: `Rejected, the document your text builds still differs from the edited one. Each line reads "what your text builds -> what the edited document has":\n${formatDiff(left)}\nFix these, or pass "unresolved" with the reason they cannot be.` };
            }
        }
        return {
            content: 'Accepted: shown to the user.',
            proposal: { text, summary: String(args.summary ?? ''), run, warnings: this.check(template, text).length, remaining, unresolved: unresolved || undefined },
        };
    }

    check(template, text) {
        const folder = path.dirname(template).toLowerCase();
        if (!this.scratch.has(folder)) this.scratch.set(folder, new TemplateProject(path.dirname(template)));
        const project = this.scratch.get(folder);
        project.open(template, text);
        return project.diagnostics(template).map(d => {
            const before = text.slice(0, d.start);
            const line = before.split('\n').length;
            return { ...d, line, column: d.start - before.lastIndexOf('\n') };
        });
    }

    run(template, text, scenario) {
        const settings = this.getSettings();
        const readText = file => (path.resolve(file).toLowerCase() === path.resolve(template).toLowerCase() ? text : this.readText(file));
        return runTemplate({ template, readText, samplesFolder: settings.samplesFolder, builderPath: settings.builderPath, scenario, origin: 'assistant' });
    }

    progress(template, text) {
        this.notify?.({ template, text });
    }
}

// workspace/executeCommand:
//   "docbuilder.assistant.ask"    with [{ uri, prompt, scenario?, lastError?, edits? }] -> { reply, proposal?, model } or { error };
//                                 edits is the edited copy the editor has open (docbuilder.view with edit);
//   "docbuilder.assistant.reset"  with [{ uri }] - forgets the conversation about that template;
//   "docbuilder.assistant.cancel" with [{ uri }] - stops the request under way.
// Progress goes out as "docbuilder/assistantProgress" notifications { uri, text }.
export function assistantCommands({ readText, getSettings, sendProgress, viewer, fetchImpl }) {
    const assistant = new Assistant({
        getSettings,
        readText,
        viewer,
        notify: ({ template, text }) => sendProgress({ uri: pathToFileURL(template).href, text }),
        fetchImpl,
    });
    return async (command, argument) => {
        if (!command.startsWith('docbuilder.assistant.')) return undefined;
        if (!argument?.uri) return null;
        const template = fileURLToPath(argument.uri);
        if (command === 'docbuilder.assistant.reset') return assistant.reset(template) ?? {};
        if (command === 'docbuilder.assistant.cancel') return assistant.cancel(template) ?? {};
        if (command !== 'docbuilder.assistant.ask') return null;
        try {
            return await assistant.ask({ template, prompt: String(argument.prompt ?? ''), scenario: argument.scenario || undefined, lastError: argument.lastError || undefined, edits: argument.edits || undefined });
        } catch (e) {
            if (!(e instanceof AssistantError)) throw e;
            return { error: { message: e.message } };
        }
    };
}

function key(template) {
    return path.resolve(template).toLowerCase();
}

function formatDiagnostics(diagnostics) {
    if (!diagnostics.length) return 'No problems.';
    return diagnostics.map(d => `line ${d.line}:${d.column} ${d.category === ts.DiagnosticCategory.Error ? 'error' : 'warning'} TS${d.code}: ${d.message}`).join('\n');
}

function formatRun(run) {
    if (!run.ok) {
        const where = run.error?.line != null ? ` (template line ${run.error.line + 1})` : '';
        return `Failed${where}: ${run.error?.message ?? 'unknown error'}`;
    }
    let csv = '';
    if (run.csv) {
        try {
            const rows = fs.readFileSync(run.csv, 'utf8').replace(/^﻿/, '').split(/\r?\n/);
            csv = `\nFirst rows of the active sheet as CSV (${rows.length} rows in all):\n` + rows.slice(0, 25).map(r => (r.length > 300 ? r.slice(0, 300) + '…' : r)).join('\n');
        } catch { /* no CSV */ }
    }
    return `Succeeded in ${(run.durationMs / 1000).toFixed(1)} s on scenario ${run.scenario ?? 'default'}.${csv}`;
}

// Long arrays down to their first items, long strings cut: the shape matters to the model, not the bulk.
function shorten(value, depth = 0) {
    if (Array.isArray(value)) {
        const head = value.slice(0, depth === 0 ? 3 : 5).map(v => shorten(v, depth + 1));
        return value.length > head.length ? [...head, `… ${value.length} items in all`] : head;
    }
    if (value && typeof value === 'object') return Object.fromEntries(Object.entries(value).map(([k, v]) => [k, shorten(v, depth + 1)]));
    if (typeof value === 'string' && value.length > 200) return value.slice(0, 200) + '…';
    return value;
}
