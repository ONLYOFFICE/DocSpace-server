---
name: fix-bug
description: "Reproduce and fix a DocSpace bug by its tracker number, test-first: find or port the covering test, prove it is red, fix the product, prove it is green. USE FOR: fix bug 12345, check bug 12345, is bug 12345 still reproducible, this bug came back. DO NOT USE FOR: writing new feature tests (tests rule), running a test suite without a bug number (run-tests skill)."
---

# Fix a bug, test first

Applies when the request names a bug number. The point is that nothing is touched in the
product until a test proves the bug is real, and the same test proves it is gone.

Never edit product code before step 3 has produced a red test.

## 1. Find the covering .NET test

Bug numbers live in traits, so the trait is the index:

```bash
grep -rn '\[Trait("Bug", "12345")\]' --include=*.cs products/*/Tests common/Tests
```

A hit means the bug is already covered — go to step 3.

## 2. No .NET test? Port the TypeScript one

The TypeScript suite is the older, wider set; most bugs are documented there first. Search
`../tests/api-tests/src/tests/` for the number — open bugs are marked `test.fail(...)` and
carry `BUG 12345` in the test name:

```bash
grep -rn "BUG 12345" ../tests/api-tests/src/tests/
```

Grep is right here, and not for lack of a language server: the number lives inside a string
literal (the test title), so it is not a symbol and `workspaceSymbol` would return nothing —
the same exception `csharp-lsp.md` carves out for literals and comments. For navigating the
SDK while translating, read the generated client from `sdk/docspace-api-sdk-typescript`.

Port it following `.claude/rules/tests.md` — feature subfolder, matching access matrices, no
raw HTTP where a typed DTO works — and mark it `[Trait("Bug", "12345")]`. Assert the behaviour
the product **should** have, never the buggy one: that is what makes the test flip from red to
green on its own when the fix lands.

If the number appears in neither suite, stop and ask what the bug actually is. Do not guess a
reproduction from the number alone.

## 3. Prove it is red

```bash
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj -- --filter-trait "Bug=12345"
```

Read the failure and check it is the bug being described, not a broken Arrange (a wrong access
level in an invitation fails the same way — see the access matrix section of the tests rule).

**If the test passes, stop here.** The bug is either already fixed or not reproducible as
written. Report which one it looks like and leave the product alone — this is the whole
purpose of the step, and "fix it anyway" is how phantom changes get made.

## 4. Fix the product

Navigate with LSP (`.claude/rules/csharp-lsp.md`), not grep. Prefer the narrowest change that
addresses the cause; when validation is involved, check whether neighbouring DTOs of the same
feature carry the same rule, and say so if they diverge rather than silently aligning them.

Follow `.claude/rules/csharp-style.md`, including the mandatory `dotnet format style` check.

## 5. Prove it is green

Run the bug's own test plus everything around it, because a validation fix in particular tends
to move neighbouring cases:

```bash
# the bug itself
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj -- --filter-trait "Bug=12345"

# its class, then its whole feature folder
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj -- --filter-class "*RoomCoverValidationTests"
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj -- --filter-namespace "*_03_Rooms.Covers*"
```

**Keep the `[Trait("Bug", ...)]`.** It is the permanent link to the bug record, so a regression
points straight back at the number. Update only the `<summary>`: describe the old behaviour in
the past tense and how it was fixed.

## Build notes

A locally running DocSpace locks the shared DLLs, which breaks a full build. Build narrowly:

```bash
dotnet build products/ASC.Files/Server/ASC.Files.csproj --no-dependencies
dotnet build products/ASC.Files/Tests/ASC.Files.Tests.csproj -p:BuildProjectReferences=false
```

For everything about running tests — MTP filter syntax, Aspire fixture, where the logs are,
telling a regression from a flake — use the `run-tests` skill.
