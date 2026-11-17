# Tasks

## 1. Billing and accounting clients

- [x] 1.1 Read the goodwill account and credit a bonus through the accounting client; the credit verified by `AccountingClientBonusCreditTests` (credit read back, conflict, server error), the read by `BT/Bonus/GoodwillBalanceTests.cs` and `Unit/TariffServiceWalletCoverageTests.cs`
- [x] 1.2 Redeem a promo code through the billing client: refusal shape → reason code, any other failure stays a billing failure, unknown portal → not found; verified by `BillingClientPromoCodeTests`
- [x] 1.3 Read the source split of movements and usage totals, and send the `type` filter as `operationTypes`; verified by `AccountingClientOperationsTests` (`operationTypes` in the query, split read, null without one, negative bonus part as a magnitude)

## 2. Redeeming a promo code and the bonus balance

- [x] 2.1 Add `applyPromoCode` with the 1–100 character bound, 400 for a refusal, 402 for `promo_failed` and an unknown portal (500 for an answer that cannot be read), the audit event without the code and the wallet push for a completed credit; the codes verified by `BT/Bonus/PromoCodeTests.cs` and the access tests `BT/Bonus/BonusPermissionsTests.cs`, `BonusPermissionsControlTests.cs`, the unreadable answer by `Unit/BillingClientPromoCodeTests.cs`, the audit by 2.4, the push decision by 2.6 (its wiring is 2.5)
- [x] 2.2 Add `getCustomerGoodwill`; verified by `BT/Bonus/GoodwillBalanceTests.cs` (account with credits; no customer → empty without asking accounting)
- [x] 2.3 Add an integration test for the boundary of exactly 100 characters, passed to the billing service, and a code of only spaces refused like an empty one; verified by `BT/Bonus/PromoCodeTests.cs` (`ApplyPromoCode_CodeOfHundredCharacters_IsPassedToBilling`, `ApplyPromoCode_EmptyCode_…` with `""` and `"   "`)
- [x] 2.4 Add an integration test that a redeemed code, completed or pending, writes one audit event with the amount and currency and without the code, and a refused one none; verified by `BT/Bonus/PromoCodeAuditTests.cs` through `GET api/2.0/security/audit/events/filter`, red without the audit call
- [ ] 2.5 Add a test that a completed credit pushes the wallet change and a pending one does not; the billing stack runs no socket service, so it needs the socket service among the stack's resources or a test in a suite that has it
- [x] 2.6 Move the decision whether a redeemed bonus is pushed into `PromoCodeResult.IsCredited`; verified by `Unit/WalletBonusDecisionsTests.cs` (`IsCredited_OnlyForABonusAlreadyOnTheBalance`)

## 3. Bonus part of movements, usage and reports

- [x] 3.1 Add `sources` to movements and usage totals, unsigned, and the `GoodwillCredit`/`GoodwillExpiration` descriptions; verified by `BT/Customer/CustomerOperationsTests.cs` (expired bonus split by its sources) and `BT/Customer/CustomerServiceUsageTests.cs` (split, merged splits, merge without a split)
- [x] 3.2 Add the bonus columns to the operations and service usage report files; verified by the build only, no test yet (3.3)
- [ ] 3.3 Add a test that the operations report file has the "Credit (bonuses)" and "Debit (bonuses)" columns filled from `sources`; the billing stack has no document builder (Document Server) to produce the `xlsx`, so it needs a stack that runs one with the payment stubs
- [x] 3.4 Move the split of a movement's bonus part into the credit and debit columns into `OperationSource.BonusPartsOf`; verified by `Unit/WalletBonusDecisionsTests.cs` (`BonusPartsOf_PutsTheBonusOnTheSideOfTheMovement`, each side above 0 and both 0 included)

## 4. Paying from the wallet with the bonus counted

- [x] 4.1 Count the bonus in the wallet currency before topping up, an unreadable bonus as none, in `moveSubscriptionToWallet` and the subscription renewal; verified by `common/Tests/ASC.Core.Common.Tests/TariffServiceWalletCoverageTests.cs` (covers, part, other currency, unreadable, no top-up allowed)
- [x] 4.2 Next to the tests cited by the "Moving the subscription balance to the wallet" rows of `openspec/specs/billing/verification.md` (`BT/Checkout/SubscriptionPayerPermissionsTests.cs:42-49`, `SubscriptionPermissionsControlTests.cs:54-79`, which check the payer only and stay as they are), add billing-suite cases where no bonus, part of the price and the whole price are covered, and a bonus in another currency; verified by `BT/Checkout/SubscriptionMoveToWalletTests.cs` through the deposit request the stub received, red without `8c5d6d7264`
- [x] 4.3 Add billing-suite cases for the renewal with a bonus that covers part and the whole price; verified by `BT/Wallet/RenewalWithBonusTests.cs`, which waits for the renewal job of Web.Studio, red without `8c5d6d7264`
- [x] 4.4 Read the bonus again with the wallet after each top-up, keeping the last bonus read when a later read fails, and count a negative bonus as none; verified by `Unit/TariffServiceWalletCoverageTests.cs` (`EnsureWalletBalance_BonusSpentDuringTheTopUp_TopsUpAgainForTheRest`, `EnsureWalletBalance_BonusUnreadableAfterTheTopUp_KeepsTheBonusReadBefore`, `EnsureWalletBalance_NegativeBonus_CountsAsNone`, `EnsureWalletBalance_BonusAnswerUnreadable_TopsUpAsIfThereWereNone`), red without the re-read and without the clamp
- [x] 4.5 Count only the bonus credits that have not ended, at most the sub-account (`GoodwillBalance.SpendableAmount`); verified by `Unit/WalletBonusDecisionsTests.cs` (the end a day and an hour ago, now, an hour and a day ahead, never; an end without an offset; other currency; no `lots`) and `Unit/TariffServiceWalletCoverageTests.cs` (`EnsureWalletBalance_BonusEndedAnHourAgo_TopsUpAsIfThereWereNone`)

## 5. Specs and checks

- [x] 5.1 Add verification rows to `openspec/specs/billing/verification.md` for the new requirements, citing the tests of groups 1–4; verify each cited range opens on its test method
- [x] 5.2 Run `openspec validate --specs --no-interactive` and `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec billing`; the published document does not hold the new operations until the SDKs are regenerated, so check them against this list instead: `applyPromoCode` → `BT/Bonus/PromoCodeTests.cs` (SDK), `getCustomerGoodwill` → `BT/Bonus/GoodwillBalanceTests.cs` (SDK)

## Workflow follow-up

- The delta is synced into `openspec/specs/billing/spec.md`; archive the change once the branch is merged.
- Tasks 2.5 and 3.3 need a stack with the socket service or a document builder next to the payment stubs; the decisions behind them are unit-tested (2.6, 3.4), only the wiring is open.
