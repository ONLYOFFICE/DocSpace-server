# Project facts: the generator, and the traps around it

Durable findings about how this repository turns C# into OpenAPI documents, and about the behaviour of
the tools in the loop. They live here rather than in `SKILL.md` because they are facts about the
project, not steps of the procedure, and rather than in the run memory because they outlive any single
measurement — the memory records what a run *measured*, this file records what the project *is*.

Read it in step 3, before opening any generator source: most entries below cost a careful reading to
establish and take seconds to re-use. Add to it whenever you work something out that is not here yet,
with the date and what proved it. Two things to keep out: **measurements**, which belong in the memory
because they change with every run, and **anything true only of one machine** — a locale, an execution
policy, an installed version, which shell was in use. Those make the file wrong for the next reader
rather than merely stale. Where an entry names a line number, check it still points at what it claims
before relying on it.

## Generator facts

**`<summary>` on a controller action becomes the operation's `summary`; `<remarks>` becomes its
`description`.** The convention in this repository is **inverted relative to ordinary C# habit**:
controllers put the *short title* in `<summary>` and the *long explanatory sentence* in `<remarks>`,
and write the `<remarks>` block first. Do not "helpfully" swap them — the swap silently exchanges an
operation's title and its description throughout the published contract and every generated SDK.
(2026-08-28.)

**The mechanism is Swashbuckle's built-in XML-comments filter**, installed by
`c.IncludeXmlComments(() => doc)` in `common/ASC.Api.Core/Extensions/OpenApiExtension.cs` — two call
sites, at lines 292 and 308 (re-verified 2026-09-01; they were 262 and 278 on 2026-08-28, so grep for
the call rather than trusting the number). Line 313 carries a load-bearing comment: the
`XmlCommentsMemberDescriptionSchemaFilter` on the next line must stay after every `IncludeXmlComments`
call. An `info`-level fix, an envelope fix and an XML-doc fix therefore all meet in this one file, so
read it before reordering anything in it.

**An XML `<param>` tag cannot describe a route placeholder that no DTO property binds.** Swashbuckle
matches `<param name="x">` against the *method's* own parameters, and these controllers take a single
DTO argument — so a tag naming a route placeholder matches nothing, reaches no document, and costs two
compiler warnings on the way (`CS1572` for the tag with no parameter, plus `CS1573` on the DTO
argument, which the tag has just made "partially documented"). Measured 2026-09-01 on
`SaveFormRoleMapping`: tag added, project rebuilt, the emitted parameter still had no `description`.

The place to put that text is `[SwaggerPathParameter(name, description)]` on the action, read by
`SwaggerPathParameterFilter` in `common/ASC.Common/Utils/SwaggerCustomOperationFilter.cs`. It is
fill-in only, so it never fights a description that arrives some other way. Reach for it **only** when
the placeholder genuinely cannot be bound: if a DTO property can carry the value, `[FromRoute]` plus an
XML `<summary>` on that property both documents the parameter and binds it, which is strictly better.

**An operation can carry a `description` and no `summary`.** It then looks perfectly documented to
anyone skimming the document, and `operation-description` stays silent, because it is a separate rule
watching a separate field. A present `description` is never evidence that the `<summary>` tag
survived — check the field the failing rule actually names.

**The XML documentation artefact is not always under a TFM subdirectory.** A project that overrides
its output path puts it directly in `bin/Debug/`; one that does not puts it in `bin/Debug/net10.0/`.
Both layouts exist here, so locate the file instead of assuming either shape (2026-08-28):

```bash
find <project-dir>/bin/Debug -name '<Assembly>.xml'
```

## Tool traps

**An unrecognised XML doc tag produces no diagnostic anywhere in the C# toolchain.** A misspelled tag
is copied into the built `.xml` verbatim; the build reports nothing even with
`EnforceCodeStyleInBuild`, and `dotnet format style --verify-no-changes` is equally content. Spectral,
at the document level, is the only tool in the chain that sees it.

The reason matters, because it inverts the intuition about which typo is dangerous. Roslyn's CS1570
checks that a doc comment is *well-formed XML*, and it validates a few known tags (`param`,
`typeparam`, `cref`), but it never checks an element *name* against a known set:

| the typo | XML | caught? |
|---|---|---|
| `<ssummary>…</ssummary>` — both tags misspelled | well-formed, unknown element name | **silent, everywhere** |
| `<ssummary>…</summary>` — one tag misspelled | ill-formed | CS1570 fires |

So the careless-looking version is the safe one, and the version where the author typed the same
mistake twice is invisible until a document-level check runs. This has happened here: `<ssummary>` in
`common/ASC.Common/Security/Cryptography/PasswordHasher.cs`, fixed by commit `fa41eecd8b` ("fix linter
findings"). The sweep costs nothing when you suspect another:

```bash
grep -rn 'ssummary' --include=*.cs .
```

The consequence for this loop: step 6's check of the built `.xml` is the only cheap thing that catches
this class of defect before a regeneration, because the build, the style check and a reading of the
diff all pass.

**Spectral prints nothing when the output flags are set.** With `--output.html`, `--output.markdown`
and `--output.json` given, a successful run writes the three files and says nothing at all, while
`--fail-severity hint` makes it exit 1 whenever any finding exists. A silent run that exits 1 is the
normal, successful outcome. Do not read that exit code through a pipe — a pipe hands you the last
command's status instead.

**The build's success line is localised; the document line is not.** Grepping build output for "Build
succeeded" can find nothing on a perfectly successful build. `Writing document named '2.0'` stays
English, which is another reason it is the line to look for.

**A "top offending rules" summary in an HTML report did not come from Spectral.** The built-in HTML
formatter renders a bare "Spectral Report" heading and one collapsible group per document, nothing
else. A report with severity tiles and per-rule/per-file bar charts was rendered by
`npx @api-common/spectral-reporter <spectral -f json output> -o <file>` (Apache-2.0, no dependencies,
reads the JSON and writes HTML — it never touches the documents or the ruleset). Worth knowing in two
directions: it is what to run when somebody asks to *look at* the findings rather than close one, and
it is how to date an unfamiliar report someone shows you — a rule listed in such a summary may since
have been closed or switched `off`. It also takes `--totals <file>`, a sidecar of
`{"rules": {"<rule>": {"checked": N, "passed": N}}}`, which adds a compliance scoreboard; nothing
generates that sidecar today, but it is the natural home for the ruleset's `# measured: 0` notes,
since `checked 829 / passed 829` is evidence a check ran and held and an empty report never is.
(2026-09-01.)

**Compare the ruleset header's stated count against the report's actual TOTAL, early.** The header
carries a hand-maintained "N findings as of <date>" figure. When it disagrees with what `count.py`
says about the report in front of you, something changed since the header was last written — most
usefully, it is the cheapest early signal that a finding is a regression rather than debt. Treat it as
a hint, not an authority: the header can equally just be stale.
