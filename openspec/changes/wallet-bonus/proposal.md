# Proposal

## Why

The accounting and billing services now keep bonus money for a portal on a goodwill account of its own: promo
codes credit it, bonuses expire, and the accounting service spends the bonus before the wallet money. The
portal had no way to redeem a code, show the bonus, tell bonus movements from wallet money, or count the bonus
when it pays from the wallet, so customers were charged on their card for what their bonus already covered.

## What Changes

- New `applyPromoCode` (`POST api/2.0/portal/payment/promoCode`): a DocSpace admin redeems a promo code; the
  answer says whether the bonus is credited now or later, its amount, currency and expiry. The redemption is
  audited, and a bonus credited at once is announced to the portal's clients like a top-up.
- New `getCustomerGoodwill` (`GET api/2.0/portal/payment/customer/goodwill`): the bonus balance with the bonus
  credits still to spend.
- `getCustomerOperations` and `getCustomerServiceUsage` tell which part of an amount fell on the bonus
  (`sources`, `isBonus`, amounts always zero or more); bonus credits and expirations appear as their own
  movement kinds with their own descriptions; the operations and usage report files get bonus columns.
- The `type` filter of `getCustomerOperations` and its report now reaches the accounting service; before it was
  sent under a name the service ignores, so every kind came back.
- **BREAKING** `moveSubscriptionToWallet` counts the bonus: it tops the wallet up only for the part the wallet
  money and the bonus do not cover, and answers 402 only when both together fall short after the top-up. An
  unreadable bonus counts as none.
- **BREAKING** The automatic renewal of a wallet subscription counts the bonus the same way.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `billing`: found by `moveSubscriptionToWallet`, `getCustomerOperations`, `getCustomerServiceUsage`,
  `createCustomerOperationsReport`, `createCustomerServiceUsageReport` and the routes
  `api/2.0/portal/payment/*`, where the new `applyPromoCode` and `getCustomerGoodwill` belong.
  Requirement modified: "Moving the subscription balance to the wallet". Requirements added: redeeming a
  promo code, the bonus balance, the bonus part of movements and usage, paying from the wallet with the bonus counted.

## Breaks current behaviour

- **Moving the subscription balance to the wallet** (`moveSubscriptionToWallet`). With a bonus in the
  wallet currency, the card is charged only for the price minus the wallet money and the bonus, possibly
  nothing; the bonus is spent on the administrators first. The 402 after the top-up now means that the wallet
  and the bonus together fall short. Affected:
  - web client (`../client`): a dialog that computes the amount to top up from the wallet balance alone shows
    more than is charged; the client was not checked from this repository;
  - SDKs and the OpenAPI document: only the description of the operation changes, no schema;
  - webhooks, socket events, backup and restore, portal migration, stored data: none; the same wallet event
    is sent after the move.
- **Automatic renewal of a wallet subscription** (no requirement names its top-up amount today). The card is
  charged less, or not at all, when a bonus covers the price; "Insufficient balance" is raised only when the
  wallet and the bonus together fall short.
- **`type` filter of `getCustomerOperations`**: a caller that passed it got every kind and now gets the kind
  asked for, as the requirement "Wallet movements, usage and reports" already states.
- Additive for existing callers: two operations, `sources` on movements and usage totals, the movement kind
  `GoodwillExpiration`, bonus columns in the report files.

Deviations: `W4` (`moveSubscriptionToWallet` compares a price in the request region's currency with a USD
wallet) is kept: the bonus is read in the same currency as the wallet. `W13` (`topUpDeposit` reads an
unreadable answer as false) is kept.

## Impact

- API: `PaymentController` (two actions, texts of `moveSubscriptionToWallet`, movements and usage), payment
  DTOs, `PaymentHelper`.
- Core: billing and accounting clients (promo code, goodwill balance, bonus credit, `operationTypes`),
  `TariffService` (goodwill read, top-up with the bonus counted), operation sources.
- Reports: operations and service usage report builders.
- Audit: a new payment action, translated in every culture.
- SDK: regenerated C# package; the SDK submodules are regenerated but not committed.
