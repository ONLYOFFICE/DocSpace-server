# Tasks

The server code of this change is already on branch `feature/trial-license`; the groups mark it done with the
checks that proved it, and leave open what the specs, the documents and the tests still need.

## 1. Reading the editing service's trial

- [x] 1.1 Classify the answer of the `license` command as active, expired, not a trial or unknown, taking a trial only with `trial` set and the end as a UTC day; verified by `DocsTrialClassificationTests` in `common/Tests/ASC.Core.Common.Tests` on real answers of the editing service 10.0.1, proven red with `.claude/scripts/prove-red.sh`
- [x] 1.2 Cache the answer in the distributed cache (the default `IFusionCache`: memory, Redis, backplane), one value per installation, reused for 15 minutes and kept by fail-safe for 3 days; with Redis the worker refreshes it for every API node and it survives a restart, without Redis each process keeps its own copy. A known fresh answer replaces it, an unknown one leaves it alone, and a reader's failure does not replace an answer another process stored meanwhile; the readers of a process wait for the one request in flight as long as it may run (lock timeout = hard timeout, not the cache's 5-second default), and a failure with no answer known is stored under their lock before they go on; the period of the re-read is read as `d.hh:mm:ss` between a minute and a day, a third of the 3-day fail-safe. Verified by `DocsTrialCacheTests` (the services' default entry options over an in-memory `IDistributedCache` with the System.Text.Json serializer, another process and a restart included), proven red for the fresh unknown answer, the fail-safe copy, the lock timeout, the failure stored under the lock, the failure kept only for the failure duration and leaving no copy once expired, the failure of a reader while another process stores an answer, and the bound of the period

## 2. Writing the trial

- [x] 2.1 Keep or create the installation's own trial quota and bring its tariff end closer by day, never later, never over a license from a file, under the lock the license-file writer takes too; verified by `LicenseReaderTests` (`ChooseTrialDueDate`, license file on disk writes nothing), proven red
- [x] 2.2 Put the license files back on any failure before the license is applied (requirement "Activating a license"); verified by `LicenseReaderTests.RefreshLicenseAsync_ActiveAndStagedFiles_RestoresActiveOnAnyFailure`, proven red
- [x] 2.3 Follow the trial on the daily background re-read without a license file, accepted or not; checked on a local Enterprise stand (the re-read restored the editing service's end after it was moved in the database)

## 3. Wizard and `getIsLicenseRequired`

- [x] 3.1 Answer `getIsLicenseRequired` false while the editing service reports an active trial and no license file exists; checked on a local Enterprise stand, including a frozen editing service
- [x] 3.2 Complete the wizard without a license file on the trial, record the acceptance, write no audit event, and keep the installation's own trial with its current end when the editing service does not answer, unless that trial is not in force or has ended, or the last answer known said no trial or an ended one; checked on a local Enterprise stand on a clean database (acceptance recorded, own trial kept); the silent-service path is checked in 3.3
- [x] 3.3 Check the silent-service path of 3.2 on a local Enterprise stand with the editing service frozen at completion; done when `completeWizard` answers 200 and `getPortalTariff` reports the installation's own `dueDate` unchanged
- [x] 3.4 Take the editing service's trial in `getIsLicenseRequired`, in the wizard and in the silent-service fallback only when the end it leads to is today or later, so a trial that would already be over asks for a license file as before, and the wizard writes nothing for it; the re-read still writes a passed end; the anonymous question reads the cached tariff, not the stored one. Verified by `LicenseReaderTests` (`WillTrialBeInForce`, and `IsOwnTrialInForceAsync`, `WillDocsTrialBeInForceAsync` and `DocsTrialToDBAsync` on an own trial that ended, over stubbed quotas and tariff, and the cached tariff read with the arguments that keep it in the cache), each proven red; the wizard's own call order is not proven by a test (Web.Api has no unit tests here)

## 4. Documents and contract texts

- [x] 4.1 Describe the trial in the descriptions of `getIsLicenseRequired`, `acceptLicense` and `completeWizard` within the sentence rules of `.claude/rules/openapi-endpoint-docs.md`; checked by sentence counts (at most 20 words, average at most 12)
- [x] 4.2 Regenerate the API documents (stage A of the `generate-sdk` skill) and check that only these three descriptions differ in `json/*_2.0.json`

## 5. Specs and verification

- [x] 5.1 Update `openspec/specs/branding-and-licensing/verification.md`: the wizard row ("Wizard completion refreshes the license and records acceptance"), the periodic re-read row ("only after acceptance"), the audit row (`LicenseKeyUploaded` without the wizard on the trial), and a row for "Following the editing service's trial" citing the unit tests of groups 1 and 2 as CODE-ONLY plus the stand checks; done when every row of the modified requirements names the current code
- [x] 5.2 Update `openspec/specs/billing/verification.md` (the 30-day `start_trial` row) and `openspec/specs/portal-lifecycle/verification.md` (the wizard rows) for the trial; done when both rows name the trial path
- [x] 5.3 Re-run the integration tests that cite the modified requirements and confirm they still pass on the open-source stack: `web/ASC.Web.Api.Tests/Tests/02_Settings/License/LicenseOpenSourceTests.cs`, `LicensePermissionsTests.cs` and `web/ASC.Web.Api.Tests/Tests/04_Portal/Lifecycle/PortalWizardTests.cs` (`run-tests` skill); run 2026-11-17: 24 of 31 passed; the 7 red ones are the 6 `[Trait("Category", "Bug")]` cases of `LicensePermissionsTests` (red while their defect is open, as on the base) and the `buyUrl` case, red because the local config had `license.type` set to `developer`
- [ ] 5.4 Integration tests for "Following the editing service's trial", the new scenarios of the modified requirements and the `s:change-tariff` event, following `.claude/rules/tests.md`. Blocked: no test profile starts an Enterprise installation with an editing service on its trial. Until such a profile exists, the requirement stays CODE-ONLY in `openspec/specs/branding-and-licensing/verification.md` with the missing profile as the reason

## 6. Telling the open clients

- [x] 6.1 Send `s:change-tariff` without data to the `{tenant}-quota` room after every write of the tariff (`SaveBillingInfoAsync`, `UpdateNextQuantityAsync`, `DeleteDefaultBillingInfoAsync`), to every active portal for the shared tariff of tenant -1, together with the quota values of the write, built apart so that a quota that cannot be read leaves the tariff event in place, with failed sends logged once and the write kept; the event takes the state of the tariff just written and never reads the tariff, since a write may run under the lock of the tariff read: `QuotaSocketManager.ChangeTariffAsync`, a `SocketServiceClient.MakeRequest` overload that takes the state, `TariffService.NotifyTariffChangedAsync`, and the `change-tariff` route and hub function of `common/ASC.Socket.IO`. Verified by `QuotaSocketManagerTests` (the tariff event and a quota value, boxed as the tariff service hands it over, are queued for the given portal with the given state while any read of the tariff fails), proven red; the calls from `TariffService` are not proven by a test (no unit test builds the tariff service)

## 7. Final check

- [x] 7.1 Run `openspec validate --specs --no-interactive` and `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec branding-and-licensing`, `--spec billing` and `--spec portal-lifecycle`; the change adds no operation, so no new operationId needs a test, and none of the touched operations may be left uncalled

## Workflow follow-up

- Check on a local Enterprise stand that the API and the worker share the cached answer of the editing service (the key of `DocsTrialCache.GetCacheKey()`), and that an open trial banner reads the new `dueDate` after `s:change-tariff`.
- Web client: subscribe to `s:change-tariff` in the `quota` room and read `GET api/2.0/portal/tariff` again (`SocketEvents` in `../client/libs/ui-kit/utils/socket/index.ts`).
- Archive the change with `openspec-archive-change` after the branch is merged, so the deltas reach the main specs.
