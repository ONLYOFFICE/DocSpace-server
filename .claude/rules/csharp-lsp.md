# Code Intelligence (C# via LSP)

**HARD RULE: C# code navigation goes through LSP ONLY. Never Grep for a C# symbol.**

At the start of any task that touches C# code, load the LSP tool FIRST
(via ToolSearch if it is deferred) — before the first search, so it is
already at hand when you need it.

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

Before renaming or changing a function signature, use `findReferences`
to find all call sites first.

Grep/Glob are allowed ONLY for non-symbol text: string literals,
comments, config files (.json/.yml/.props), route templates, SQL.
Calling Grep with a pattern that is a C# identifier (class, method,
property name) is a violation of this rule.

After writing or editing code, check LSP diagnostics before moving on.
Fix any type errors or missing imports immediately.