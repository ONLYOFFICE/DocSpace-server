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
  answer for 10 minutes. For a portal that is not its customer billing answers "error: cannot find" with 200 OK
  (as the client's comment and the test stub say; not checked against the billing service), which the client
  raises as a not-found billing error and which the cache keeps as the same empty answer.

## Goals / Non-Goals

**Goals:**

- One decision, "is the tariff restricted", made from the tariff alone, so every check reads a property and a
  unit test covers the decision with all its inputs.
- No new call to billing on a paid portal, and no wait for billing on a read of a cached tariff: only a read that
  already asks billing (a missed or refreshed tariff read with a request to the payment system) asks for the customer, and only in the grace period.

**Non-Goals:**

- Knowing that a particular renewal charge is in flight. Billing does not report it; the payment method is the
  signal the user chose: billing gives `isDelayedPaymentMethod` true for SEPA and false for card.
- Self-hosted installations: they have no billing customer, their grace period stays restricted.
- The letters of the grace period: they keep going out as today.

## Decisions

1. **The flag lives on the tariff.** The tariff gains a `DelayedPaymentMethod` value, worked out in the state
   computation, true only when the state is Delay on a hosted installation and the customer pays by a delayed
   method, and a computed `IsRestricted`: NotPaid, or Delay without a delayed method. The in-memory cache shares
   the tariff object between requests, so the value is written once, after the customer is known; the state is
   still assigned step by step on that object, as before the change.
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
   distributed cache, kept for the grace days plus one after the answer. While billing answers, every customer
   read in the grace period renews it; an answer older than that no longer decides, so a portal whose billing
   stopped answering before its grace period started can be restricted before the grace period ends. A failure
   (a timeout, another error, an unreadable body) writes nothing there. The state computation decides by the
   customer known now: the one billing gives on a read that already asks billing (a missed tariff cache with a
   request to the payment system, refreshed or not; a calculation after a saved change of the tariff reads it
   from billing too, as the save drops the cached customer), otherwise the one cached by `getCustomerInfo`
   (10 minutes).
   Without a customer it reads that key; an absent key means restricted. So a failure keeps the last answer, a
   portal that stopped being a customer is restricted, a read of a cached tariff never waits for billing or the
   customer lock, and the empty answer kept after a failure needs no new meaning. The
   key is in the distributed cache because one process may read billing and another decide; it is not removed
   when the tariff cache is cleared.
   *Alternative:* the tariff row (`comment` column). Rejected: a row is written only when the due date or the
   quotas change, and the value is a cache of billing, not a record.

3. **A changed answer is announced without reading the tariff.** When the new answer differs from the stored
   one (an absent value counts as false), `change-tariff` is sent to the portal's quota room, best effort, after
   the key is written. The tariff is not read at that point: the customer read can run inside the tariff
   computation, which holds the tariff lock, and a tariff read there would wait on it. The event is sent in
   the free lane, since the state is not known there. When the stored answer cannot be read, the event is sent
   once the new answer is kept: an extra nudge costs the clients one tariff read, a missed one leaves them with a
   stale tariff. The read asks FusionCache to report a failure of the distributed level, which it otherwise takes
   for a missing value, that is for a card.

4. **`restricted` is part of the tariff response for every member**, next to `state`, filled from
   `IsRestricted`. The other fields keep their role grading.

5. **A "cannot find" under a 5xx, 408 or 429 is an unknown outcome.** The client raises the not-found error only
   for a "cannot find" under 2xx or another 4xx; under these statuses it is an ordinary billing failure, so it
   writes no last answer. The rule is in the client, so every caller follows it: a payments read whose "cannot
   find" comes with such a status no longer moves a paid tariff to yesterday ("Tariff of a hosted portal follows
   the billing service"), and `getDepositInvoice` answers 402 instead of 404 under such a status.

## Risks / Trade-offs

- [A customer who pays by SEPA and does not pay keeps full functionality for the whole grace period] → it is
  what the user asked for; NotPaid ends it on the same day as for any other portal.
- [A read that already asks billing now also asks for the customer in the grace period, under the tariff lock:
  once per 10 minutes per portal, on every call with `refresh`, and after a saved change of the tariff] → it adds to a billing call that read makes
  anyway, so readers waiting for the lock wait for both calls when billing is slow; reads of a cached tariff, the
  payment gate among them, never ask; paid portals never ask.
- [A switch of the payment method takes effect only once the customer is read again after its 10-minute cache] →
  every tariff read, a cached one included, works the state out again from the cached customer, so the first call
  that reads the customer after the cache expired applies the switch: opening a document, an AI call, the payment
  pages, `getCustomerInfo`, or a tariff read that asks billing in the grace period. The payment gate of every
  request reads the tariff without asking billing (`IsNotPaidAsync(false)`), so on a portal where no call reads the
  customer, the answer is not renewed, and one paying by SEPA with no remembered answer stays restricted until a
  call does. `getCustomerInfo` or `getPortalTariff` with `refresh=true` applies it at once; stated in the spec.
  Asking billing on the gate's misses was declined by decision, to keep billing off the payment gate. When the last answer cannot be written, no `change-tariff` is
  queued, and the event itself is best-effort.
- [The last answer expires the grace days plus one after it was given] → billing silent since before the grace
  period started restricts the portal once it expires, as with no answer at all; stated in the spec. No
  `change-tariff` is sent for the expiry: clients see it on their next tariff read.
- [The distributed cache does not answer] → the read of the last answer gives none, so the portal is restricted
  while it fails; stated in the spec.
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
