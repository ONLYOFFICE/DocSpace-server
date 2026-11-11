---
paths:
  - "**/*.cs"
---

# Fixing review findings: the class, the other paths, a test

A review finding names one place. Fixing that place alone leaves the same defect in its siblings,
to be found by the next review, and a fix that is checked only against the finding can break the
paths around it. Before a fix is handed back:

1. **Fix the class.** Name the pattern the finding is an instance of and find every other instance
   in the branch — `findReferences` on the member, the same call shape in sibling services (Files /
   People / Web.Api / Web.Studio), the same config/DTO/handler family. Fix them all, or say in the
   commit message which ones were left and why.
2. **Walk the other paths of the changed code.** Create vs update (a field left out on update),
   the lookup the old behaviour served, rows and backup archives written before the change, the
   other database in the code (the PostgreSQL branch of a model builder or of raw SQL), the success
   path and the opposite failure. If the fix adds a retry, requeue or fallback, show where it ends.
3. **Prove it with a test** that fails without the fix — for every behavioural finding — and prove
   that it fails: run `.claude/scripts/prove-red.sh` (`tests.md`, "A test has to be able to fail") and
   put its result line in the commit message. A finding that cannot be tested or proven here
   (environment, timing, cost) says so in the commit message.
4. **Close the class where it can be enumerated.** When the finding's `guard` names a test that
   walks every instance (every property of the webhook DTOs is filled by some fixture, every
   out-of-controller publish is listed in the contract, every backed-up column is declared), add
   that test instead of — or beside — the one for the cited place. A class closed by a guard does
   not come back in the next review; a class fixed by hand does, from the next new instance.
5. **Fix a review in one batch.** Take all findings of one review (one sha) in one round and only
   then ask for the next review. Fixing between review runs moves the sha under the review, and
   each run then finds the bugs of the previous round's fixes mixed with what it missed before.
6. **Offer to check your own work before handing it back.** Before a branch first goes to review,
   offer a self-review with the `review-branch` skill (its §0); after a round of fixes, offer its §7
   re-check of your fix commits. On a yes, fix what it finds, then ask for the review. The reviewer
   uses the same skill, so what it finds here would otherwise cost a round; the run itself costs a
   full pass of the skill, so the user decides (`CLAUDE.md`, Git Workflow).

A fix that adds state — a mark, an "accepted" flag, a lock, a retry, a fallback — is new code and
goes through `failure-paths.md` like any other: in past rounds most new findings were in exactly
that code. The same rule covers writing new code of the kinds that are easy to get wrong: side
effects after the primary action, external calls, background loops, queues, retries and leases.
