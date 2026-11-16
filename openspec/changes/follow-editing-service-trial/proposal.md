# Proposal

## Why

A self-hosted Enterprise installation set up next to an editing service that runs its own trial (no license file
anywhere) could not be set up: `getIsLicenseRequired` answered true, the wizard demanded a license file the
customer does not have yet, and `completeWizard` refused without one. The installation should instead run its own
trial for no longer than the editing service's, and switch to the license as soon as a file is accepted.

## What Changes

- `getIsLicenseRequired` answers false while no license file is on disk and the editing service reports an active
  trial of its own. **BREAKING** for a caller that read false as "a license file was accepted".
- `completeWizard` on such an installation completes without a license file: the installation's own trial tariff
  stays in force, its end brought to the day the editing service's trial ends (never later), and the license
  counts as accepted. No audit event is written: no license key was uploaded. If the editing service does not
  answer at that moment, the wizard completes on the installation's own 30-day trial.
- The daily background re-read follows the editing service's trial while no license file is on disk, also before
  the license is accepted and before the wizard is completed: it brings the trial's end closer, never later, and an
  expired trial of the editing service ends the installation's trial yesterday. **BREAKING** for existing
  Enterprise installations that run their 30-day trial without a license file.
- A license from a file is never replaced by the trial; a file accepted later replaces the trial as before.
- `acceptLicense` and `completeWizard` put the previous license file back on any failure before the license is
  applied, as the requirement already says; the server restored it only for a refused license.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `branding-and-licensing`: "Whether a license is still required" (`getIsLicenseRequired`,
  `GET api/2.0/settings/license/required`), "Activating a license" (`acceptLicense`,
  `POST api/2.0/settings/license/accept`, and the wizard), "Refreshing the license" (the background re-read next to
  `refreshLicense`, `GET api/2.0/settings/license/refresh`); new requirement "Following the editing service's
  trial" (`completeWizard`, `getPortalTariff`).
- `billing`: "Single tariff of a self-hosted installation" (`getPortalTariff`, `GET api/2.0/portal/tariff`).
- `portal-lifecycle`: "First-run wizard of a server installation" (`completeWizard`,
  `PUT api/2.0/settings/wizard/complete`).

## Breaks current behaviour

- **branding-and-licensing "Whether a license is still required"**: `getIsLicenseRequired` answers 200 false
  instead of true on an Enterprise or Developer installation without a license file whose editing service runs an
  active trial. The answer of the editing service may be up to 15 minutes old; while the service does not answer,
  its last answer of the hour is used. Affected: the web client wizard (it skips the license upload step; no client
  change is needed), agents reading the published text. The OpenAPI document and the SDKs carry the new
  description after regeneration; no field, status code or route changes. Webhooks, socket events, backup and
  restore, the portal migration and stored data: none.
- **branding-and-licensing "Activating a license"**: completing the setup wizard without a license file no longer
  applies a file: it keeps the installation's trial in force and records the acceptance without an audit event.
  `acceptLicense` itself is unchanged for its callers, except that a failure of any kind (not only a refused
  license) leaves the previous license file in place. Affected: the audit trail (no `LicenseKeyUploaded` entry for a
  wizard completed on the trial), the web client (none). Socket events: the quota events the tariff write already
  sends, as with a license. Backup, restore, migration: none.
- **branding-and-licensing "Refreshing the license"**: the background re-read now also runs without a license file
  and without acceptance, and changes the tariff's `dueDate` reported by `getPortalTariff` to the editing service's
  trial end when that is earlier. Affected: existing Enterprise installations on their 30-day trial without a
  license file (their trial can end earlier, or at once when the editing service's trial has expired), and an
  installation restored from a backup, whose license file the restore removes (it no longer gets 30 days when the
  editing service's own trial has expired). Socket events: the quota events of a tariff write. Webhooks, SDKs:
  none.
- **billing "Single tariff of a self-hosted installation"**: the 30-day trial tariff of an Enterprise installation
  without an activated licence ends no later than the editing service's trial. Affected: `getPortalTariff`
  (`dueDate`, and `state` once that day has passed) and the web client's trial banner and payment page, which read
  them; no client change is needed.
- **portal-lifecycle "First-run wizard of a server installation"**: `completeWizard` answers 200 instead of 500
  "You must enter a license key" on an Enterprise installation without a license file whose editing service runs a
  trial or does not answer. Affected: the web client wizard (it no longer offers the upload step there); SDKs and
  OpenAPI: description only.

Deviation T4 (plain exceptions of the wizard answer 500): kept as it is. The change adds no new 500: an editing
service that does not answer at completion leads to the installation's own trial, not to an error.

## Impact

- Code (already on branch `feature/trial-license`): `DocumentServiceLicense` and `DocsTrialCache` (Files.Core),
  `LicenseReader` and `TariffService` (Core.Common), `FirstTimeTenantSettings`, `LicenseController`,
  `SettingsController` (Web.Api), `RefreshLicenseService` (Files.Worker).
- API: no contract change; the descriptions of `getIsLicenseRequired`, `acceptLicense` and `completeWizard` change,
  so the OpenAPI documents and SDKs need regeneration.
- External dependency: the `license` command of the editing service (its answer while it runs its own trial).
