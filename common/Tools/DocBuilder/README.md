# DocBuilder tools

Editor support for the ONLYOFFICE Document Builder scripts (`*.docbuilder`) in
`products/ASC.Files/Core/Services/DocumentBuilderService/ScriptTemplates`: completion and
documentation of the builder API, error checking, and a preview of the document a script builds.

| Folder | What it is |
|---|---|
| `typings/` | `generate.mjs` turns the api.onlyoffice.com reference pages into `onlyoffice-docbuilder.d.ts` |
| `language-server/` | an LSP server (Node.js) over that file: diagnostics, completion, hover, signature help, definition, outline, the `docbuilder.run` preview, the viewer/editor and the assistant |
| `ide/visualstudio/` | the Visual Studio 2022/2026 extension (VSIX) around the server |

## What the server knows about these scripts

- `${name}` placeholders are masked as same-length identifiers, so templates check clean as they are.
  A template named like a placeholder (`ReportTableStyle` for `${reportTableStyle}`) is a shared
  script: it is checked as a global script and sees the variables of the templates that paste it.
- A line starting with `builder.` is a builder command, not JavaScript: `builder` inside code is an
  error (TS90001), and a command takes literal arguments only (TS90002).
- A member that exists on only some of the types a value can have (`GetElement(0).AddText`) is a
  warning, not an error.
- The builder drops blank lines and numbers an error's line within the code between two commands;
  the preview maps it back to the template line.

## Build and test

```bash
cd common/Tools/DocBuilder/language-server
npm install
npm test            # LSP tests; the preview tests run only where a Document Builder is installed
npm run check       # type-check every template, as the editor does
npm run bundle      # dist/ for the IDE plugins
```

Regenerate the declarations from the published reference (`git fetch` the site repository first):

```bash
node common/Tools/DocBuilder/typings/generate.mjs <api.onlyoffice.com checkout> --ref origin/master
```

## Preview

`docbuilder.run` fills the template's placeholders from its sample, pastes shared scripts, runs a
local `docbuilder.exe` and returns the saved document plus a PDF of it (a sheet printed at its own
size on E-sheet landscape paper, scaled down only when wider still). The builder is looked up in the
ONLYOFFICE Document Builder and Document Server install folders or `DOCBUILDER_PATH`. The free
Document Builder adds a watermark; the one inside Document Server does not.

`docbuilder.view` shows that document in the live viewer instead: the server serves the document
and a page embedding the Document Server editor (read-only) from a loopback HTTP server, one random
token per document, and only files under the preview folder. Document Server downloads the document
from it, so it must reach this machine: `127.0.0.1` for one installed here, `fileHost`
(`host.docker.internal`) for one in a container. The editor configuration is signed with the JWT
secret from the settings or from the `local.json` of a Document Server installed here; the server is
the configured one or the first local one answering `/healthcheck` (`:8085`, `:80`, `:8080`).

With `edit`, the same page opens an editable copy (`_edits/<Template>/<run>/` under the preview folder,
next to `original.xlsx`, out of reach of the clean-up of run folders). Document Server saves it through
a callback this server answers (the callback's JWT is checked, and the file is fetched only from the
Document Server the editor runs on); Ctrl+S saves at once. `docbuilder.edits` forces a save through the
command service and compares the copy with the original straight from the workbook XML - cell values,
fonts, fills, borders, number formats, alignment, column widths, row heights, merges, frozen panes,
tables - grouped as one line per property and from/to pair over the areas it covers:

```
[Audit Trail Report] B7:B24 font.bold: false -> true
Data: 1 cell(s) hold another value or formula.
[Audit Trail Report] A2 value: "Report:" -> "Changed by hand"
```

A template cannot be derived from a document, so the edits go back through the assistant: it gets the
list with the request, and a proposal counts only when what it builds (on the scenario the edited
document came from) no longer differs from the edited document in formatting, or when the model says
which edits it cannot carry and why. Changed values are reported, not enforced: a value that comes
from a placeholder is sample data. Spreadsheets only, as all templates build xlsx; drawings and charts
are not compared.

