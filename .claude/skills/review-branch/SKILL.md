---
name: review-branch
description: "Review a DocSpace branch against the repo's own rules (`.claude/rules/*.md`, CLAUDE.md) for correctness, performance and security: scope the diff against the right base, run the mechanical scan, fan out rule/correctness/contract/performance/security/translation reviewers, verify every finding in the code, check build and style, and report in a fixed format. Also the re-check after fixes: which findings are closed, which are still open, what is new. USE FOR: review the branch, look at the changes in a branch, does the branch follow the rules, the fixes were merged - check them, re-check once more, check the fixes against the review file - in any language the user writes in. DO NOT USE FOR: fixing the findings (that is a separate request), a single-file question, a security-only audit (claude-security), a performance-only pass (dotnet-diag:analyzing-dotnet-performance), running a suite without a review (run-tests)."
---

# Review a branch against the repo rules

The review answers two questions: **does the branch follow the rules written in this repo**, and
**does it do what it claims without breaking something else**. Everything else — style taste,
alternative designs, "I would have written it differently" — stays out unless a rule backs it.

The review is read-only. Never edit product code, never commit, never switch branches, never
touch submodule checkouts or the user's running DocSpace. Fixing is a separate request.

Report in the user's language. Section headings below are given in English; translate them along
with the rest of the report.

## 0. Which mode

- **First review** — nothing reviewed on this branch yet in this conversation, and the user gave no
  review file.
- **Re-check** — "the fixes are merged, check", "re-check", a path to a previous review `.md`, or an
  earlier review in this conversation. Go to §7 after §1.

Arguments the user may pass in words: a branch other than the checked-out one, a base, "including
uncommitted changes" (→ `--worktree`), "run the tests" (→ §6 runs, not just proposes).

## Dependencies — none of them is allowed to stop the review

Some tools this skill uses are optional on a given machine. A missing one degrades one step; it
never aborts the review. Every degradation is named in the report header ("performance: manual
checklist only, dotnet-diag not installed"), so a thinner review is never mistaken for a full one.

| Dependency | Where it comes from | If it is missing |
|---|---|---|
| `python` 3.9+ (`python3` on Linux/macOS) | machine | do §1 by hand: `git merge-base`, `git log --oneline`, `git diff --name-status`, `git diff --check`, and the line-ending and migration checks from §2 on the changed files; there are no rule signals, so reviewers work from the diff alone |
| LSP (`csharp-lsp` plugin) | `.claude/settings.json` `enabledPlugins` | say so once; navigate with `codegraph_explore` if `.codegraph/` exists, otherwise by reading the files. Blast-radius claims ("all call sites") are then marked unverified |
| `codegraph_explore` | user-installed, per-machine index | skip it silently, LSP only (`csharp-lsp.md`) |
| `dotnet-diag:analyzing-dotnet-performance` | `dotnet-diag` plugin, marketplace `dotnet/skills` | reviewer D walks its own checklist (§3 D.3) plus the manual list in `csharp-style.md` → Performance work |
| `claude-security` | user-level plugin, not in the repo settings | do not offer the deep scan; reviewer E's checklist is the whole security pass || `dotnet` SDK / a free build | machine; a running DocSpace locks DLLs | §5 reports "not built" with the reason; the review continues on reading |
| repo skills (`run-tests`, `db-migrations`, `notify-emails`) | `.claude/skills/` | always present with the repo |

How to tell: a skill is available only if it is in the session's skill list; calling an absent one
returns `Unknown skill` — treat that as "missing", not as an error to retry. A subagent may not
see the same skills or tools as you: tell reviewer D to fall back on its own when the skill call
fails, and do not rerun the reviewer because of it.

## 1. Scope the diff

```bash
git fetch origin --quiet
python .claude/skills/review-branch/scan.py            # add --base origin/develop, --worktree as needed
```

`scan.py` picks the base as the remote branch with the fewest commits ahead of it (newest
`origin/release/*`, then `origin/develop`, `origin/master`) and diffs from the **merge-base**, so
merges of the base into the branch do not show up as branch changes. Read its header and check:

- **The base is right.** A `feature/*` cut from `develop` but scanned against `release/*` drags in
  half of develop. If the commit list contains commits that are obviously not this branch's work,
  re-run with an explicit `--base`. **0 commits** — wrong branch or already merged: stop and say so.
- **The tree is clean**, or the user asked for `--worktree`. Uncommitted changes are not in the
  scan otherwise — say which one you reviewed.
- **Remember `HEAD`'s sha.** Branches here move during a review (a merge of the base, a rename of
  a migration). Check it again before the report; if it moved, re-scan and say what the new commits
  changed.

