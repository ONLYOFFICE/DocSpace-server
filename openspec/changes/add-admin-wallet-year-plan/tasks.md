# Tasks

## 1. Catalogue: seed, enum, migrations, title

- [x] 1.1 Add the `adminwalletyear` row (id -19) to the quota seed and `AdminYear` to the wallet service enum; verify `ASC.Core.Common` builds with `--no-incremental` and no new warnings
- [ ] 1.2 Add the title resource `Tariffs_adminwalletyear` (resx, hand-edited designer, the locales that have `Tariffs_adminwallet`); verify the `WalletServiceTextsTests` class of `ASC.Notify.Tests` passes
- [x] 1.3 Create the MySQL SaaS migration that inserts the row and the MySQL Standalone migration that deletes it (`db-migrations` skill); verify by applying them with `ASC.Migration.Runner` and reading the row from `tenants_quota`
- [x] 1.4 Add a unit test over the seed: unique non-empty product ids, unique service names of wallet rows; verify it fails when the new row is given product id `1013` (`prove-red.sh`)
- [x] 1.5 Add a unit test that the merged quota of the yearly wallet plan carries its price and the `year` feature; verify it passes in `ASC.Core.Common.Tests`

## 2. Buying and moving: both administrators plans

- [x] 2.1 Check `WalletRenewalTiming`, the upcoming-payment letter and the retention job for an assumption of a monthly period; verify by writing the result (file and line, or "none") into the commit message
- [x] 2.2 Add the predicate "wallet administrators plan" next to the enum with a unit test for every enum member; verify the test passes
- [x] 2.3 Use the predicate in `updatewallet` (minimum by administrators) and `movetowallet` (accepted products), and refuse a purchase while the tariff holds the other plan, before the wallet is read; verify `ASC.Web.Api` builds with no new warnings
- [x] 2.4 Update the remarks and `[SwaggerResponse]` texts of `updateWalletPayment` and `moveSubscriptionToWallet` and the quantity text of the payment request DTO; verify with `check_texts.py` of the `openapi-desc-opt` skill
- [x] 2.5 Integration tests in `ASC.Web.Api.Billing.Tests` for the new requirement "Yearly administrators plan paid from the wallet": bought with `Add`; a count below the administrators answers 400; each plan refused with 400 "Quota is already set" while the other is in the tariff; the catalogue entry with price 220 and `year`; verify each fails without 2.3 or 1.1 (`prove-red.sh`)
- [x] 2.6 Integration tests for the modified requirement "Moving the subscription balance to the wallet": `adminwalletyear` passes the product check, another wallet product answers 400 "Invalid product"; verify they fail without 2.3
- [x] 2.7 Integration test that the renewal worker renews the yearly plan for price times count; verify it passes in the `Renewal` namespace of `ASC.Web.Api.Billing.Tests`
- [x] 2.8 Update the rows of "Moving the subscription balance to the wallet" and "Buying wallet services" in `openspec/specs/billing/verification.md`: the `Controller:` line citations the change moves and the new tests; verify every cited line still shows what the row says

## 3. Contract and integration checks

- [ ] 3.1 Re-emit the OpenAPI documents and regenerate the SDKs for the new enum member (`generate-sdk` skill); verify the diff of the documents shows only the enum member and the changed texts
- [ ] 3.2 Run `dotnet format style --verify-no-changes` on the edited files and the style build of the touched projects; verify no new warnings
- [ ] 3.3 Run `openspec validate --specs --no-interactive` and `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec billing`; the change adds no operationId, so verify that `updateWalletPayment`, `moveSubscriptionToWallet` and `getPaymentQuotas` are each called by a test

## Workflow follow-up

- When the payment system has the product: a migration that sets the real product id and service name, and the test constants.
- Tell the frontend: the product name `adminwalletyear`, the enum member `AdminYear`, the `year` feature as the sign of the yearly plan, the 400 "Quota is already set".
- Renumber the Business tools quota in `feature/business-tools` when it meets this branch.
- Archive the change after review.
