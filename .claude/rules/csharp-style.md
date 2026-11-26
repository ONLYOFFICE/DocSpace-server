---
paths:
  - "**/*.cs"
---

# C# Conventions

## Checklist

- `dotnet format style <csproj> --include <edited files> --verify-no-changes` passes (Style Verification 1)
- A `-p:EnforceCodeStyleInBuild=true` build adds no warning; the flag is never committed (Style Verification 2-3)
- Parameters of `[LoggerMessage]` methods in touched `*Logger.cs` files are camelCase (Style Verification 5)
- Every `using` directive is in the project's `GlobalUsings.cs`, none in the edited file (Style)
- A new file has the AGPL 3.0 header, a file-scoped namespace and explicit access modifiers (Style)
- Route segments are camelCase, never snake_case or kebab-case (Naming)
- Known JSON shapes use `Deserialize<T>`; `JsonSerializerOptions` is a `private static readonly` field (JSON)
- No DAO, cache or security lookup is awaited per entry of a list; a request list has a limit (Work per listed entry)
- Numbers and dates in stored or machine-read text use the invariant culture and a format (Text for programs)
- Hot-path performance work ran `dotnet-diag`, or says it was unavailable and walked by hand (Performance work)

## Naming
- **Namespaces**: `ASC.<Module>[.<Feature>][.<Layer>]` (e.g., `ASC.Files.Core.ApiModels.RequestDto`)
- **Controllers**: `*Controller`
- **DTOs**: `*RequestDto`, `*ResponseDto`
- **Custom attributes**: `[Singleton]`, `[Scope]`, `[ApiEndpoint]`
- **Route segments**: camelCase (e.g., `{id}/externalDbSync`, `fromTemplate`) — never snake_case or kebab-case

## Style (enforced via `.editorconfig`)
- **Indentation**: 4 spaces (no tabs); 2 spaces for XML/JSON/YAML
- **`var` usage**: preferred everywhere (`csharp_style_var_*` = true:warning)
- **Namespaces**: file-scoped (`namespace Foo;`) — enforced with warning
- **Usings**: `ImplicitUsings` enabled; system directives sorted first, separated into groups. **All `using` directives must be placed in `GlobalUsings.cs`** (one per project), never in individual `.cs` files.
- **Braces**: always required (`csharp_prefer_braces` = true:warning)
- **`using` statements**: prefer simple form (`using var x = ...`)
- **Object creation**: prefer target-typed `new()` when type is apparent
- **Default expressions**: prefer `default` over `default(T)`
- **Index/Range**: prefer `^1` and `..` operators
- **Null checks**: prefer `is null` / `is not null` over `ReferenceEquals`
- **Access modifiers**: explicit modifiers required (warning)
- **Readonly fields**: enforced with warning
- **Private fields**: `_camelCase`; public fields / constants / types: `PascalCase`; interfaces: `IName`
- **XML docs**: `<summary>`, `<remarks>`, `<example>` on API models; `GenerateDocumentationFile=True`
- **License header**: AGPL 3.0 header required on all source files
- **Line endings**: CRLF; `insert_final_newline = true`; trailing whitespace trimmed

## Style Verification (mandatory after editing .cs files)

After creating or editing any `.cs` file, verify style BEFORE finishing the task:

1. **`dotnet format style <csproj> --include <edited files> --verify-no-changes`** — runs ALL
   `.editorconfig` style rules with severity warning+ (var usage, braces, namespaces, usings
   ordering, naming, ...). This step is non-negotiable because it is the only tool that catches
   naming violations (IDE1006, e.g. PascalCase parameters): `dotnet build` NEVER reports naming
   rules, even with `EnforceCodeStyleInBuild=true` — a Roslyn limitation, naming styles run only
   in the IDE layer. Do not trust a clean build for naming.
2. **`dotnet build <csproj> -p:EnforceCodeStyleInBuild=true --no-dependencies`** — complements
   step 1: catches IDE0005 (unnecessary usings, needs full compilation), compiler warnings, and
   CA quality analyzers. Fix every new warning.
3. **NEVER commit `EnforceCodeStyleInBuild` into csproj/props files** — style warnings must not fire
   for developers building locally. Pass it only as a command-line flag (`-p:...`) in step 2.
