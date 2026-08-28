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
sites, at lines 262 and 278 (verified 2026-08-28). Line 283 carries a load-bearing comment: the
`XmlCommentsMemberDescriptionSchemaFilter` on the next line must stay after every `IncludeXmlComments`
call. An `info`-level fix, an envelope fix and an XML-doc fix therefore all meet in this one file, so
read it before reordering anything in it.

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

**Spectral prints nothing when both output flags are set.** With `--output.html` and
`--output.markdown` given, a successful run writes the two files and says nothing at all, while
`--fail-severity hint` makes it exit 1 whenever any finding exists. A silent run that exits 1 is the
normal, successful outcome. Do not read that exit code through a pipe — a pipe hands you the last
command's status instead.

**The build's success line is localised; the document line is not.** Grepping build output for "Build
succeeded" can find nothing on a perfectly successful build. `Writing document named '2.0'` stays
English, which is another reason it is the line to look for.

**Compare the ruleset header's stated count against the report's actual TOTAL, early.** The header
carries a hand-maintained "N findings as of <date>" figure. When it disagrees with what `count.py`
says about the report in front of you, something changed since the header was last written — most
usefully, it is the cheapest early signal that a finding is a regression rather than debt. Treat it as
a hint, not an authority: the header can equally just be stale.
