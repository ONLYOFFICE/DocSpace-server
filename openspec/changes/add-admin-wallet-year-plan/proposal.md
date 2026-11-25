# Proposal

## Why

The Business plan paid from the wallet exists only with a monthly period (`adminwallet`). A yearly plan paid
through the hosted checkout exists (`adminyear`), but a portal that pays from the wallet cannot take a year,
and a yearly subscription moved to the wallet becomes a monthly plan. The change adds the yearly wallet plan
`adminwalletyear` with its own price. Its product and its service are registered in the payment system, and
the plan behaves like `adminwallet`.

## What Changes

- A new seeded quota `adminwalletyear`: a primary wallet plan with the features of `adminyear` (the `year`
  feature included) and a price of 220 per administrator. The hosted (SaaS) catalogue gets it, a standalone
  installation does not. Its product id and service name are the ones registered in the payment system.
- `updateWalletPayment` with `Add` sells `adminwalletyear` under the rules of `adminwallet`: the quantity is
  not below the portal's administrators unless the plan is already active.
- `updateWalletPayment` with `Add` refuses one administrators plan with 400 "Quota is already set" while the
  tariff holds the other with its period not ended. A plan that has ended does not count. The check and the
  purchase are one step per portal, so two requests sent together do not buy both. For the check the billing
  service is asked for the portal's current payments: when it does not answer, the call answers 402 and
  buys nothing, and a request that waits for the step of another until the wait ends answers 409. Switching
  between the two while one is in force is not part of this change.
- `moveSubscriptionToWallet` accepts `adminwalletyear` as well as `adminwallet`, and the move keeps the
  period: a yearly subscription moves to `adminwalletyear` only, a monthly one to `adminwallet` only. The plan
  of the other period answers 400 "Invalid product".
- The wallet service enum published in the API gets the member `AdminYear`. Three operations that bind the
  enum or its service names change with it: `changeTenantWalletServiceState` and `getWalletService` take
  `AdminYear`, and the `serviceName` filter of the wallet movements takes `adminyear`.
- The renewal worker is not changed: it renews every wallet plan by its due date, which the billing service
  sets.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `billing`: found by `updateWalletPayment` (`PUT payment/updatewallet`), `moveSubscriptionToWallet`
  (`POST payment/subscription/movetowallet`), `getPaymentQuotas` (`GET payment/quotas`),
  `changeTenantWalletServiceState` (`POST payment/servicestate`), `getWalletService`
  (`GET payment/walletservice`) and `getCustomerOperations` (`GET payment/customer/operations`).
  - "Moving the subscription balance to the wallet" changes: two products instead of one.
  - "Wallet services switched on per portal" changes: the list of `service` values gets `AdminYear`. The
    delta also rewords two phrases of that requirement, with the same meaning, to keep its text under the
    length limit: "403 otherwise and without billing" as "else 403, also without billing", and
    "400 otherwise" as "else 400".
  - Four requirements are added: the yearly wallet plan with the rule for the other plan, the purchase of an
    administrators plan on the billing service's own answer, the move that keeps the period of the
    subscription, and the yearly plan in the operations that take a service.
  - "Buying wallet services" keeps its text: its minimum "the portal's administrators" now applies to both
    plans.
  - "Paid seats and tariff limits" keeps its text ("Buying `Admin` units from the wallet SHALL raise the
    seats"): `AdminYear` units raise them the same way, through the `manager` count of the row. The text
    is 8 characters short of the length limit, so the yearly plan is stated as a scenario of the new
    requirement instead of a modified one.

## Breaks current behaviour

- **"Moving the subscription balance to the wallet"**: `moveSubscriptionToWallet` with the product
  `adminwalletyear` answered 400 "Invalid product" (the name matched no quota); for a yearly subscription it
  now buys the yearly plan, for a monthly one it still answers 400 "Invalid product". With `adminwallet` on a
  yearly subscription it answered 200 and bought the monthly plan; it now answers 400 "Invalid product" and
  moves nothing. A monthly subscription with `adminwallet` behaves as before.
- **"Buying wallet services"** (text unchanged): `updateWalletPayment` with `Add` for `adminwallet` on a portal
  whose `adminwalletyear` is in force now answers 400 "Quota is already set". No existing portal holds that
  plan, so no existing caller sees a difference. `Add` for `adminwallet` now answers 402 when the billing
  service does not answer the request for the current payments; it used to go on to the purchase on the
  stored tariff. It answers 409 when another purchase of an administrators plan holds the portal's lock for
  the whole wait.
- **"Wallet services switched on per portal"**: `changeTenantWalletServiceState` with `service` `AdminYear`
  answered 400 (not a member of the enum); it now answers 200 and stores it, as it does for `Admin`.
- **"Purchasable quotas and wallet services"** (text unchanged): `getWalletService` with `service`
  `AdminYear` answered 400; it now answers the yearly plan on a hosted portal.
- **Wallet movements**: the `serviceName` filter with `adminyear` answered 404; it now reaches the accounting
  service.
- Who is affected:
  - the web client (`../client`): not checked in this change. It has to offer the new product name, read the
    `year` feature, and send `adminwalletyear` when it moves a yearly subscription: a client that sends
    `adminwallet` for a yearly subscription now gets 400;
  - the published SDKs and the OpenAPI document: the wallet service enum gains `AdminYear`, and the texts of
    two operations and two request properties change. The Web.Api document and the C# package are
    regenerated in the branch; the SDK submodules follow separately. No field is removed or renamed;
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
- `PaymentsController` (`updatewallet`, `subscription/movetowallet`), `PaymentHelper`, the wallet service
  enum, the payment request DTO texts, the tariff title resource.
- The Web.Api OpenAPI document and the C# SDK package.
- The payment system: the billing product and the accounting service of the plan are registered. In an
  environment without them the billing service refuses the purchase; in `moveSubscriptionToWallet` that
  refusal comes after the subscription has ended and the wallet was topped up. The integration tests run
  against the payment stub.
- The branch `feature/business-tools` uses the same quota id for another quota and has to be renumbered when
  the two meet.
