# Design

## Context

See proposal.md for the motivation. What shapes the approach:

- The server does not compute a subscription period. The tariff takes each payment's end date from the
  billing service (`billing`, "Tariff of a hosted portal follows the billing service"), so "a year" is a
  property of the product in the payment system; on the server the yearly plan differs by its `year` feature
  and its price only.
- A payment is matched to a quota by product id, with a lookup that fails on two matches, and the usage
  report keys wallet quotas by service name. So the new row needs a product id and a service name no other
  row has.
- The price of a wallet quota is asked from the accounting service by service name; an unknown name answers
  an empty price list, and the seeded price is used (`billing`, "Prices are resolved per region and cached").
- The rules of `adminwallet` are tied to one enum member in two places of the payments controller, and the
  usage filter accepts only members of the wallet service enum.
- The renewal worker selects wallet quotas by their flags and matches renewals by product id and service
  name (`billing`, "Automatic renewal of a wallet subscription"); it has no rule per quota id for the
  administrators plan.
- A row of a primary wallet plan stays in the tariff after its due date: only overdue add-on rows are moved
  out of it. So "the tariff holds the plan" and "the plan is in force" are different facts.

## Goals / Non-Goals

**Goals:**

- The yearly plan can be bought, moved to and renewed exactly as the monthly one.
- A subscription moved to the wallet keeps its period.
- One place decides "this is a wallet administrators plan" and "the other one is in force".
- The seed cannot get two wallet rows with one product id or one service name unnoticed.

**Non-Goals:**

- Switching between the monthly and the yearly plan while one is in force, and any proration between them.
- Registering the product in the payment system.
- The PostgreSQL migrations, which hold none of the wallet quotas.
- The hosted checkout (`getPaymentUrl`), which keeps refusing yearly and wallet products.
- What `changeTenantWalletServiceState` means for a plan: it stores `AdminYear` as it stores `Admin`.

## Decisions

1. **Seed row.** Quota id -19, name `adminwalletyear`, features of `adminyear`, price 220, wallet, primary
   (not additional), visible. Product id `1021` and service name `adminyear` are the ones the payment system
   registered for the plan (the billing product and the accounting service, given by the task owner), at the
   same price of 220 per administrator. The
   service name has the description and unit texts of the wallet operations already, so no new letter or
   report text is needed. *Alternative:* an empty product id until the product exists — rejected: a wallet
   quota without a product id cannot be found by name for a purchase, so nothing could be tested.
2. **Enum member `AdminYear`.** The usage filter and the report resolve a service through the enum, so
   without a member the new plan answers 404 there. *Alternative:* relax those checks to "any wallet
   quota" — rejected as a wider behaviour change than this task. The member is accepted by every operation
   that binds the enum; the delta states the three that change.
3. **One predicate for both plans**, next to the enum, used by `updatewallet` (minimum by administrators) and
   `movetowallet` (accepted products). *Alternative:* decide by the quota's flags (`wallet` and not
   additional) — rejected for now: the flags describe the catalogue, and a third primary wallet plan would
   silently get the administrators rules.
4. **The other plan blocks a purchase only while it is in force.** `updatewallet` with `Add` answers
   400 "Quota is already set", the text the DocsCloud / Dev Pack pair already uses, when the tariff holds the
   other administrators plan and its due date has not passed. The check stands before the wallet is read or
   topped up. A row whose due date has passed does not count: it does not leave the tariff when the plan
   ends, so counting it would block the other plan for good. The decision is a function of the tariff rows,
   next to the predicate, with a unit test for an end later, in a minute and in the past. `Set` cannot bring
   the other plan in: the next quantity is stored only for a quota the tariff holds. The check and the
   purchase of an administrators plan run under one lock per portal, on a tariff read anew from the billing
   service, so of two `Add` requests sent together the second sees what the first bought; the DocsCloud
   switch to the Dev Pack is guarded the same way. The lock is released when the request ends, also on a
   failure, and a request that cannot get it fails instead of buying. `movetowallet` has no
   check of its own: it answers 400 "Invalid product" when the first primary row of the tariff is a wallet
   product, and a tariff with a provider subscription and a wallet administrators plan side by side is not a
   state the purchase paths produce.
5. **The move keeps the period.** `movetowallet` reads the quota of the current subscription before anything
   is moved and answers 400 "Invalid product" when the `year` feature of that quota and of the requested
   plan differ: a yearly subscription moves to the yearly plan only, a monthly one to the monthly plan only
   (decided by the task owner). The decision is a function of the two flags with a unit test. *Alternative:* choose the plan from the subscription and ignore the name
   in the request — rejected: the request names the product and its count, and a silent swap would charge a
   price the caller did not ask for.
6. **Migrations.** One MySQL SaaS migration inserts the row (down: deletes it). One MySQL Standalone
   migration deletes it, because the runner applies the SaaS migrations to a standalone database first.
7. **Guard test on the seed**: non-empty product ids are unique over all rows, and service names are unique
   over the wallet rows. It closes the class "a duplicate breaks the payment lookup" for the next row.

## Risks / Trade-offs

- [The product is not in the payment system of an environment the branch is deployed to] → the billing
  product and the accounting service are registered (reported by the task owner); an environment with its
  own payment system needs them before the migration. Without the product `updatewallet` answers 402 and
  moves nothing, but `movetowallet` ends the subscription and tops the wallet up before the billing service
  refuses the purchase. The order of those steps predates the change.
- [A purchase made after the other plan ended leaves both rows in the tariff] → it does not: the billing
  service does not list an ended payment among the current ones (confirmed by the task owner), and the
  tariff rows are rebuilt from that list, so the ended row goes with the first tariff read after the
  purchase. Until that purchase the ended row stays in the stored tariff, which is why the check of the
  other plan reads its end.
- [The period of the subscription is read from the `year` feature of its quota] → only the plan `adminyear`
  carries it, so the other paid plans, the legacy ones included, move to `adminwallet` only.
- [The registered price differs from the seeded 220, or changes later] → the API reads the price through a
  cache of an hour. The renewal worker keeps its list of wallet quotas, prices included, for the life of its
  process, so a changed price, product id or service name reaches renewals after the worker restarts.
- [An `Add` of an administrators plan and `movetowallet` at the same moment] → not under the lock:
  `movetowallet` is refused for a portal whose plan is a wallet product, and a provider subscription with a
  wallet administrators plan beside it is not a state the purchase paths produce.
- [`feature/business-tools` uses quota id -19 for another quota] → that branch is
  renumbered (row, enum member, migration) when it meets this one; decided by the task owner.
- [The in-flight change `wallet-bonus` modifies "Moving the subscription balance to the wallet" too] → the
  change archived second re-bases its delta on the main spec the first one left.
- [The merged quota of a portal on the yearly plan reports `year`] → intended; the three readers of that flag
  work on the non-wallet catalogue.
- [A year is long for a wallet renewal] → the worker tops the wallet up for price times count at the due
  date, as for the monthly plan; no new step, so no new row in the renewal's failure table. The three-day
  letter about an upcoming payment covers add-ons only, for the monthly plan as well.

## Migration Plan

Deploy: with the product and the service registered in the payment system, apply the SaaS migration (and the Standalone one on
standalone). Rollback: the down migration deletes the row; a portal that bought the plan in between would
keep a tariff row without a quota, so roll back only before the product is sold.

## Open Questions

None.
