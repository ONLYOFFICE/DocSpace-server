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
   other database, the success path and the opposite failure. If the fix adds a retry, requeue or
   fallback, show where it ends.
3. **Prove it with a test** that fails without the fix — for every behavioural finding. A finding
   that cannot be tested here (environment, timing) says so in the commit message.
4. **Close the class where it can be enumerated.** When the finding's `guard` names a test that
   walks every instance (every property of the webhook DTOs is filled by some fixture, every
   out-of-controller publish is listed in the contract, every backed-up column is declared), add
   that test instead of — or beside — the one for the cited place. A class closed by a guard does
   not come back in the next review; a class fixed by hand does, from the next new instance.
5. **Fix a review in one batch.** Take all findings of one review (one sha) in one round and only
   then ask for the next review. Fixing between review runs moves the sha under the review, and
   each run then finds the bugs of the previous round's fixes mixed with what it missed before.
6. **Re-check your own fix commits** with the `review-branch` skill, §7 (re-check mode), before
   asking for another review round.

The same questions apply when writing new code of the kinds that are easy to get wrong: background loops,
queues, retries and leases (`review-branch` §3 G), and side effects after the primary action
(`review-branch` §3 B).
