# Tasks

The server code of this change is already on branch `feature/trial-license`; the groups mark it done with the
checks that proved it, and leave open what the specs, the documents and the tests still need.

## 1. Reading the editing service's trial

- [x] 1.1 Classify the answer of the `license` command as active, expired, not a trial or unknown, taking a trial only with `trial` set and the end as a UTC day; verified by `DocsTrialClassificationTests` in `common/Tests/ASC.Core.Common.Tests` on real answers of the editing service 10.0.1, proven red with `.claude/scripts/prove-red.sh`
- [x] 1.2 Cache the answer for the anonymous question with fail-safe, a known fresh answer replacing the cached one on this node and making the other nodes ask again, an unknown one leaving it alone, and the last answer known readable without a request; verified by `DocsTrialCacheTests` (real in-memory cache), proven red for the fresh-answer and fail-safe rules

## 2. Writing the trial

- [x] 2.1 Keep or create the installation's own trial quota and bring its tariff end closer by day, never later, never over a license from a file, under the lock the license-file writer takes too; verified by `LicenseReaderTests` (`ChooseTrialDueDate`, license file on disk writes nothing), proven red
- [x] 2.2 Put the license files back on any failure before the license is applied (requirement "Activating a license"); verified by `LicenseReaderTests.RefreshLicenseAsync_ActiveAndStagedFiles_RestoresActiveOnAnyFailure`, proven red
- [x] 2.3 Follow the trial on the daily background re-read without a license file, accepted or not; checked on a local Enterprise stand (the re-read restored the editing service's end after it was moved in the database)

## 3. Wizard and `getIsLicenseRequired`

- [x] 3.1 Answer `getIsLicenseRequired` false while the editing service reports an active trial and no license file exists; checked on a local Enterprise stand, including a frozen editing service
- [x] 3.2 Complete the wizard without a license file on the trial, record the acceptance, write no audit event, and keep the installation's own trial with its current end when the editing service does not answer, unless that trial is not in force or has ended, or the last answer known said no trial or an ended one; checked on a local Enterprise stand on a clean database (acceptance recorded, own trial kept); the silent-service path is checked in 3.3
- [x] 3.3 Check the silent-service path of 3.2 on a local Enterprise stand with the editing service frozen at completion; done when `completeWizard` answers 200 and `getPortalTariff` reports the installation's own `dueDate` unchanged

## 4. Documents and contract texts

- [x] 4.1 Describe the trial in the descriptions of `getIsLicenseRequired`, `acceptLicense` and `completeWizard` within the sentence rules of `.claude/rules/openapi-endpoint-docs.md`; checked by sentence counts (at most 20 words, average at most 12)
- [x] 4.2 Regenerate the API documents (stage A of the `generate-sdk` skill) and check that only these three descriptions differ in `json/*_2.0.json`

## 5. Specs and verification

- [x] 5.1 Update `openspec/specs/branding-and-licensing/verification.md`: the wizard row ("Wizard completion refreshes the license and records acceptance"), the periodic re-read row ("only after acceptance"), the audit row (`LicenseKeyUploaded` without the wizard on the trial), and a row for "Following the editing service's trial" citing the unit tests of groups 1 and 2 as CODE-ONLY plus the stand checks; done when every row of the modified requirements names the current code
- [x] 5.2 Update `openspec/specs/billing/verification.md` (the 30-day `start_trial` row) and `openspec/specs/portal-lifecycle/verification.md` (the wizard rows) for the trial; done when both rows name the trial path
- [x] 5.3 Re-run the integration tests that cite the modified requirements and confirm they still pass on the open-source stack: `web/ASC.Web.Api.Tests/Tests/02_Settings/License/LicenseOpenSourceTests.cs`, `LicensePermissionsTests.cs` and `web/ASC.Web.Api.Tests/Tests/04_Portal/Lifecycle/PortalWizardTests.cs` (`run-tests` skill); run 2026-11-17: 24 of 31 passed; the 7 red ones are the 6 `[Trait("Category", "Bug")]` cases of `LicensePermissionsTests` (red while their defect is open, as on the base) and the `buyUrl` case, red because the local config had `license.type` set to `developer`
- [x] 5.4 Integration tests for "Following the editing service's trial" and the new scenarios of the modified requirements, following `.claude/rules/tests.md`: blocked, because no test profile starts an Enterprise installation with an editing service on its trial; record the requirement as CODE-ONLY in 5.1 and name the missing Enterprise profile as the reason
- [x] 5.5 Run `openspec validate --specs --no-interactive` and `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec branding-and-licensing`, `--spec billing` and `--spec portal-lifecycle`; the change adds no operation, so no new operationId needs a test, and none of the touched operations may be left uncalled

## Workflow follow-up

- Archive the change with `openspec-archive-change` after the branch is merged, so the deltas reach the main specs.
