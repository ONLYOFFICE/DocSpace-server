# Tasks

## 1. The restriction decision

- [x] 1.1 Add `DelayedPaymentMethod` (worked out on every read; never serialized, kept only on the shared in-memory
  instance) and the computed
  `IsRestricted` and lane state to the tariff (`common/ASC.Core.Common/Billing/Tariff.cs`); verified by the unit test of 4.1.
- [x] 1.2 Write the last known `isDelayedPaymentMethod` on every answer of billing to the customer request (false
  for "cannot find" not under a 5xx, a portal that is not a customer) to its own key of the distributed cache, kept for the grace days plus one; write nothing on a failure;
  keep the key out of the tariff cache clearing. Verified by the unit test of 4.2 and the integration test of 4.6.
- [x] 1.3 When the written answer differs from the stored one (absent counts as false), send `change-tariff` to the
  portal's quota room after the write, best effort in its own `try/catch` with a `[LoggerMessage]` warning, without
  reading the tariff. Verified by the unit test of 4.2 (the event queued for the socket service); the billing stack
  runs no socket service, so 4.7 checks the effect on the tariff only.
- [x] 1.4 In the state computation, fill `DelayedPaymentMethod` when the state is Delay on a hosted installation,
  false otherwise, written once: the customer known now decides, then that key (1.5). Verified by 4.4 and 4.6.
- [x] 1.5 Ask billing for the customer only on a tariff read that already asks billing (a missed or refreshed tariff
  read with a request to the payment system, each of its calculations, in the grace period); any other read decides from the cached customer, then the last answer, and
  never waits for billing or the customer lock. The customer known now wins over the last answer. Skip all of it
  when billing is not configured. Write the flag once, after the customer is known, since the in-memory cache shares
  the tariff object. Verified by 4.12.

## 2. The checks

- [x] 2.1 Replace `State > Paid` with `IsRestricted` in the count checkers (`CountPaidUserChecker`,
  `CountUserChecker`, `CountRoomChecker`, `CountAIAgentChecker`); verified by 4.4 (the Room admin checker) and by
  the unit test of 4.1 (the decision of the room and AI agent checkers, see 4.5).
- [x] 2.2 Replace the state checks of `updateWalletPayment` (add-on), the subscription balance helper (Trial or
  restricted), `startDocsCloudTrial`, the Dev Pack preparation and the add-on wallet renewal with `IsRestricted`;
  verified by 4.8 and by the unchanged tests of those operations (the whole billing suite: the wallet services,
  Docs Connect and renewal classes pass).
- [x] 2.3 Run the backup schedule when the tariff is not restricted instead of below Delay; verified by the unit test
  of 4.1 (the decision) and the backup suite staying green (359 tests: none failed, 3 skipped).
- [x] 2.4 Pass the lane state instead of `State` from the socket client, the audit event handler, the thumbnail
  handler and the tariff change notification; verified by build and the unit test of 4.1.
- [x] 2.5 `findReferences` on the tariff state once more over the solution: no check that refuses an action in the
  grace period is left on `State`; the list of the ones deliberately kept (letters, retention, audit cleaner,
  payment URL, billing sync) goes into the commit message.

## 3. The tariff response

- [x] 3.1 Add `restricted` to `TariffDto` with `<summary>` and `<example>` by `.claude/rules/openapi-dto-docs.md`,
  filled for every member in `getPortalTariff`; update the remarks of `getPortalTariff`, `updateWalletPayment`,
  `getSubscriptionBalanceInfo`, `moveSubscriptionToWallet`, `startDocsCloudTrial`, `calculateDevPack`,
  `switchToDevPack` where they name the grace period; verified by a build of `ASC.Web.Api` with no new warnings.

## 4. Tests

- [x] 4.1 Unit test (`common/Tests/ASC.Core.Common.Tests`): `IsRestricted` and the lane state for Trial, Paid,
  Delay with and without a delayed method, NotPaid with and without one; proven red with `prove-red.sh`.
- [x] 4.2 Unit test of the last known answer: a delayed answer, a card answer, no customer, a failure after a delayed
  answer, a failure with no earlier answer; proven red.
- [x] 4.3 Extend the billing stub's `Payments.Customer(...)` with the delayed payment method as `GetCustomerInfo`
  returns it (`isDelayedPaymentMethod`), and its failure answer (500).
- [x] 4.4 Billing suite, new folder `Tests/GracePeriod/`: grace period with a delayed method — `getPortalTariff`
  `state` Delay and `restricted` false; `addMember` of a User and the invitation link of a Room admin answer 200.
  Grace period with a card — `restricted` true and 402 "Your pricing plan does not support this option". Grace
  period over with a delayed method — `state` NotPaid, `restricted` true, the gate answers 402. Done as
  `GracePeriodTests`: the counted action is the invitation link of a Room admin and `addMember` of a Room admin,
  which keeps that type (a User is checked by no count checker); the gate is checked on `GET settings/logo`.
- [x] 4.5 Room creation in the grace period with a delayed method (200) and with a card (402): add the Files service
  to the billing suite as a lazy resource, or, if the suite cannot start it, cover the room checker by the unit test
  of 4.1 and write the wiring as not proven in the commit. The suite starts no Files service: the room checker is
  covered by the unit test of 4.1, its wiring is not proven.
- [x] 4.6 Billing suite: billing answers the customer request with 500 after a delayed answer — `restricted` stays
  false; with no earlier answer — `restricted` true.
