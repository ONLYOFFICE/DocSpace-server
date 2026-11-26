---
paths:
  - "**/*.cs"
  - "common/ASC.Identity/**/*.java"
---

# Failure paths and the order of side effects

Code gets written for the success path. Most review findings are about what it leaves behind when a
step fails halfway, two requests race, or time passes. Answer the questions below while writing the code, not
after the review: the answer usually changes the order of a few lines, sometimes adds a
`try/finally` or a test. This is not a call for defensive code. An exception of the primary action
still reaches the caller: never catch it just to keep going. Each section names the test that proves
it. Reviewers ask the same questions (`review-branch` lenses B and G, matrices M2, M4, M5, M9, M10)
and cite them as `[failure-paths.md §N]`.

## Checklist

- Checks and rights run before any write; audit rows and "sent" marks come after the action took effect (§1)
- A mark, lock or reserved quota taken before work that can fail is released on every failure (§2)
- Each best-effort effect (event, socket, webhook, index) has its own try/catch, placed after the cleanup (§3)
- A timeout, 5xx or unreadable body is unknown, never a refusal; a 4xx about the caller's input maps to ours (§4)
- Check-then-act is an upsert, conditional write or lock; an irreversible step comes after its record (§5, §6)
- No `catch {}` or catch inside a transaction the callee joins; retries end; a lease outlasts its work (§7, §8)
- Every decision on a state that ends (trial, lease, token) compares the end with now (§9)
- A multi-step operation has a table of step x interruption, re-checked against each fix (§10)
- A decision the stand cannot reach is a pure function with a unit test at its boundaries (§11)

## 1. Order: check → action → record → best-effort

- Repeat checks, rights and limits come before anything that writes: a link, a short link, an audit
  row, a spent invitation, a user update.
- The audit record, the spent limit and the "sent" mark come after the action took effect. If one
  has to come first, its text says "requested", not "sent".
- Seen in review: the repeat check ran after the confirmation link was built, so every call added a
  `short_links` row and an audit record although the letter itself was suppressed.
- Guard: call twice and assert that the second call added no rows. Asserting only that no exception
  was thrown proves nothing.

## 2. A mark taken before work that can fail is released on every failure

- This covers a deduplication mark, an "accepted" flag, a distributed lock, a reserved quota, and a
  cache key written to block a repeat.
- Hold it in a scope that releases it unless the work completes: `await using` + `Complete()`, or
  `try/finally`. The release removes only the caller's own value (compare a token), in one atomic
  step, never a value another request wrote in the meantime.
- Seen in review: the "accepted" mark was written, then shortening the link threw a 500, and the
  client's retry within a minute got 200 "queued" for a letter that never existed.
- Guard: make the step after the mark throw, then check that the next identical request goes through.

## 3. A best-effort effect cannot break the primary action

- An event bus publish, a socket event, a webhook, an index update or a notification after the
  action took effect gets its own `try/catch`. The catch logs a warning through `[LoggerMessage]`
  (`logging.md`) and continues. Put the effect after the cleanup the primary path has to do, never
  inside the `try` that guards it.
- Build the payload from the entry in the state it describes, after the change.
- Seen in review: a RabbitMQ failure on an event published inside a form submission skipped the XLSX
  report update and the draft cleanup.
- Guard: a publisher that throws; the primary result and its cleanup are still in place.

## 4. An external call has three outcomes: accepted, refused, unknown

- A timeout, a dropped connection, a 5xx or a body that does not parse is **unknown**. Never map
  unknown to refused.
- On a path whose effect a retry cannot undo (a deposit, a renewal, a payment), unknown goes to a
  deferred check. It never triggers a repeat of the call, and never a "do it by hand" letter.
- A 4xx from the dependency about the caller's input becomes our documented status (400, 404), not a
  500 from `EnsureSuccessStatusCode`.
- When the response needs only a small part of the dependency (display names), an outage of the
  dependency degrades that part (null) instead of failing the whole response.
- Seen in review: `result == "\"ok\""` turned an HTML page from a proxy into a refusal, and the
  guard against a second deposit never ran.
- Guard: a stub that answers with an unreadable 200, a timeout and a 4xx, one assertion per outcome.

## 5. Check-then-act is a race

- When code reads and then `Add`s, gets and then sets, or checks existence and then creates, two
  concurrent requests both pass the check. Use an upsert, a conditional write ("set if absent", a
  version check), a distributed lock around both steps, or catch the duplicate key and continue as
  an update.
- Seen in review: a double click sent two PUTs, and the second failed on the duplicate primary key
  with a 500 although the key had been saved.
- Guard: two calls through `Task.WhenAll` both succeed and leave one row.

## 6. An irreversible step comes after the record of it

- For DDL (MySQL commits an `ALTER` at once), a call to an external service or a delete, write the
  state that describes it first or in the same transaction. A failure between the two must leave the
  stored state in line with what actually happened.
- Work due after a commit (a cleanup message, a cache eviction for other nodes) is written in the
  same transaction (outbox), or the comment states the gap. Work kept only in memory between the
  commit and the send is lost on a crash.