4. To auto-fix what step 1 reports, rerun `dotnet format style` without `--verify-no-changes`.
5. **Known blind spot**: parameters of `partial` methods (i.e. every `[LoggerMessage]` method in
   `*Logger` classes) are skipped by the IDE1006 naming analyzer entirely — verified empirically.
   Neither build nor `dotnet format` will flag them; only Rider/ReSharper does. After touching a
   `*Logger.cs` file, eyeball the signatures yourself: all parameters must be camelCase.

## JSON
- **Prefer typed deserialization**: `JsonSerializer.Deserialize<T>` into records/DTOs when the payload shape is known. Manual `JsonDocument`/`JsonElement` traversal only for truly dynamic shapes.
- **Cache `JsonSerializerOptions`** in a `private static readonly` field (e.g. with `PropertyNameCaseInsensitive = true`) — never allocate options per call.

## Work per listed entry
This applies to every edit, not only to the performance work below: the cost grows with the list,
and a test stand lists a few entries.
- Code that runs once per entry of a list (a DTO helper of a listing, a security filter, a loop over
  request ids) awaits no DAO, cache, security or user lookup inside the loop. Load what it needs in
  one call for the whole list before the loop (`FileSecurity.FilterReadAsync` over the entries, one
  query for the counts of all ids) and pass the result in.
- A field a `*DtoHelper` fills per entry gets its batch on every path that maps a list (folder
  content, Recent, Favorites, search, a section root), or the helper batches it itself. A per-entry
  query kept as the fallback is an N+1 on every path that does not prefetch.
- A list that comes from the request has a named upper limit, checked before the first lookup.
- Seen in review: a count per room was prefetched for one section root and read with one query per
  room in every other listing.

## Text for programs
- A number or a date written into text that is stored or read by a program (an audit description, a
  cache key, a file name, a header, a URL) is formatted with `CultureInfo.InvariantCulture` and an
  explicit format (`amount.ToString("0.##", CultureInfo.InvariantCulture)`, ISO 8601 for a date). An
  interpolated `$"{amount}"` takes the culture of the host and the scale the value arrived with:
  `10.0000`, or `10,5` on a host with another culture.

## Performance work
Only when the task is explicitly about the performance of a **hot path** (code that runs per request
or per listed item: security filters, DAO calls, serialization) — not as part of the normal edit loop.

1. Invoke the `dotnet-diag:analyzing-dotnet-performance` skill (plugin `dotnet-diag`, enabled in
   `.claude/settings.json`). Its value is the checklist discipline: ~50 anti-patterns, exact hit
   counts, and an explicit list of things NOT to flag (LINQ off the hot path, `ConfigureAwait(false)`
   in app code, `Span<T>` in async methods).
2. **Treat it as a reading guide, not a verdict.** On `FileSecurity.cs` every string and regex recipe
   returned 0 hits, and both real findings came from reading the code around a hit, not from the
   catalogue itself.
3. **Measure before acting.** Analyzer/CPU savings do not automatically become wall-clock savings:
   disabling ASP0017/ASP0018 cut 11.9s of analyzer CPU off a full rebuild and changed the build time
   by nothing, because the critical path is the project dependency chain, not the analysis.
4. **If the skill is unavailable** (`Unknown skill` — the `dotnet-diag` plugin is off), say so
   instead of silently skipping the step, and walk the hot path by hand looking for:
   - `await` inside `foreach`/`for` — N sequential DB/cache round-trips where one batched call would do;
   - per-item allocations inside the loop: `new Dictionary`/`new List`, `Enum.GetValues<T>()`,
     LINQ closures — anything rebuilt for every entry instead of hoisted or held in a static;
   - `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` — sync-over-async;
   - linear `Contains` over an `IEnumerable`/`List` where the set is fixed and a `FrozenSet` fits.

   Read the surrounding method either way — a hit count is not a finding.

## API Patterns
- API versioning via `Asp.Versioning`
- Swagger annotations for OpenAPI generation
- Controllers inherit common base, use `[ApiEndpoint]` attribute (sets the route template and controller name)
- Request/Response models in `ApiModels/RequestDto` and `ApiModels/ResponseDto` namespaces