- [x] 4.7 Billing suite: switching the customer to a delayed method and calling `getCustomerInfo` with `refresh=true`
  makes the cached tariff report `restricted` false. The stack runs no socket service or router, so `PortalSocket`
  cannot connect there; the `change-tariff` event is checked by the unit test of 4.2.
- [x] 4.8 `Tests/Checkout/SubscriptionBalanceTests.cs:43-73`: keep the card case of the grace period (402 "Tariff is
  not paid") and add the delayed-method case (no 402); update the `verification.md` row of "Moving the subscription
  balance to the wallet".
- [x] 4.9 Tests citing the modified "Reading the tariff" (`BT/Tariff/TariffRoleTests.cs:45-101`,
  `web/ASC.Web.Api.Tests/.../Quota/PortalTariffTests.cs:41-62`, `Quota/PortalTariffPermissionsTests.cs:47-77`):
  assert `restricted` false on a paid plan, once the SDK carries the field (raw JSON until then). Done with a
  local build of the regenerated C# SDK package (`TariffDto.Restricted`, `.nuget/packages`), `GracePeriodTests`
  moved off raw JSON too; the SDK submodule pointers are not moved in this branch. A missing field reads as
  false, so the guard that every role gets it is a new case of `TariffRoleTests`: in a grace period with a card the
  owner, a Room admin, a User and a Guest read `restricted` true.
- [x] 4.10 Update the CODE-ONLY rows that describe the grace period refusals (`billing` "Payment gate…" grace row,
  `quotas` row of the grace / not-paid refusal, `backup-and-restore` "Running the schedule") with the new tests.
- [x] 4.11 Billing suite, `Tests/GracePeriod/GracePeriodPaymentsTests.cs`: the modified requirements of money
  operations with a delayed method and with a card — `updateWalletPayment` `Set` of an add-on (`false` / 402),
  `startDocsCloudTrial` (granted / 402), `calculateDevPack` (priced / 402); update their `verification.md` rows. The
  add-on renewal and the backup schedule decide the same way in background services this stack cannot drive: their
  decision is the unit test of 4.1, their wiring is not proven.
- [x] 4.12 Unit tests of 1.5 and of the last answer: a read of a cached tariff asks no billing and uses the cached
  customer; billing not configured takes no lock; an unreadable answer or a dropped connection after a delayed
  answer keeps it; the answer is cached for the grace days plus one (`RecordingProxy` over the cache). Billing suite:
  `getPortalTariff` with `refresh=true` after a switch to a delayed method reports `restricted` false.
- [x] 4.13 Review round: "cannot find" under a 5xx is an unknown outcome (`BillingClient`; unit tests of
  the client and of the last answer); a refresh whose tariff enters the grace period only after the payments are
  read again reads the customer from billing, as saving the tariff drops the cached customer (billing suite); a read of a cached tariff, before the tariff lock or after waiting for
  it, asks no billing and takes no customer lock (unit test through `GetTariffAsync`).
- [x] 4.14 Second review round: "cannot find" under 408 or 429 is an unknown outcome too (`BillingClient`, unit
  theory); when the earlier last answer cannot be read, `change-tariff` is queued once the new answer is kept, the
  read asking FusionCache to report a failure of the distributed level (unit tests with a failing read and with a
  failing distributed cache); the texts of `restricted`, `getPortalTariff`, the 402 texts of the member, owner,
  product-admin, room and room-from-template operations, and `createAgent` name the transitions that are refused.
- [x] 4.15 Billing suite: a payments request answered "cannot find" under 500, 408 and 429 leaves a paid tariff's
  due date as it was (scenario "Not-found answer under a failing status"), and under 200 the read takes it for a
  refund (`TariffStateTests.GetPortalTariff_BillingAnswersCannotFind_IsARefundOnlyWhenTheOutcomeIsKnown`). The
  stack does not save the due date of a refund (billing verification, "Refund"), so the refund case asserts only
  that the read moved the due date into the past.

## 5. Checks

- [x] 5.1 `dotnet build` of every touched project with `--no-dependencies --no-incremental` and `dotnet format style
  --verify-no-changes` on the edited files: no new warnings (the two `xUnit1051` of
  `LegacyAiProvidersMigrationGateTests.cs` come from the base).
- [x] 5.2 Run the new and touched test classes (billing suite, `ASC.Core.Common.Tests`, the Web.Api tariff tests);
  all green, except what fails without the change too: 11 billing tests of `TopUpDepositOutcomeTests`,
  `TopUpSettingsCurrencyTests`, `CustomerDepositsFailureTests`, `RestrictedAiModelsAccessTests` and
  `QuotaSettingsTariffTests` (4 of them marked `Category=Bug`) fail the same with the product code of the base, and
  `AiAgentsMigrationConfigurationTests` failed once in the parallel unit run and passes alone.
- [x] 5.3 `openspec validate --specs --no-interactive`, and `python -I -X utf8
  .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec <capability>` for `billing`, `quotas`,
  `backup-and-restore`, `storage-and-integrations`: every operation of the change is called by a test. No new
  operationId; the new field `restricted` of `getPortalTariff` is asserted by 4.4 and 4.9.

## Workflow follow-up

- Commit the regenerated SDK submodules (`sdk/docspace-api-sdk-csharp`, `sdk/docspace-api-spec`) and move their
  pointers; the OpenAPI documents and the package in `.nuget/packages` are regenerated in this branch.
- Tell the frontend: show the grace period by `state`, disable controls by `restricted`, re-read on `change-tariff`.
- Archive the change after the merge (`openspec-archive-change`).
