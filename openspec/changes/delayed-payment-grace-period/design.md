# Design

## Context

See proposal.md (Why). Facts the design builds on:

- `billing`, "Tariff state of a portal": the state is computed from the due date on every read of the tariff,
  including a read served from the cache, so a decision made in that computation is made on every read.
- The grace period never refuses anything by itself. Each refusal is a separate check of the state against
  `Paid`: the four count checkers of `quotas` ("Count limits of the tariff"), `updateWalletPayment`, the
  subscription balance helper, the two Docs Connect checks, the add-on renewal, the backup scheduler, and the
  queue lanes of the socket service, the audit trail and the thumbnail worker. The NotPaid payment gate
  (`billing`, "Payment gate while the portal is not paid") reads another helper and is left alone.
- `getCustomerInfo` (`billing`, "Billing account page and payment method") already reads `isDelayedPaymentMethod`
  from billing and keeps the answer in the distributed cache for 10 minutes. After a failure it keeps an empty
  answer for 10 minutes. For a portal that is not its customer billing answers "error: cannot find", which the
  client raises as a not-found billing error and which the cache keeps as the same empty answer.

## Goals / Non-Goals

**Goals:**

- One decision, "is the tariff restricted", made from the tariff alone, so every check reads a property and a
  unit test covers the decision with all its inputs.
- No new call to billing on a paid portal; at most one cached customer read per tariff read in the grace period.

**Non-Goals:**

- Knowing that a particular renewal charge is in flight. Billing does not report it; the payment method is the
  signal the user chose: billing gives `isDelayedPaymentMethod` true for SEPA and false for card.
- Self-hosted installations: they have no billing customer, their grace period stays restricted.
- The letters of the grace period: they keep going out as today.

## Decisions

1. **The flag lives on the tariff.** The tariff gains a `DelayedPaymentMethod` value, filled in the state
   computation only when the state is Delay on a hosted installation and reset to false otherwise (the
   object comes from the cache), and a computed `IsRestricted`: NotPaid, or Delay without a delayed method.
   Each check replaces `State > Paid` with `IsRestricted`; the subscription balance check
   (`State != Paid`) becomes "Trial or restricted"; the backup scheduler (`State < Delay`) becomes "not
   restricted". The queue lanes take a computed lane state: Paid for an unrestricted grace period, the state
   otherwise; the three producers (the socket client, the audit handler, the thumbnail handler) and the tariff
   change notification pass it.
   *Alternative:* a separate service asked by every check. Rejected: each check would read the customer again,
   and the client-facing `restricted` would be computed in another place than the checks.
   *Alternative:* a fifth tariff state. Rejected: `state` Delay is what the client must keep showing, and every
   existing comparison of states would change meaning.

2. **The last known answer is written on an answer only.** Every answer of billing to the customer request, a
   customer or "cannot find", writes `isDelayedPaymentMethod` (false for no customer) to its own key of the
   distributed cache, kept for the grace days plus one so that it outlives any grace period. A failure
   (a timeout, another error, an unreadable body) writes nothing there. The state computation asks for the
   customer as `getCustomerInfo` does (cached, 10 minutes)
   and then reads that key; an absent key means restricted. So a failure keeps the last answer, a portal that
   stopped being a customer is restricted, and the empty answer kept after a failure needs no new meaning. The
   key is in the distributed cache because one process may read billing and another decide; it is not removed
   when the tariff cache is cleared.
   *Alternative:* the tariff row (`comment` column). Rejected: a row is written only when the due date or the
   quotas change, and the value is a cache of billing, not a record.

3. **A changed answer is announced without reading the tariff.** When the new answer differs from the stored
   one (an absent value counts as false), `change-tariff` is sent to the portal's quota room, best effort, after
   the key is written. The tariff is not read at that point: the customer read can run inside the tariff
   computation, which holds the tariff lock, and a tariff read there would wait on it. The event is sent in
   the free lane, since the state is not known there.

4. **`restricted` is part of the tariff response for every member**, next to `state`, filled from
   `IsRestricted`. The other fields keep their role grading.

## Risks / Trade-offs

- [A customer who pays by SEPA and does not pay keeps full functionality for the whole grace period] → it is
  what the user asked for; NotPaid ends it on the same day as for any other portal.
- [A customer read inside the tariff computation adds a billing call on a cache miss, once per 10 minutes per
  portal in the grace period] → bounded by the existing customer cache and its lock; paid portals never ask.
- [A switch of the payment method takes effect within 10 minutes] → `getCustomerInfo` with `refresh=true`
  applies it at once; stated in the spec.
- [The web client disables controls from `state` Delay until it reads `restricted`] → the server goes first;
  the PR description names the field and the event for the frontend.

## Migration Plan

No stored data changes. Existing portals in the grace period become unrestricted on the first tariff read after
the deployment if billing reports a delayed method. Rollback restores the old checks; the extra cache key expires
by itself.

## Consistency with the requirements that change

Every requirement listed under "Breaks current behaviour" keeps its meaning for a portal without a delayed
payment method: `IsRestricted` is exactly `State > Paid` there. Portals with one move from the refusal to the
paid behaviour, which the deltas state per requirement.