Then read the commit messages: they are the branch's **declared intent**, the yardstick for scope
creep in §3.

## 2. The scan output is hints, not findings

Every section of `scan.py` is a cheap textual signal. Nothing from it goes into the report until it
is read in context. What each section is for:

| Section | Turns into a finding when |
|---|---|
| Rule signals | the hit really breaks the cited rule in that place (a `RemoveByTagAsync` on the *default* FusionCache is fine; on the `"memory"` one it is a bug). `[security]` and `[performance]` hits feed reviewers E and D |
| Line-ending flips | always report: a whole-file LF↔CRLF flip turns 30 real lines into a 8 000-line diff, kills blame and conflicts every parallel branch. Fix = separate normalization commit or revert in this branch. Check first that the base itself did not normalize the file |
| `git diff --check` | trailing whitespace / conflict markers in hand-written code (generated `*.Designer.cs` — ignore) |
| Migrations | §3 C |
| API contract and SDK | §3 C |
| Localized resources (.resx) | §3 F. `!! placeholder mismatch` is a finding once you have looked at the value (a tag lost or renamed in a translation renders as literal `$Tag` or as nothing); "neutral changed, translation untouched" is one only if the commits do not declare it ("English only for now") |
| Tests | §3 A3 and §6 |
| Possibly committed by accident | a review `.md`, a `.binlog`, `.env` committed by accident |

Things `scan.py` cannot see and you must look for yourself: broken indentation left by a merge,
`<summary>`/`<remarks>` swapped, a new project not added to `ASC.Web.slnx` / `ASC.Tests.slnx`.

## 3. Fan out reviewers

Load LSP first (ToolSearch if deferred); use `codegraph_explore` only if `.codegraph/` exists
(`.claude/rules/csharp-lsp.md`). Then launch the reviewers that apply **in one message** with the
Agent tool (`general-purpose`), so they run in parallel. Skip a reviewer whose area the branch does
not touch. Each prompt must be self-contained: base, merge-base, branch sha, the file list for its
area, the relevant scan hints, the rule files to read, and the return format below.

**A. Rules compliance.** The set of rules to check is **not** fixed in this skill — it is the
"Applicable rules" section of the scan, which reads `.claude/rules/*.md` and matches each rule's
`paths:` frontmatter against the diff. A rule added to the repo tomorrow shows up there without any
edit here. Distribute every applicable rule to a reviewer:

- the groups below are the default bundles for the rules that exist today;
- a rule not named in any group gets **its own reviewer** (or joins the group whose files it covers,
  when it is short) — never drop it because this skill does not mention it;
- a rule listed as "always" (no `paths:`) is checked only if it constrains the code itself;
  `csharp-lsp.md` constrains how *you* navigate and is not a review criterion;
- the bullet lists are reminders of what past reviews caught, not the rule. The reviewer reads the
  rule file in full and checks against it — including its Definition of done, if it has one.

- **A1** — `csharp-style.md`, `logging.md`, `caching.md`, `http-clients.md` over the changed
  product `.cs` files. Includes: `using` only in `GlobalUsings.cs`, AGPL header on new files,
  `[LoggerMessage]` with camelCase parameters (the analyzer is blind to partial-method parameters),
  FusionCache with explicit duration and `tenantId` in key and tag, tags through `CacheExtention`,
  no new named `HttpClient`, JSON options cached, no sync-over-async.
