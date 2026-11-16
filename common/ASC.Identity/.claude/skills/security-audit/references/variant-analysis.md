# Variant Analysis

Find the other instances of a bug you have already found. One root cause usually has several
manifestations, and they are rarely in the module where you found the first one.

Adapted from [Trail of Bits variant-analysis](https://github.com/trailofbits/skills/tree/main/plugins/variant-analysis).

## When to Use

- A vulnerability has been found and you need to search for similar instances
- Building or refining CodeQL/Semgrep queries for security patterns
- Performing systematic code audits after an initial issue discovery
- Analyzing how a single root cause manifests in different code paths

## When NOT to Use

- Initial vulnerability discovery — use the main security-audit workflow first
- General code review with no known pattern to search for
- Understanding unfamiliar code before any finding exists

## The Five Steps

**1. Understand the original issue.** Extract the root cause — why the code is wrong, not
what it does — and enumerate the directions a variant could hide in: related identifiers,
other manifestations of the same mistake, data-type edge cases.

**2. Create an exact match.** Write a pattern matching ONLY the known instance and confirm
it hits. A pattern that matches nothing means you have misunderstood the bug.

**3–4. Generalize one element at a time.** Climb from the exact match toward the pattern
family, running and reading all matches after each single change. Stop when more than half
the matches are noise.

**5. Triage.** Decide which candidates are real, with severity attached.

**Then write it up**, including patterns that failed and a regression guard (test and/or Semgrep).
→ [reporting.md](reporting.md)

## What Makes Hunts Fail

1. **Narrow scope** — searching only the module the original bug was in
2. **Pattern too specific** — searching one attribute and missing the family around it
3. **One vulnerability class** — chasing a single manifestation of the root cause
4. **Happy-path testing** — never trying the null, empty, and boundary cases
5. **Generalizing too fast** — abstracting several elements at once

## Resources

Upstream CodeQL/Semgrep templates:
https://github.com/trailofbits/skills/tree/main/plugins/variant-analysis
