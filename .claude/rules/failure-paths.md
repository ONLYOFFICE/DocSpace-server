---
paths:
  - "**/*.cs"
  - "common/ASC.Identity/**/*.java"
---

# Failure paths and the order of side effects

Code gets written for the success path. Most review findings are about what it leaves behind when a
step fails halfway or two requests race. Answer the questions below while writing the code, not
after the review: the answer usually changes the order of a few lines, sometimes adds a
`try/finally` or a test. This is not a call for defensive code. An exception of the primary action
still reaches the caller: never catch it just to keep going. Each section names the test that proves
it. Reviewers ask the same questions (`review-branch` lenses B and G, matrices M2, M4, M5) and cite
them as `[failure-paths.md §N]`.

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

## Before handing the change back

Walk your own diff once. For every new mark or lock, external call, effect after the action, `catch`
and loop, name the section that applies and the test that proves it, or say in the commit message
why there is none. A fix for a review finding is new code too: the marks, retries and fallbacks it
adds go through the same walk (`fixing-findings.md`).
