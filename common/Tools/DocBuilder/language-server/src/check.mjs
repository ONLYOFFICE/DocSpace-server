#!/usr/bin/env node
// Type-checks *.docbuilder templates against the Document Builder declarations, with the same rules
// the language server applies in the editor.
//
//   node src/check.mjs [templates folder]

import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { TemplateProject, ts } from './project.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const folder = process.argv[2]
    ?? path.resolve(here, '../../../../../products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates');

const project = new TemplateProject(folder);
const templates = project.templates();
let errors = 0;

for (const template of templates) {
    const text = project.sourceText(template);
    for (const d of project.diagnostics(template)) {
        const { line, character } = ts.getLineAndCharacterOfPosition(ts.createSourceFile('x', text, ts.ScriptTarget.Latest), d.start);
        const kind = d.category === ts.DiagnosticCategory.Error ? 'error' : 'warning';
        console.log(`${template}(${line + 1},${character + 1}): ${kind} TS${d.code}: ${d.message}`);
        if (kind === 'error') errors++;
    }
}

if (!templates.length) {
    console.error(`no *.docbuilder files in ${folder}`);
    process.exit(2);
}
console.log(errors ? `${errors} error(s) in ${templates.length} templates` : `${templates.length} templates OK`);
process.exit(errors ? 1 : 0);
