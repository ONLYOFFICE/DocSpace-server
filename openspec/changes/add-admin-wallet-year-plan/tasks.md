# Tasks

## 1. Catalogue: seed, enum, migrations, title

- [x] 1.1 Add the `adminwalletyear` row (id -19) to the quota seed and `AdminYear` to the wallet service enum; verify `ASC.Core.Common` builds with `--no-incremental` and no new warnings
- [x] 1.2 Add the title resource `Tariffs_adminwalletyear` (resx, hand-edited designer, the locales that have `Tariffs_adminwallet`); verify the `WalletServiceTextsTests` class of `ASC.Notify.Tests` passes
- [x] 1.3 Create the MySQL SaaS migration that inserts the row and the MySQL Standalone migration that deletes it (`db-migrations` skill); verify that both migration projects build, and that the SaaS one, applied with `ASC.Migration.Runner` before it was committed, leaves the row in `tenants_quota` (read back with the product id it had then, `1017`; the id was changed twice afterwards, in the migration and by hand in that database, and the migration was not applied again)
- [ ] 1.6 Apply the Standalone migration on a standalone database and verify the row is gone. Not done: no standalone database on the development stand
- [x] 1.4 Add a unit test over the seed: unique non-empty product ids, unique service names of wallet rows; verify by hand that both uniqueness tests fail when the new row is given product id `1013` and service name `admin`
- [x] 1.5 Add a unit test that the merged quota of the yearly wallet plan carries its price and the `year` feature; verify it passes in `ASC.Core.Common.Tests`

## 2. Buying and moving: both administrators plans

- [x] 2.1 Check `WalletRenewalTiming`, the upcoming-payment letter and the retention job for an assumption of a monthly period; verify by writing the result (file and line, or "none") into the commit message
- [x] 2.2 Add the predicate "wallet administrators plan" next to the enum with a unit test for every enum member; verify the test passes
- [x] 2.3 Use the predicate in `updatewallet` (minimum by administrators) and `movetowallet` (accepted products), and refuse a purchase while the tariff holds the other plan, before the wallet is read; verify `ASC.Web.Api` builds with no new warnings
- [x] 2.4 Update the remarks and `[SwaggerResponse]` texts of `updateWalletPayment` and `moveSubscriptionToWallet`; verify with `check_texts.py` of the `openapi-desc-opt` skill
- [x] 2.5 Integration tests in `ASC.Web.Api.Billing.Tests` for the new requirement "Yearly administrators plan paid from the wallet": bought with `Add`; a count below the administrators answers 400; each plan refused with 400 "Quota is already set" while the other is in the tariff; the catalogue entry with price 220 and `year`; verify with `prove-red.sh` that the count and the two refusal cases fail without 2.3 (the purchase and the catalogue entry pass on the seed alone)
- [x] 2.6 Integration tests for the modified requirement "Moving the subscription balance to the wallet": `adminwalletyear` passes the product check, another wallet product answers 400 "Invalid product"; verify with `prove-red.sh` that the first fails without 2.3 (the second guards against widening and passes without it)
- [x] 2.7 Integration test that the renewal worker renews the yearly plan for price times count; verify it passes in the `Renewal` namespace of `ASC.Web.Api.Billing.Tests`
- [x] 2.8 Update the rows of "Moving the subscription balance to the wallet" in `openspec/specs/billing/verification.md`: the `Controller:` line citations the change moves and the new tests; verify every cited line still shows what the row says

## 3. Contract and integration checks

- [x] 3.1 Re-emit the OpenAPI documents and rebuild the C# SDK package for the new enum member (`generate-sdk` skill); verify the diff of the documents shows the enum member, the changed texts and nothing else of this change
- [x] 3.2 Run `dotnet format style --verify-no-changes` on the edited files; verify no changes are reported
- [x] 3.3 Run `openspec validate --specs --no-interactive` and `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec billing`; the change adds no operationId, so verify that `updateWalletPayment`, `moveSubscriptionToWallet` and `getPaymentQuotas` are each called by a test

## 4. After the hand-over check