- Seen in review: an `ALTER` renamed columns, then the version link was saved under a lock that
  could fail, and answers ended up in two columns.
- Guard: make the second step throw; the stored state still describes the first.

## 7. catch blocks

- Log the exception, rethrow it (`throw;`) or drop it with a comment that says why. Never `catch {}`.
  A second fault inside the catch (cleanup, logging) must not replace the first.
- Never catch inside a transaction that the called code joins: the transaction is already marked
  rollback-only and the commit throws anyway. Put the `try` outside the transaction, or the
  transaction inside the `try`.
- Log the progress of a loop whose steps commit one by one (rows deleted, items sent) in `finally`.
  An exception or a cancellation in batch 31 must not hide the 30 batches already committed.

## 8. Loops, retries, leases

- Every retry, requeue and re-schedule ends, by a count, an age window or a terminal state.
- A lease is longer than the work it covers. Stop a batch at a deadline shorter than the lease.
- Two instances run the same loop at the same moment, so claim rows atomically. Details:
  `review-branch` lens G.

## 9. A state that ends is checked by its end

- A trial, a lease, a grace period, a token, a quota or a tariff "in force" has an end. Every place
  that decides on such a state — "is it in force", "is a file still required", "may the wizard
  complete" — checks the end against now, not only the kind of the state.
- Find those places by the date, not by the state's name: `findReferences` on the end property
  (`DueDate`, `ExpiresAt`), and every writer that can move the end into the past (a background
  re-read that shortens it). Each decision is checked at three points: ends later, ends today, ended
  yesterday.
- Seen in review: a setup wizard completed on an installation's own trial after that trial had
  ended. Three places decided on the trial without comparing its end with today; the re-check found
  one of them, the next review all three.
- Guard: a unit test of the decision with the end yesterday (§11).

## 10. A multi-step operation: one table, every step

- An operation of several steps that cannot all be undone — money moved, an external call, a claim
  held across them — gets a table before the code and before each fix of it: one row per step, one
  column per interruption — the step throws, the answer is unknown (§4), the process stops right
  after the step, another instance takes the item over (the lease ran out, §8). Each cell names what
  is on record at that moment and what the next run does with it.
- A fix of one cell is checked against its whole row and the row of the next step. A fix that keeps
  a claim, adds a fallback or moves a record changes other cells.
- Seen in review: a subscription renewal paid from a wallet went through five re-checks, each
  round's fix of one cell opening the next. The claim on the renewal was released although its
  pending record was not written; keeping the claim instead let another instance resume it a minute
  after the payment call timed out, with a second payment possible; the claim's age limit covered
  the payment call but not the top-up loop before it.
- Guard: one test per cell the stand can reach (a stub that times out, a second call with the same
  claim). A cell it cannot reach gets a unit test of the decision behind it, moved where a unit test
  reaches it (§11); only the wiring left after that goes into the commit message as "not proven".

## 11. A decision the stand cannot reach is still tested

This section lives here, not in `tests.md`, because the decision is written in product code, where
`tests.md` does not load: reviews found two such decisions untested, in a Web.Studio service and in
the AppHost configuration.

"Not proven red: needs an Enterprise stack" is where later reviews kept finding the bugs: in one
branch three fix commits wrote it, and the next reviews found the decisions behind it wrong (a
wizard's fallback, an expired-trial check). A stand that cannot reach the path does not excuse the
decision from a test:

- Move the decision into a function of its inputs — the quota, the tariff end, the last answer
  known, now — with no I/O, and call it where the decision was. The unit-test projects are
  `common/Tests/ASC.Core.Common.Tests`, `common/Tests/ASC.Data.Backup.Core.Tests` and
  `common/Tests/ASC.Site.Core.Tests` (the Site.Core host only, which no AppHost profile starts); the
  product and Web suites are Aspire integration suites. So the function goes into a library a unit-test
  project references (`ASC.Core.Common`, `ASC.Files.Core` or `ASC.Api.Core` for `ASC.Core.Common.Tests`),
  also when the decision is made in Web.Api or Web.Studio, which no unit-test project reaches. No
  unit-test project references the AppHost (`common/ASC.AppHost`): a decision of its configuration is
  kept a function of its inputs there, and the commit names the missing unit test as "not proven".
  The integration test stays for the wiring; the unit test covers the decision with its boundaries:
  the end yesterday, today and later (§9), an unknown answer next to a refusal (§4), each cell of a
  multi-step table the stand cannot reach (§10).
- Prove that unit test red with `prove-red.sh` like any other.
- "Not proven" is left for what remains after that — the wiring itself — and the commit names it.

## Before handing the change back

Walk your own diff once. For every new mark or lock, external call, effect after the action, `catch`,
loop, state that ends (§9) and multi-step operation (§10), name the section that applies and the
test that proves it, or say in the commit message why there is none — for a path the stand cannot
reach, after the decision on it has a unit test (§11). A fix for a review finding is new code
too: the marks, retries and fallbacks it adds go through the same walk (`fixing-findings.md`).
