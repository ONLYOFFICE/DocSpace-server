---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.props"
  - ".editorconfig"
---

# Build warnings are not optional

A warning in a file you touched is unfinished work. The API projects build with **zero** warnings
today, so "no new warnings" is a realistic bar, not an aspiration.

## See them

After editing `.cs` files, build every project you touched:

```bash
dotnet build <csproj> --no-dependencies --no-incremental
```

`--no-incremental` matters: a build that skips `CoreCompile` prints no warnings at all, so a
"clean" incremental build proves nothing. A build that cannot run (`MSB3027`, DLLs locked by a
running DocSpace) is reported as "not built" — never as clean.

## Documentation warnings

The repo's own analyzer, `common/ASC.Analyzers` (`ApiControllerXmlDocumentationAnalyzer`), reports
missing API documentation as warnings:

| Id | Fires when |
|---|---|
| API002 | a public `[Http*]` action has no XML doc |
| API003 | the action has no `<summary>` |
| API004 | the action has no `<remarks>` |
| API005 | a property of a DTO used as a parameter or a result has no `<summary>` |
| API006 | that property (a scalar, enum or system collection) has no `<example>` |

Plus the compiler's `CS1570` (malformed XML doc), `CS1572` / `CS1573` (`<param>` that does not match,
or is missing for, a parameter). `CS1591` is switched off repo-wide — do not add docs just to satisfy
it outside the API types.

These warnings only say the text must **exist**. What it must say is decided by
`openapi-endpoint-docs.md` (the operation) and `openapi-dto-docs.md` (properties) — a placeholder
written to silence API006 is a violation of those rules.

The analyzer is attached only to `ASC.Files`, `ASC.People`, `ASC.AI`, `ASC.Web.Api`, `ASC.Data.Backup`
and `ASC.ApiSystem`. A DTO declared in a `*.Core` project is checked only when the controller project
that uses it is built, and the warning then points at the **action method**, not at the DTO. After
changing such a DTO, build the consuming controller project too.

## Never silence

Forbidden: `NoWarn` additions, `#pragma warning disable`, `[SuppressMessage]`, or lowering a severity
in `.editorconfig` to make a warning go away. The single exception is an analyzer that is wrong for
the whole codebase: then the `.editorconfig` change carries a comment saying why, as the existing
entries for `CA2000` and `AV0014` do.

## Style warnings

`IDE*` warnings (visible with `-p:EnforceCodeStyleInBuild=true`, never committed) follow
`csharp-style.md` → Style Verification. That mode has a pre-existing baseline (e.g. `IDE0060` in
`ASC.Files.Core`): fix what is in the code you changed, leave the rest alone.
