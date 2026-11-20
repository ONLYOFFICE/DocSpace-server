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
- **BREAKING** `moveSubscriptionToWallet` counts the bonus credits that have not ended: it tops the wallet up only for the part the wallet
  money and the bonus do not cover, and answers 402 only when both together fall short after the top-up. A bonus
  that cannot be read before the first top-up counts as none; one that cannot be read after it keeps the bonus read
  before.
- **BREAKING** The automatic renewal of a wallet subscription counts the bonus the same way.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `billing`: found by `moveSubscriptionToWallet`, `getCustomerOperations`, `getCustomerServiceUsage`,
  `createCustomerOperationsReport`, `createCustomerServiceUsageReport` and the routes
  `api/2.0/portal/payment/*`, where the new `applyPromoCode` and `getCustomerGoodwill` belong.
  Requirements modified: "Moving the subscription balance to the wallet", "Automatic renewal of a wallet
  subscription" (the wallet and the bonus decide the top-up), "Buying wallet services" (a bonus stands in for a
  missing wallet sub-account). Requirements added: redeeming a
  promo code, its audit and announcement, the bonus balance, the bonus part of movements and usage, paying from the
  wallet with the bonus counted.

## Breaks current behaviour

- **Moving the subscription balance to the wallet** (`moveSubscriptionToWallet`). With a bonus in the
  wallet currency, the card is charged only for the price minus the wallet money and the bonus, possibly
  nothing; the accounting service spends the bonus on the administrators first. The 402 after the top-up now means that the wallet
  and the bonus together fall short. Affected:
  - web client (`../client`): a dialog that computes the amount to top up from the wallet balance alone shows
    more than is charged; the client was not checked from this repository;
  - SDKs and the OpenAPI document: only the description of the operation changes, no schema;
  - webhooks, socket events, backup and restore, portal migration, stored data: none; the same wallet event
    is sent after the move.
- **Automatic renewal of a wallet subscription** (the requirement the release added says the wallet is topped up
  for what it lacks; it now says what the wallet and the bonus lack). The card is
  charged less, or not at all, when a bonus covers the price; "Insufficient balance" is raised only when the
  wallet and the bonus together fall short.
- **`type` filter of `getCustomerOperations`**: a caller that passed it got every kind and now gets the kind
  asked for, as the requirement "Wallet movements, usage and reports" already states.
- **Buying wallet services** (`updateWalletPayment` and `calculateWalletPayment` with `Add`). The accounting service
  gives a customer who has linked a card but never topped up the wallet no wallet sub-account, also with a bonus
  credited (seen on a stand with the accounting test service). Such a portal got 404 "Subaccount could not be
  found"; with a bonus in the currency that has not ended it now goes on to the purchase, which the bonus pays. A
  bonus that cannot be read lets the call go on as well, and billing decides. A portal known to have neither a
  sub-account nor such a bonus still gets 404.
- **A wallet never topped up, when the wallet pays** (`moveSubscriptionToWallet`, the automatic renewal). The top-up
  read the missing sub-account as a missing balance and answered 404, also before this change; it now reads it as
  an empty wallet, so the bonus counts and the shortfall is topped up. Without a balance at all it is still 404.
- **`lastCredit.date` of `getCustomerBalance`**: it shared the type of the bonus balance's `lastCredit` and was a
  plain date written as UTC (`...Z`); it is now an `ApiDateTime` with the portal's offset, like the dates of the
  movements. The moment is the same; a client that parses ISO 8601 reads both forms. The web client (its
  `feature/wallet-bonus` branch and its UI kit) does not read `lastCredit`. In the OpenAPI document the property
  becomes a `$ref` to `ApiDateTime`, so the C# SDK model `TransactionInfoDto` takes and returns `ApiDateTime` instead
  of `DateTime`: code built on the SDK that reads `Date` as a `DateTime` no longer compiles.
- **Automatic wallet top-up and the low balance letter** (`TopUpWalletService`). A socket service that fails after
  the deposit no longer reports the top-up to the payer as failed, and one that fails before the low balance letter
  no longer keeps the letter from going out; both failures are only logged.
- Additive for existing callers: two operations, `sources` on movements and usage totals, the movement kind
  `GoodwillExpiration`, bonus columns in the report files.

Deviations: `W4` (`moveSubscriptionToWallet` compares a price in the request region's currency with a USD
wallet) is kept: the bonus is read in the same currency as the wallet. `W13` (`topUpDeposit` reads an
unreadable answer as false) is kept. `getCustomerGoodwill` answers an accounting service it cannot read with an
empty result, as `getCustomerBalance` does (the class of `W10` and `W12`), by decision: the client reads the two
together, and the web client shows no bonus for an empty answer and for an error alike.

## Impact

- API: `PaymentController` (two actions, texts of `moveSubscriptionToWallet`, movements and usage), payment
  DTOs, `PaymentHelper`.
- Core: billing and accounting clients (promo code, goodwill balance, `operationTypes`),
  `TariffService` (goodwill read, top-up with the bonus counted), operation sources.
- Reports: operations and service usage report builders.
- Audit: a new payment action; its text is in English only for now (`AuditReportResource`, neutral culture).
- SDK: regenerated C# package; the SDK submodules are regenerated but not committed.