## Assistant

The preview window carries an assistant that changes the template on request ("add a size column
with a total") or fixes the error of the last run. The model is the DocSpace portal's: the server
reads the portal's AI profiles (`GET /api/2.0/ai/profiles/list`), takes the `onlyoffice` one - the
portal's own AI, paid from its wallet - and talks to it through the OpenAI-compatible entry of
`ASC.AI.Chat`, `POST /api/2.0/ai/openai/{profileId}/v1/chat/completions`, with the user's DocSpace API
key as a bearer token. The portal is only called, never calls back, so a cloud portal works too.

The tools run here: `docbuilder_search_api` (the declarations, indexed), `docbuilder_get_sample`,
`docbuilder_check`, `docbuilder_run` (a real run, with the first rows of the active sheet as CSV so
the model can read the cells) and `docbuilder_propose`. A proposal is checked and run again before
it counts; one that fails goes back to the model with the errors, up to three times. What reaches
the user has passed both, and the preview shows what it builds; Diff opens it against the editor's
text, Apply puts it into the editor as one undoable change and saves nothing.

Settings: Tools > Options > DocBuilder Scripts > Assistant (portal, API key, optionally a profile id).

## Sample data

`<Template>.sample.json` next to the template holds named scenarios; the preview window lists them. It lives there so that the template, the C# that fills its placeholders and the sample change in one diff; `ASC.Files.Core` builds only the templates it lists as `EmbeddedResource`, so the samples are neither embedded, copied nor published. A scenario is a
deep merge - the file's `extends`, its `base` scenario, its own `extends`, its `placeholders` -
so it states only what differs. Shared parts live in `ScriptTemplates/_shared/` (the report header per
culture). A file with a bare `placeholders` object is a single scenario.

```json
{
  "extends": ["_shared/header.en-US"],
  "default": "audit",
  "scenarios": {
    "audit":    { "description": "...", "placeholders": { "inputData": { ... }, "dataValues": [ ... ] } },
    "rowLimit": { "base": "audit", "placeholders": { "inputData": { "info": { "rowLimit": "..." } } } },
    "large":    { "base": "audit", "placeholders": { "dataValues": { "$generate": { "count": 5000, "seed": 7, "strings": true, "columns": ["ip", "country", "date", "user"] } } } },
    "ru-RU":    { "base": "audit", "extends": ["_shared/header.ru-RU"], "placeholders": { ... } }
  }
}
```

`$generate` makes rows from a seed - the same rows on every run - as arrays (`columns`) or objects
(`fields`), in `en-US` or `ru-RU` formats. Kinds: `ip`, `country`, `city`, `browser`, `platform`,
`user`, `email`, `date` (`from`, `to`, `order`, `format`: `G`/`d`/`iso`), `url`, `action` (`login`),
`actionType`, `location`, `actionId`, `targetId`, `fileName`, `folderName`, `word`, `sentence`,
`longText`, `int`, `number`, `bool`, `id`, `pick` (`values`), `const` (`value`); any column takes
`nulls` (0..1). The full list is in `language-server/src/generate.mjs`.

## Visual Studio

```bash
cd common/Tools/DocBuilder/language-server && npm install && npm run bundle
msbuild common/Tools/DocBuilder/ide/visualstudio/DocBuilder.VisualStudio/DocBuilder.VisualStudio.csproj -restore -p:Configuration=Release
```

Install `bin/Release/net472/DocBuilder.VisualStudio.vsix`, or press F5 in the project for an
experimental instance. Tools > Options > DocBuilder Scripts holds the Node.js, Document Builder and
samples paths and "Run on save". The preview window is Tools > DocBuilder Preview; Ctrl+Shift+Alt+F5
runs the active template.
