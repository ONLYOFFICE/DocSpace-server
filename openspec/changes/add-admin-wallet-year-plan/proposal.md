# Proposal

## Why

The Business plan paid from the wallet exists only with a monthly period (`adminwallet`). A yearly plan paid
through the hosted checkout exists (`adminyear`), but a portal that pays from the wallet cannot take a year.
The change adds the yearly wallet plan `adminwalletyear` with its own price. Its product is registered in the
payment system later, so the plan is seeded now and behaves like `adminwallet`.

## What Changes

- A new seeded quota `adminwalletyear`: a primary wallet plan with the features of `adminyear` (the `year`
  feature included) and a price of 220 per administrator. Its product id and service name are placeholders
  until the payment system has the product; the hosted (SaaS) catalogue gets it, a standalone installation
  does not.
- `updateWalletPayment` with `Add` sells `adminwalletyear` under the rules of `adminwallet`: the quantity is
  not below the portal's administrators unless the plan is already active.
- `moveSubscriptionToWallet` accepts `adminwalletyear` as well as `adminwallet`.
- A portal holds one wallet administrators plan: buying `adminwallet` while `adminwalletyear` is in the tariff,
  or the reverse, is refused with 400 "Quota is already set". Switching between the monthly and the yearly
  plan is not part of this change.
- The wallet service enum published in the API gets the member `AdminYear`, so the usage report and the
  usage filter accept the new plan.
- The renewal worker is not changed: it renews every wallet plan by its due date, which the billing service
  sets.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `billing`: found by `updateWalletPayment` (`PUT payment/updatewallet`), `moveSubscriptionToWallet`
  (`POST payment/subscription/movetowallet`) and `getPaymentQuotas` (`GET payment/quotas`). The requirement
  "Moving the subscription balance to the wallet" changes (two products instead of one), and a requirement is
  added for the yearly wallet plan and the one-plan rule. "Buying wallet services" keeps its text: its
  minimum "the portal's administrators" now applies to both plans.

## Breaks current behaviour

- **"Moving the subscription balance to the wallet"**: `moveSubscriptionToWallet` with the product
  `adminwalletyear` answered 400 "Invalid product" (the name matched no quota); it now buys the yearly plan.
  A request with `adminwallet` behaves as before, except on a portal that already holds `adminwalletyear`,
  which did not exist before.
- **"Buying wallet services"** (text unchanged): `updateWalletPayment` with `Add` for `adminwallet` on a portal
  that holds `adminwalletyear` now answers 400 "Quota is already set". No existing portal holds that plan, so
  no existing caller sees a difference.
- Who is affected:
  - the web client (`../client`): not checked in this change. It has to offer the new product name and read
    the `year` feature; nothing it sends today changes meaning;
  - the published SDKs and the OpenAPI document: the wallet service enum gains `AdminYear`; the documents and
    the SDKs are regenerated. No field is removed or renamed;
  - webhooks, socket events: none added or changed. The purchase raises the tariff events `adminwallet`
    raises today;
  - backup and restore, the portal migration: the quota table is already declared; a seeded row adds no
    schema;
  - existing stored data: the migration only inserts the new row. Standalone installations delete it, as
    they delete the other hosted-only quotas.

## Deviations

- `W4` (`moveSubscriptionToWallet` compares a price in the request region's currency with a USD wallet
  balance): kept. The yearly plan goes through the same comparison.

## Impact

- Seed data and migrations of the quota table (MySQL SaaS insert, MySQL Standalone delete); the PostgreSQL
  migrations do not hold the wallet quotas and are not changed.
- `PaymentsController` (`updatewallet`, `subscription/movetowallet`), the wallet service enum, the tariff
  title resource.
- OpenAPI documents and SDKs (enum member).
- Not registered in the payment system yet: until it is, the plan shows the seeded price, and a real purchase
  is refused by the billing service. The integration tests run against the payment stub.
- The branch `feature/business-tools` uses the same quota id for another quota and has to be renumbered when
  the two meet.
