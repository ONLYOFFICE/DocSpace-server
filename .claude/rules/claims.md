# Texts that state behaviour: every claim has a source

A `<remarks>`, a comment, a spec requirement or scenario, a proposal, a task list, a PR description
and a commit message all state what the code does. Reviews keep finding these texts wrong more often
than any single class of code defect: in two branches reviewed in November 2026, 8 of 20 and 6 of 19
findings were texts that did not match the code, most of them promising more than it did. The texts
were written from the design, and later fix rounds changed the code under them. This rule has no
`paths:`: commit messages and PR descriptions belong to every branch, whatever files it changes. Reviewers check the same points (`review-branch` §3 B
"claims the branch writes", §3 S) and cite them as `[claims.md §N]`.

## Checklist

- Every number, absolute word ("never", `SHALL`) and promised outcome has its constant, line or test (§1)
- A number is the value the code really hits, including a dependency's own limit or a fail-safe copy (§1)
- A claim true only within a window (until a pass, while a dependency answers) names it in the sentence (§1)
- A scenario's value (date, status, count) is the one a test asserts; text and code do not disagree (§2)
- After each fix round, every text it reaches (remarks, comments, specs, tasks, PR, commits) is re-checked (§3)
- A task is ticked only when done; a blocked one stays unticked with its reason or moves to a follow-up (§4)
- A commit message says "updated" only for what it updated, and is checked before it is pushed (§4)
- A `verification.md` row cites by name, `File.cs::Method`; a row the branch touches is rewritten so (§4)
- A number or absolute word a fix round writes names its test (one-step mutation) or CODE-ONLY; a library's behaviour, its characterization test (§6)

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
- A `verification.md` row cites a test or a code member by name — `File.cs::Method`, not
  `File.cs:120-140`. Line citations moved with every edit above them: one review found 121 of a spec's
  378 stale, and stale citations were 8 findings of ten reviews. A row the branch touches is rewritten
  to the name form; one that still cites lines of a file the branch moves is updated in the same branch.
  The scan and `apply_run_marks.py` read a further name after a comma only when another name, a
  backtick, `;`, `)`, `(`, `:`, `|`, a full stop, "and", "or" or the end of the row follows it: write
  `File.cs::A, B` in backticks when prose follows the list.
- A commit message is checked before it is pushed: once pushed it cannot change, so a review that
  finds it wrong can only ask for the PR description to say it right. The scan lists the claims of the
  branch's commit messages ("Claims with numbers").

## 5. What the scan gives

`review-branch/scan.py` helps with the mechanical part; each hit is a sentence to check against its
source, not a finding by itself, and a claim worded otherwise is still the writer's to find:

- "Claims with numbers": numbers with units, worded durations ("an hour", "two minutes"), limits
  ("at most 100") and date words in the branch's commit messages, its spec texts and its C# comments
  (whole-line ones and a trailing `//` or `/* */`); the first 200 lines and a count, all with `--full`;
- in the OpenSpec section: the requirements the branch adds at 500 characters or more, brings to 500 or
  more, or makes longer while over (compared with the bodies the requirement had at the merge-base
  under its name or its RENAMED old name, in the same file, its old path and, for a delta, the
  capability's spec; a requirement moved to another spec file counts as new), ticked tasks that say
  they are blocked or not done (untracked files included with
  `--worktree`), and the `verification.md` rows that cite a changed file by its path, an alias, its
  file name (marked when several files share it) or a folder alias — by line; by a member name, as
  stale when the file's code no longer has the name, and as a row to re-read while it has. Only that
  last list is capped: its first 100 entries and 8 line numbers each, all with `--full`;
- "Texts the diff writes or reaches": the comments and doc comments the diff writes in `.cs`, `.ts`,
  `.py` and `.sh` files (outside `sdk/` and `migrations/`; generated files and a new file's license
  header left out), the doc comments of the actions whose lines it changes, the Markdown files it
  writes outside `sdk/` and `openspec/config.yaml`, with their added line counts, and which of the branch's commits are not pushed yet — the list §3 has the
  author re-read after a fix round (`fixing-findings.md` §6). Each list shows its first 60 entries,
  all with `--full`; a text whose behaviour changed outside the diff is still the writer's to find.
- "verification.md line citations": each cited range of a changed C# file checked for where it starts
  and ends and for the backticked names after it, and the cited blocks of untouched rows that moved;
  its `!!` lines fail `--gate` (`fixing-findings.md` §12).

## 6. A claim the fix writes is pinned in the same batch

- A number or an absolute word that a fix round adds to a remark, a comment, a spec, a
  `verification.md` row or a commit message names, in the same batch, the test that pins it, or is
  marked CODE-ONLY in `verification.md` (in the commit message when no row covers it). A statement
  about how a dependency behaves names its characterization test (`dependency-behaviour.md` §1);
  CODE-ONLY does not stand for it. Reasoning about the code is not a source: the claims below were all
  reasoned.
- A test pins a value only if a mutation of that value by one step makes it red: a threshold of 5
  changed to 4 and to 6, a duration a unit shorter and longer (`prove-red.sh --mutate`, `tests.md`
  "A test has to be able to fail"). A test that stays green under a mutation pins a range; the text
  then states the range, not the value.
- Seen in review, in one branch, each written by a fix round to answer a finding and corrected by the next:
  "up to 4 requests" where a failed creation sent 6; "5 or more requests failed" written as a state,
  although the breaker opens only on a failed request after them; "an error status is not counted",
  although Polly records an outcome its predicate does not handle as a success.
- Guard: the scan's "Claims with numbers" of the round's commits, each line with its test and
  mutation or CODE-ONLY (`fixing-findings.md` §9); reviewers build matrix M12 (`review-branch` §3 M).
