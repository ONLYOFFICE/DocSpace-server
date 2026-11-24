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
  an empty price list, cached for an hour, and the seeded price is used (`billing`, "Prices are resolved per
  region and cached").
- The rules of `adminwallet` are tied to one enum member in two places of the payments controller, and the
  usage filter accepts only members of the wallet service enum.
- The renewal worker selects wallet quotas by their flags and matches renewals by product id and service
  name (`billing`, "Automatic renewal of a wallet subscription"); it has no rule per quota id for the
  administrators plan.

## Goals / Non-Goals

**Goals:**

- The yearly plan can be bought, moved to and renewed exactly as the monthly one.
- One place decides "this is a wallet administrators plan".
- The seed cannot get two wallet rows with one product id or one service name unnoticed.

**Non-Goals:**

- Switching between the monthly and the yearly plan, and any proration between them.
- Registering the product in the payment system, and the real product id and service name.
- The PostgreSQL migrations, which hold none of the wallet quotas.
- The hosted checkout (`getPaymentUrl`), which keeps refusing yearly and wallet products.

## Decisions

1. **Seed row.** Quota id -19, name `adminwalletyear`, features of `adminyear`, price 220, wallet, primary
   (not additional), product id `1020` and service name `adminyear` as placeholders. The service name
   reuses the existing description and unit texts of the wallet operations, so no new letter or report text
   is needed. *Alternative:* an empty product id until the product exists — rejected: a wallet quota without
   a product id cannot be found by name for a purchase, so nothing could be tested.
2. **Enum member `AdminYear`.** The usage filter and the report resolve a service through the enum, so
   without a member the new plan answers 404 there. *Alternative:* relax those checks to "any wallet
   quota" — rejected as a wider behaviour change than this task.
3. **One predicate for both plans**, next to the enum, used by `updatewallet` (minimum by administrators) and
   `movetowallet` (accepted products). *Alternative:* decide by the quota's flags (`wallet` and not
   additional) — rejected for now: the flags describe the catalogue, and a third primary wallet plan would
   silently get the administrators rules.
4. **One plan at a time**, checked before the wallet is read or topped up, in `updatewallet` (`Add`) and in
   `movetowallet`: if the tariff holds the other administrators plan, 400 "Quota is already set", the text
   the DocsCloud / Dev Pack pair already uses. The tariff row counts whatever its state, so an overdue
   monthly plan also blocks the yearly one; the way out is to cancel it (`Set` 0) and let it end.
5. **Migrations.** One MySQL SaaS migration inserts the row (down: deletes it). One MySQL Standalone
   migration deletes it, because the runner applies the SaaS migrations to a standalone database first.
6. **Guard test on the seed**: non-empty product ids are unique over all rows, and service names are unique
   over the wallet rows. It closes the class "a duplicate breaks the payment lookup" for the next row.

## Risks / Trade-offs

- [The placeholders differ from what the payment system registers] → a follow-up migration updates the row
  and the test constants; until then a real purchase is refused by the billing service.
- [After registration the price stays the seeded one for up to an hour] → the existing price cache; accepted.
- [`feature/business-tools` uses quota id -19 for another quota] → that branch is renumbered (row, enum
  member, migration) when it meets this one; decided by the user.
- [The in-flight change `wallet-bonus` modifies "Moving the subscription balance to the wallet" too] → the
  change archived second re-bases its delta on the main spec the first one left.
- [The merged quota of a portal on the yearly plan reports `year`] → intended; the three readers of that flag
  work on the non-wallet catalogue.
- [A year is long for a wallet renewal] → the worker tops the wallet up for price times count at the due
  date, as for the monthly plan; no new step, so no new row in the renewal's failure table.

## Migration Plan

Deploy: apply the SaaS migration (and the Standalone one on standalone). Rollback: the down migration deletes
the row; a portal that bought the plan in between would keep a tariff row without a quota, so roll back only
before the product is sold.

## Open Questions

- The real product id and service name of the plan in the payment system.