- [x] 4.1 Count the other administrators plan only while its due date has not passed, as a function of the tariff rows next to the predicate; verify with a unit test for an end later, in a minute and in the past, and an integration test that buys each plan after the other has ended (`prove-red.sh`)
- [x] 4.2 Keep the period in `movetowallet`: a yearly subscription moves to `adminwalletyear` only and a monthly one to `adminwallet` only, the other plan answers 400 "Invalid product" before the balance is moved; verify with a unit test of the decision and integration tests for both directions (`prove-red.sh`)
- [x] 4.3 Correct the quantity texts of the payment request DTO (the key is the product name, the example is one the call accepts) and give the two administrators members of the enum a code comment (the published text of an enum member comes from its `[Description]`, which is not changed); verify with `check_texts.py` and a re-emitted document
- [x] 4.4 Integration tests for the cases the first round left out: more of the plan the portal holds, `Set` of a plan it does not hold, and the three operations that take the new enum member or its service name (`changeTenantWalletServiceState`, `getWalletService`, `getCustomerOperations`); verify they pass
- [x] 4.5 Update `openspec/specs/billing/verification.md` and `openspec/specs/quotas/verification.md` for the lines this round moves, cite the new tests in the rows of "Buying wallet services", and leave the run marks to the next run; verify the cited lines
- [x] 4.6 Re-emit the Web.Api document and rebuild the C# SDK package; verify that the package the tests restore is the repository's one (its hash), then run `ASC.Web.Api.Billing.Tests` and the unit classes of the change
- [x] 4.7 Style build of the touched projects (`-p:EnforceCodeStyleInBuild=true`); verify no new warnings in the changed files

## 5. Registered product

- [x] 5.1 Set the product id the payment system registered (`1021`) in the seed, the SaaS migration with its designer and snapshot, and the test constants; verify the seed tests and the billing classes of the change pass

- [x] 5.2 Make the check of the other administrators plan and the purchase one step per portal: a distributed lock in `updatewallet` with `Add` and a tariff read anew under it; verify with an integration test of two `Add` requests sent together (`prove-red.sh`)

## 6. After the review of 536589bc7c

- [x] 6.1 Ask the billing service itself for the current payments under the lock of an administrators plan purchase and end the call when it does not answer, instead of deciding on the stored tariff; verify with an integration test where billing answers 500 to that request (`prove-red.sh`)
- [x] 6.2 Answer 409 when the lock of an administrators plan purchase is not got within the provider's wait, with its `[SwaggerResponse]` and remarks; verify that `ASC.Web.Api` builds with no new warnings. The wait itself is not reproduced by a test: it needs a billing answer slower than the lock wait
- [x] 6.3 Keep only the meaning of the key and the value in the two `quantity` texts of the payment request DTO, drop the sentence about a null or zero value, and correct the wording of the `movetowallet` remarks; verify with `check_texts.py` on the two operations
- [x] 6.4 State the seats of the yearly plan as a scenario, the purchase on the billing service's own answer as a requirement, and "nothing left of the period to move" in the 440 USD scenario; verify with `openspec validate --specs --no-interactive`
- [x] 6.5 Shift every citation of `PaymentRequestsDto.cs` and `PaymentHelper.cs` since the merge-base and of `PaymentsController.cs` and the yearly plan tests for this round, in the three `verification.md` files that cite them, the bare ranges that continue a citation included; verify the cited lines of the rows the round touches
- [x] 6.6 Re-emit the Web.Api document and rebuild the C# SDK package for the changed texts and the 409; verify the diff of the document, then run `ASC.Web.Api.Billing.Tests` and the unit classes of the change on the repository's package

## Workflow follow-up

- The billing product 1021 and the accounting service `adminyear` are registered; an environment with its own payment system needs both before the migration.
- Tell the frontend: the product name `adminwalletyear`, the enum member `AdminYear`, the `year` feature as the sign of the yearly plan, the 400 "Quota is already set", and that `movetowallet` takes the plan of the subscription's own period only.
- Check the web client (`../client`) for the wallet service enum and the list of wallet plans.
- Commit the regenerated SDK sources and the published contract in their submodules and move the pointers.
- Renumber the Business tools quota in `feature/business-tools` when it meets this branch.
- Archive the change after review.