- **A2** — `openapi-endpoint-docs.md` and `openapi-dto-docs.md`, only if controllers, DTOs,
  `ApiModels` or enums changed. Walk each rule's **Definition of done** checklist per changed
  operation/property; `<summary>` = title, `<remarks>` = description (inverted!), every code named
  in prose has a `[SwaggerResponse]`, realistic `<example>`, behavioural facts on the operation, not
  the property; for a shared DTO find every operation that binds it.
- **A3** — `tests.md`, only if test files changed. One feature = one folder, class under ~24 cases,
  SDK not raw HTTP outside the carve-outs, `ApiException` assertions, `TestContext.Current.CancellationToken`,
  deadline polling (no fixed `Task.Delay` before an assert, no timeout token inside the loop),
  access matrices that match `FileSecurity.AvailableRoomAccesses`, `[Trait("Bug")]` instead of Skip.

**B. Correctness and behaviour** — always:

- bugs on the changed lines: null paths, lost values, off-by-one, wrong cancellation, races,
  transactions, disposed readers held across awaits;
- **scope creep**: every behaviour change the commit messages do not declare (a condition widened
  in a security check, a default changed) — not necessarily wrong, but it must be named;
- **blast radius**: for every changed member whose meaning changed (`IsForm` began to mean "any
  PDF"), run `findReferences` and walk the call sites — security filters, share rights, DTO
  mapping, sockets. This is where the expensive findings were in past reviews;
- before calling a semantic flag a defect, read its documented meaning (XML doc, commit message):
  a `Visible` flag documented as "shown in UI pickers" is not an access control. When the code
  matches the documented intent and only the intent is debatable — it is a question, not a bug.

**C. Data, contract, infrastructure** — if the branch touches migrations, contract files,
`*.csproj`/`*.props`, AppHost, submodules or `ASC.Data.Backup*`:

- migrations: which of the four variants `migrations/{mysql,postgre}/{SaaS,Standalone}` changed
  and whether that is justified (`db-migrations` skill; a standalone-only change can be
  deliberate); `*.Designer.cs` and the model snapshot updated; ordering after the newest migration
  in the base; no edit of an already-applied migration; a new table is wired into backup/restore;
- contract: a renamed/removed property, a `bool` → `bool?`, a changed route is a breaking change for
  the SDKs — were the spec and `sdk/*` submodule pointers updated, or is regeneration knowingly
  postponed? Is the submodule pointer in the branch the one tests were built against?
- packages and TFM only in `Directory.Packages.props`; no `EnforceCodeStyleInBuild` committed; new
  projects in `ASC.Web.slnx` / `ASC.Tests.slnx`.

**D. Performance** — always, over the changed product `.cs` files (not tests, not migrations):

1. Sort the files into **hot path** (runs per request or per listed item: security filters, DAO
   list/search queries, DTO mapping of lists, serialization, per-row loops of reports, exports,
   indexers, socket notifications) and **the rest**.
2. Hot-path files: invoke the `dotnet-diag:analyzing-dotnet-performance` skill over them and treat
   it as a reading guide, as `csharp-style.md` → Performance work prescribes — a hit count is not a
   finding; read the method around each hit. If the skill is unavailable, say so and walk the
   manual list from that section.
3. All changed files, the cheap DocSpace list: N+1 — `await` of a DAO/cache/user lookup inside a
   loop over entries; a query without a page bound or an unbounded `IN (...)`; materializing
   (`ToListAsync`) before filtering; a new query shape with no supporting index in the migration;
   a reader/`DbContext` held open across per-row awaits; per-item allocations that could be hoisted
   (`JsonSerializerOptions`, `Regex`, `Enum.GetValues`, LINQ closures); reflection per cell;
   repeated encoding of the same string; a cache read per item where one batched read would do
   (`caching.md`); a new call on a hot path that adds round-trips to every request (e.g. extra
   `GetSharedInfo` per file).
4. Severity: a cost that grows with data on a hot path is a remark (blocker only for a clear
   O(n) round-trips per request on a list endpoint); off the hot path — omit unless gross. Every
   performance finding states the scale it matters at ("5 000 queries per page of 100") and that
   it is static analysis, not a measurement.

**E. Security** — always, over the changed product files (`.cs`, `.ts`, configs, nginx):

- **authorization**: every new or changed endpoint and service method checks access on the entry
  it actually acts on (`FileSecurity.Can*Async` / the role check) **before** acting, not on its
  parent or after the fact; `[AllowAnonymous]` and `<requiresAuthorization>false` are deliberate;
  an id taken from the request is never used without that check (IDOR); a widened condition in
  `FileSecurity` / share-rights code is a security change even when the commit calls it a fix;
- **tenant isolation**: every new DAO query filters by `TenantId`; cache keys and tags carry the
  tenant (`caching.md`); background tasks restore the tenant and user context they run under;
- **anonymous, guest and public-link paths**: what a link-holder or guest can read or enumerate
  through the new code (names of fields, ids, other users' data); link tokens and passwords
  validated on every path, including the new one;
- **input**: length and count limits, `Enum.IsDefined`, `[Range]` on paging (DoS); raw SQL
  (`FromSqlRaw`/`ExecuteSqlRaw`/interpolated SQL); file names and storage paths from user input
  (path traversal, zip slip); content-type and size checks on uploads;
- **outbound requests**: a user-supplied URL goes only through `UrlValidator.PinnedHttpClient`
  (`http-clients.md`); no disabled certificate validation outside the `SslIgnore` clients;
- **injection into output**: user values reaching HTML, notification letters (`notify-emails`),
  editor config, CSV/XLSX exports (formula injection), log lines;
- **secrets and crypto**: no tokens, passwords, keys or PII in logs or response DTOs; nothing
  hardcoded; `RandomNumberGenerator` for tokens, no MD5/SHA1 for security; no
  `TypeNameHandling`/`BinaryFormatter`;
- **audit**: a security-relevant action (access change, deletion, settings change) writes an audit
  event the way its neighbours do.

A security finding states the attacker (anonymous, link-holder, guest, user of another tenant,
room member with a lower role) and what they gain. Without a concrete attacker and gain it is a
remark or a question, not a blocker. When the branch touches auth, sharing, public links or
login and the user wants more depth — and only if `claude-security` is in the skill list — offer its change scan as a
follow-up — do not start it yourself, it is a separate, heavy run.

**F. Translations** — only if the branch changes `.resx` values (the scan's "Localized resources"
section). The scan gives the keys whose neutral text changed and the mechanical facts per culture;
F judges the text itself, for those keys only, never the whole file:

- **meaning**: each culture says what the neutral text says now — nothing lost, nothing added, no
  leftover of the old wording when the neutral text was rewritten;
- **placeholders and markup**: every `$Tag`, `{0}` and textile/HTML mark of the neutral text is
  present, unchanged and not translated. Tag names are ASCII (`NVelocityPatternFormatter.DefaultPattern`),
  so a suffix glued on (`$UserName님`) is fine; a translated or misspelled tag is not;
- **terminology**: names of services, plans and units match the same entities elsewhere in that
  culture (`Resource.<culture>.resx` and siblings) — a letter that says "Business plan" must not call
  it differently from the billing page in the same language;
- **form**: the same register and form of address as the neighbouring texts of that culture, no
  machine-translation artefacts (untranslated English fragments, literal word order), grammar that
  survives the substituted values (a number or a unit inserted into a sentence that needs agreement).

F reads the translations itself. Findings in low-resource locales (`hy-AM`, `sq-AL`, `az`, `sr-*`,
`lv`, `sl`, ...) are marked lower-confidence.

Severity: a lost or broken placeholder in a value that is rendered — blocker (the customer sees
`$ServiceName` or an empty slot); a meaning that differs from the neutral text — remark, with the
neutral text, the translation and a back-translation side by side; untranslated cultures — a remark
only when the branch does not say so on purpose. Do not report wording taste.

**Return format for every reviewer** (ask for exactly this):

```
- severity: blocker | rule | remark | question
  category: correctness | rule | performance | security | contract | translation
  where: path:line
  rule: <rule file §section>  (or "—" for a correctness finding)
  what: one sentence
  scenario: concrete input/state → wrong result (for bugs), or the rule text it breaks
  fix: one sentence
  introduced_by_branch: yes | no (exists in the merge-base already)
```

Tell them: read the rule file in full before judging; navigate with LSP, not grep, for symbols;
cite only code they have actually opened; do not edit anything; no praise section.

## 4. Verify before reporting

Reviewers over-report. For every finding, open the code at `where` yourself and confirm it:

- the line exists on the reviewed sha and says what the finding claims;
- `introduced_by_branch: no` → drop it, or keep it in one line under "not from this branch" only when the
  branch makes it worse (moved it onto a hot path, widened its reach);
- a rule finding cites a rule that really says that (open the rule, check the section);
- duplicates across reviewers merged; a finding that needs a product decision moved to "Questions".

Drop what you cannot confirm. A short report of true findings beats a long one the user has to
re-check.

## 5. Build and style

Build only the projects the branch touches, narrowly — a running local DocSpace locks the shared
DLLs (`MSB3027`); report that as "not built: DLLs locked", do not stop the user's processes.

```bash
dotnet build <csproj> --no-dependencies -p:EnforceCodeStyleInBuild=true
dotnet format style <csproj> --include <changed .cs files> --verify-no-changes
```

New warnings in the changed files are findings under `csharp-style.md`. `dotnet format` is the
only check that sees naming (IDE1006); neither sees partial-method parameters of `*Logger` classes —
eyeball those.

## 6. Tests

Name the suites that cover the changed area and give the exact command (`run-tests` skill for
filter syntax). Run them only if the user asked; otherwise end the report with the command.

When running, the known traps are environmental, not the branch's: a local DocSpace holding DLLs,
a stale Aspire DCP bundle (`aspire restore`), service binaries older than the product code (build
the services, not only the test project), a submodule checkout that differs from the recorded
pointer. Tell a flake from a regression by re-running the single test and checking whether the
changed code is on its path. A red test that encodes the **old** behaviour the branch deliberately
changed is a question for the user (fix the expectation or the code), not a regression.

## 7. Re-check mode

Input: the previous findings (a `.md` path the user gave, or the last review in this
conversation). Re-scan (§1) and then:

1. For **each** previous item, read the current code at its place and classify: **Fixed**
   (how, with `path:line`), **Partially fixed**, **Not fixed**. Do not trust the commit message
   ("review fixes") — check each item.
2. Review only what changed since the previous review (`git log <prev-sha>..HEAD`, diff of those
   commits) with §3–§4 for **New** — a fix commit is where new bugs come from.
3. If an earlier finding turns out to be wrong, say so plainly and withdraw it.

## 8. Report

Header line: branch, base, number of commits and files, sha reviewed, what was run (build / format
/ tests) and what was **not** — "verified by reading the code, tests not run" when that is the case.

Then, omitting empty sections:

```
## 🔴 Blockers         — data loss, security hole, regression, broken contract
## 🟠 Rule violations  — each with the rule file and §: [caching.md] …
## 🟡 Remarks          — real but not blocking
## ❓ Questions        — behaviour changes and debatable semantics that need the author's decision
## Not verified       — what stays unverified and what would verify it (a suite, a profile)
```

Per finding: a `[security]` / `[performance]` / `[translation]` tag when that is its category, the `[path:line](path:line)` link, what is wrong, the concrete scenario, the fix. Code
snippets only when the line itself is the point (broken indentation, a wrong condition). No scan
tables, no "what was done well" section — at most one line if something deserves it.

Re-check report: **Fixed / Not fixed / New**, same per-item format.

Do not report:
- a renamed or re-numbered migration as a risk "for local DBs where the old one was applied" — the
  user drops that every time;
- formatting the generated files (`*.Designer.cs`, `json/*_2.0.json`, SDK sources);
- anything already present in the merge-base, unless the branch makes it worse (§4).

End with one line offering the next step: fix the blockers, run the named suites, or save the
findings to a file.

### Saving to a file

On request ("save to md"), write only the findings — no header statistics, no "Fixed"
unless asked — to `<branch-name-with-dashes>-review.md` at the repo root. Make each item readable
without this conversation: place, scenario, fix. The file stays untracked; never `git add` it.
