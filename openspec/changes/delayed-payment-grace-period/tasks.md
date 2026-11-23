# Tasks

## 1. The restriction decision

- [ ] 1.1 Add `DelayedPaymentMethod` (worked out on every read, so neither cached nor serialized) and the computed
  `IsRestricted` and lane state to the tariff (`common/ASC.Core.Common/Billing/Tariff.cs`); verified by the unit test of 4.1.
- [ ] 1.2 Write the last known `isDelayedPaymentMethod` on every answer of billing to the customer request (false
  for "cannot find", a portal that is not a customer) to its own key of the distributed cache, kept for the grace days plus one; write nothing on a failure;
  keep the key out of the tariff cache clearing. Verified by the unit test of 4.2 and the integration test of 4.6.
- [ ] 1.3 When the written answer differs from the stored one (absent counts as false), send `change-tariff` to the
  portal's quota room after the write, best effort in its own `try/catch` with a `[LoggerMessage]` warning, without
  reading the tariff. Verified by the integration test of 4.7.
- [ ] 1.4 In the state computation, fill `DelayedPaymentMethod` from that key when the state is Delay on a hosted
  installation (after the cached customer read), reset it to false otherwise. Verified by 4.3 and 4.5.

## 2. The checks

- [ ] 2.1 Replace `State > Paid` with `IsRestricted` in the count checkers (`CountPaidUserChecker`,
  `CountUserChecker`, `CountRoomChecker`, `CountAIAgentChecker`); verified by 4.4 and 4.5.
- [ ] 2.2 Replace the state checks of `updateWalletPayment` (add-on), the subscription balance helper (Trial or
  restricted), `startDocsCloudTrial`, the Dev Pack preparation and the add-on wallet renewal with `IsRestricted`;
  verified by 4.8 and by the unchanged tests of those operations.
- [ ] 2.3 Run the backup schedule when the tariff is not restricted instead of below Delay; verified by the unit test
  of 4.1 (the decision) and the backup suite staying green.
- [ ] 2.4 Pass the lane state instead of `State` from the socket client, the audit event handler, the thumbnail
  handler and the tariff change notification; verified by build and the unit test of 4.1.
- [ ] 2.5 `findReferences` on the tariff state once more over the solution: no check that refuses an action in the
  grace period is left on `State`; the list of the ones deliberately kept (letters, retention, audit cleaner,
  payment URL, billing sync) goes into the commit message.

## 3. The tariff response

- [ ] 3.1 Add `restricted` to `TariffDto` with `<summary>` and `<example>` by `.claude/rules/openapi-dto-docs.md`,
  filled for every member in `getPortalTariff`; update the remarks of `getPortalTariff`, `updateWalletPayment`,
  `getSubscriptionBalanceInfo`, `moveSubscriptionToWallet`, `startDocsCloudTrial`, `calculateDevPack`,
  `switchToDevPack` where they name the grace period; verified by a build of `ASC.Web.Api` with no new warnings.

## 4. Tests

- [ ] 4.1 Unit test (`common/Tests/ASC.Core.Common.Tests`): `IsRestricted` and the lane state for Trial, Paid,
  Delay with and without a delayed method, NotPaid with and without one; proven red with `prove-red.sh`.
- [ ] 4.2 Unit test of the last known answer: a delayed answer, a card answer, no customer, a failure after a delayed
  answer, a failure with no earlier answer; proven red.
- [ ] 4.3 Extend the billing stub's `Payments.Customer(...)` with the delayed payment method as `GetCustomerInfo`
  returns it (`isDelayedPaymentMethod`), and its failure answer (500).
- [ ] 4.4 Billing suite, new folder `Tests/GracePeriod/`: grace period with a delayed method — `getPortalTariff`
  `state` Delay and `restricted` false; `addMember` of a User and the invitation link of a Room admin answer 200.
  Grace period with a card — `restricted` true and 402 "Your pricing plan does not support this option". Grace
  period over with a delayed method — `state` NotPaid, `restricted` true, the gate answers 402.
- [ ] 4.5 Room creation in the grace period with a delayed method (200) and with a card (402): add the Files service
  to the billing suite as a lazy resource, or, if the suite cannot start it, cover the room checker by the unit test
  of 4.1 and write the wiring as not proven in the commit.
- [ ] 4.6 Billing suite: billing answers the customer request with 500 after a delayed answer — `restricted` stays
  false; with no earlier answer — `restricted` true.
- [ ] 4.7 Billing suite, with `PortalSocket`: switching the customer to a delayed method and calling `getCustomerInfo`
  with `refresh=true` delivers `change-tariff` to the quota room and `getPortalTariff` reports `restricted` false.
- [ ] 4.8 `Tests/Checkout/SubscriptionBalanceTests.cs:43-73`: keep the card case of the grace period (402 "Tariff is
  not paid") and add the delayed-method case (no 402); update the `verification.md` row of "Moving the subscription
  balance to the wallet".
- [ ] 4.9 Tests citing the modified "Reading the tariff" (`BT/Tariff/TariffRoleTests.cs:45-101`,
  `web/ASC.Web.Api.Tests/.../Quota/PortalTariffTests.cs:41-62`, `Quota/PortalTariffPermissionsTests.cs:47-77`):
  assert `restricted` false on a paid plan, once the SDK carries the field (raw JSON until then).
- [ ] 4.10 Update the CODE-ONLY rows that describe the grace period refusals (`billing` "Payment gate…" grace row,
  `quotas` row of the grace / not-paid refusal, `backup-and-restore` "Running the schedule") with the new tests.

## 5. Checks

- [ ] 5.1 `dotnet build` of every touched project with `--no-dependencies --no-incremental` and `dotnet format style
  --verify-no-changes` on the edited files: no new warnings.
- [ ] 5.2 Run the new and touched test classes (billing suite, `ASC.Core.Common.Tests`, the Web.Api tariff tests);
  all green.
- [ ] 5.3 `openspec validate --specs --no-interactive`, and `python -I -X utf8
  .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec <capability>` for `billing`, `quotas`,
  `backup-and-restore`, `storage-and-integrations`: every operation of the change is called by a test. No new
  operationId; the new field `restricted` of `getPortalTariff` is asserted by 4.4 and 4.9.

## Workflow follow-up

- Regenerate the OpenAPI documents and the SDKs (`generate-sdk`) so `TariffDto` carries `restricted`.
- Tell the frontend: show the grace period by `state`, disable controls by `restricted`, re-read on `change-tariff`.
- Archive the change after the merge (`openspec-archive-change`).
