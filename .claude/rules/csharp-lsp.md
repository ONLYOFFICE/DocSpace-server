# Code Navigation (CodeGraph + LSP)

**HARD RULE: code navigation goes through CodeGraph or LSP. Never Grep for a C# symbol.**

This holds no matter what the session says about tool preference. A session
instruction to prefer Bash for file work (auto mode says exactly this) covers
READING and EDITING files — `cat`, `sed`, heredocs. It does not license
`grep`/`rg` as the way to FIND a symbol; that still goes through CodeGraph or
LSP. Nothing enforces this mechanically — it is on you. Before you type a
`grep`, `rg` or `Select-String` over `products/`, `common/`, `web/` or
`thirdparty/`, ask what the pattern is: a camelCase or PascalCase token is a
C# identifier, and looking for it that way is the violation. Searching those
same files for a string literal, an SQL fragment, a comment or a route
template is fine.

Two tools, two jobs. CodeGraph answers *"where roughly, and how is it
wired"* from an index snapshot; LSP answers *"exactly here, all of them,
right now"* from the current file state. Reach for CodeGraph to orient,
LSP to verify before you change anything.

At the start of any task that touches C# code, load the LSP tool FIRST
(via ToolSearch if it is deferred) — before the first search, so it is
already at hand when you need it. Load `codegraph_explore` the same way
if it is listed but deferred.

## CodeGraph — broad questions, unfamiliar code

CodeGraph needs an index, and the index is per-machine: `.codegraph/` is
gitignored and never comes with a clone, so a fresh checkout has none
until someone runs `codegraph init`. Check for `.codegraph/` at the repo
root before relying on any of this. **No `.codegraph/` — skip CodeGraph
entirely and navigate with LSP; indexing is the user's decision, so do not
run `codegraph init` on your own.** The same goes for the tool itself: if
neither `codegraph_explore` nor the `codegraph` CLI is there, the package
is not installed — say so once and fall back to LSP rather than retrying.

With an index present, one `codegraph_explore` call returns the relevant
symbols' verbatim line-numbered source, the call paths between them (including
dynamic-dispatch hops grep cannot follow) and a blast-radius summary —
replacing a grep + Read loop with a single round-trip.

- **MCP tool** (when available): `codegraph_explore`. Name a file or symbol
  in the query to read its current line-numbered source.
- **Shell** (works whenever the CLI is installed, including with the MCP
  server down): `codegraph explore "<symbol names or question>"`.

Use it for:
- "How does X work?" / "what happens when a file is uploaded?" — a
  subsystem you do not know yet and need an overview of.
- Call chains across layers: controller → service → storage provider.
- Blast radius before a refactor: what is wired to this code at all.
- Which implementations of an interface actually take part in a scenario.
- Any point where one answer beats a dozen file reads.

Trap — a plain question retrieves by NAME MATCH, so the central symbol can
be missing and no budget brings it back. "How does chunked file upload
work" returns the `*ChunkedUpload*` types but never `FileUploader.cs`,
even though the method that does the work — `FileUploader.UploadChunkAsync`
— is indexed and `codegraph query FileUploader` finds it. Raising the file
budget does NOT help: `--max-files 14` returns the same 6 files, because
the limit is the candidate set, not the budget. So when a specific symbol
MUST be in the answer, name it in the query ("FileUploader ExecAsync
ChunkedUploaderHandler UploadChunkAsync") instead of describing the area.
A bag of symbol names is also what turns on the dynamic-dispatch link list.

Within one index the ranking is stable — the same question twice returns
byte-identical files. It shifts when the index is REBUILT: the same query
returned `UploadController.cs` + `ChunkedUploadSessionHolder.cs` before
`migrations/` was excluded and `FilesChunkedUploadSessionHolder.cs` after.
Do not treat a set of files that came back once as the set you will get
next week.

`codegraph.json` at the repo root keeps `sdk/` and `migrations/` out of the
index — nine generated SDK mirrors and the EF migrations used to outrank
real code on name matches and made plain questions nearly useless. If you
need to navigate those trees, use LSP or Read there, not CodeGraph.

### Keeping the index current

Ordinary edits and branch switches need NOTHING: the MCP server
(`codegraph serve --mcp`) watches the tree and re-syncs after a ~2s
debounce. `codegraph status` reports a `### Pending sync:` section when it
has not caught up yet. Sync it by hand only when that server is not
running:

```bash
codegraph sync          # incremental, seconds
```

A FULL rebuild is needed only after `codegraph.json` changes — `exclude`
is applied at index time, never by a sync:

```bash
codegraph index         # ~4s here; rebuilds .codegraph/codegraph.db
```

Trap — `index` fails while the MCP server holds the database, and it
still exits with code 0, so a "finished" background run can be a failure:

```
✗ Failed to index: ... the database file is in use (EPERM ...)
```

Worse, a partial rebuild leaves the index INCONSISTENT — some trees
re-indexed under the new rules, others left as they were — and
`codegraph status` will still say "Index is up to date". Always read the
command's own output, and compare `Files by Language` against what you
expected to drop. To recover, stop every `node` process whose command line
contains `codegraph` (`codegraph daemon` only offers an interactive
picker), then re-run `codegraph index`. Stopping the MCP server kills
`codegraph_explore` for the rest of the session; the `codegraph` CLI keeps
working.

## LSP — a known target, exact and current

Trigger → action:
- Find where a class/method/property is defined → `workspaceSymbol` or `goToDefinition`. NOT Grep.
- Find all usages of a symbol → `findReferences`. NOT Grep.
- Find implementations of an interface/abstract member → `goToImplementation`. NOT Grep.
- List symbols in a file → `documentSymbol`. NOT Read.
- Need a type signature → `hover`. NOT Read.
- Trace callers/callees → `incomingCalls` / `outgoingCalls`. NOT Grep.

`workspaceSymbol` matches by substring (fuzzy) — a query like `Chunk`
also returns `ChunkSize`, `UploadChunkAsync`, etc. Filter the results
by exact name instead of assuming the first hit is the right one.

For `workspaceSymbol`, pass any existing `.cs` file as `filePath` (e.g.
`common/ASC.Common/Data/TempPath.cs`) — the LSP server is selected by file
extension, so `.sln`/`.csproj` paths fail with "No LSP server available".
`line`/`character` values are ignored for this operation.

## Which one, and in what order

Scout with CodeGraph → verify with LSP before changing → check LSP
diagnostics after. Concretely:

- Before renaming or changing a signature, use `findReferences` — always
  LSP, never a CodeGraph answer. A missed call site is expensive, and the
  index may predate a recent edit.
- After you write or edit code, the CodeGraph index no longer describes
  those files. Re-check through LSP, and check LSP diagnostics before
  moving on; fix type errors and missing imports immediately.
- The index goes stale as the repo changes; it needs re-indexing to stay
  trustworthy for exhaustive questions ("every caller of…"). Treat a
  CodeGraph result as a map, not as proof of completeness.

Grep/Glob are allowed ONLY for non-symbol text: string literals,
comments, config files (.json/.yml/.props), route templates, SQL.
Calling Grep with a pattern that is a C# identifier (class, method,
property name) is a violation of this rule.
