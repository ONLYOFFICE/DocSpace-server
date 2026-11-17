# Texts that state behaviour: every claim has a source

A `<remarks>`, a comment, a spec requirement or scenario, a proposal, a task list, a PR description
and a commit message all state what the code does. Reviews keep finding these texts wrong more often
than any single class of code defect: in two branches reviewed in November 2026, 8 of 20 and 6 of 19
findings were texts that did not match the code, most of them promising more than it did. The texts
were written from the design, and later fix rounds changed the code under them. This rule has no
`paths:`: commit messages and PR descriptions belong to every branch, whatever files it changes. Reviewers check the same points (`review-branch` §3 B
"claims the branch writes", §3 S) and cite them as `[claims.md §N]`.

## 1. A claim points at the code or the test that makes it true

- A number (a duration, a count, a limit, a date such as "yesterday"), an absolute word ("never",
  "always", "only", "at most", "exactly once", "no longer", `SHALL`) and a promised outcome ("the
  wizard falls back to the installation's own trial") each have a source: the constant, the line
  that decides it, or the test that asserts it. Look the source up before writing the sentence; a
  number is plain text, so search the code for it.
- The value that applies is the one the code really hits, including a dependency's own limit.
  Seen in review: a comment promised a 10 s timeout around a command that gave up after 5 s on its
  own; a spec delta said an answer "MAY be up to 15 minutes old" while a fail-safe copy kept it for
  an hour.
- A claim that holds only outside a window names the window in the same sentence: until the first
  background pass, while the dependency answers, until a cache entry expires, before a restart.
  Seen in review: a requirement said a value "SHALL" be applied when the setup wizard completes,
  while the change's own scenario described the wizard completing without it when the dependency
  did not answer.

## 2. A spec scenario or a documented value is the value a test asserts

- Write the value of a scenario (the end date, the status code, the count) from the test that pins
  the code, or write the test first. When the intended text and the code disagree, decide which one
  moves before handing the branch over; never leave both.
- Seen in review: a delta said an expired external trial ends the installation's trial "yesterday";
  the code took the earlier of that trial's end and yesterday, and the unit test pinned the code,
  not the delta.

## 3. Every fix round re-reads the texts it reaches

- A fix changes behaviour that texts written in earlier rounds describe. After each round, list the
  texts the branch wrote — remarks of the actions the change reaches, comments, spec deltas,
  `proposal.md`, `design.md`, `tasks.md`, the `verification.md` rows, the PR description, the claims
  of earlier commit messages — and re-check the ones the round's behaviour reaches, in the same round.
- Seen in review: an endpoint's remarks kept a sentence from before the change ("the call does not
  ask the external service") after the change made the call ask it; a PR description kept a promise
  ("after that the usual failure letter is sent") that a later fix had made untrue for one case.

## 4. Status texts say exactly what was done

- A task is ticked only when it is done. A task that is blocked stays unticked with the reason, or
  moves to a follow-up list: the archive step warns only about unticked tasks, so a ticked blocked
  one is archived as done.
- A commit message says "updated" only for what it updated. "Line references updated" covers all of
  them, or names which ones; seen in review, three citations in a `verification.md` were still stale.
- A `verification.md` row cites lines of a file: when the branch moves those lines, the row is
  updated in the same branch.

## 5. What the scan gives

`review-branch/scan.py` helps with the mechanical part; each hit is a sentence to check against its
source, not a finding by itself, and a claim worded otherwise is still the writer's to find:

- "Claims with numbers": numbers with units, worded durations ("an hour", "two minutes"), limits
  ("at most 100") and date words in the branch's commit messages, its spec texts and its C# comments
  (whole-line ones and a trailing `//` or `/* */`); the first 200 lines and a count, all with `--full`;
- in the OpenSpec section: the requirements the branch adds or changes at 500 characters or more
  (compared by name with the same requirement at the merge-base; a delta, active or archived, with
  its capability's spec), ticked tasks that say they are
  blocked or not done (untracked files included with `--worktree`), and the `verification.md` rows
  that cite a changed file by its path, an alias, its file name (marked when several files share it)
  or a folder alias; the first 100 such entries and 8 line numbers each, all with `--full`.
