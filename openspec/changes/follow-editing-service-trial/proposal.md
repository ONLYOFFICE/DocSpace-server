# Proposal

## Why

A self-hosted Enterprise installation set up next to an editing service that runs its own trial (no license file
anywhere) could not be set up: `getIsLicenseRequired` answered true, the wizard demanded a license file the
customer does not have yet, and `completeWizard` refused without one. The installation should instead run its own
trial for no longer than the editing service's, and switch to the license as soon as a file is accepted.

## What Changes

- `getIsLicenseRequired` answers false while no license file is on disk, the editing service reports an active
  trial of its own and the installation's own trial has not ended. **BREAKING** for a caller that read false as "a
  license file was accepted".
- `completeWizard` on such an installation completes without a license file: the installation's own trial tariff
  stays in force, its end brought to the day the editing service's trial ends (never later), and the license
  counts as accepted. No audit event is written: no license key was uploaded. If the installation's own trial has
  already ended, nothing is written and a license file is required as before. If the editing service does not
  answer at that moment, the wizard completes on the installation's own trial, whose end stays as it is until the
  next background re-read. A license file is still required, as before, when that trial is not in force or has
  already ended, or when the last answer known said the editing service runs no trial, that its trial ended, or that
  it runs a trial that would leave no trial in force today once applied; then the call fails and does not complete
  the wizard. With no answer known the wizard page asks for a file, but the wizard still completes on the own trial,
  by decision.
- The daily background re-read follows the editing service's trial while no license file is at the license path (a
  staged upload does not count) and the
  installation runs the trial it creates on its first tariff read (not one activated on request, and nothing before
  that first read), also before the license is accepted and before the wizard is completed, also while an uploaded
  license waits to be accepted: it brings the trial's end closer, never later, and an
  expired trial of the editing service ends the installation's trial no later than yesterday (earlier, when the
  editing service's trial ended earlier). **BREAKING** for existing Enterprise installations that run their 30-day
  trial without a license file.
- A license from a file is never replaced by the trial; a file accepted later replaces the trial as before. With no
  distributed lock provider (Redis or ZooKeeper) the writers of the license quota and tariff are ordered per process
  only, so the worker and the API may interleave.
- An answer of the editing service whose body is `null` says nothing about its trial: it is unknown, not "no trial".
- `acceptLicense` and `completeWizard` put the previous license file back on any failure before the license is
  applied, as the requirement already says; the server restored it only for a refused license.
- The answer of the editing service about its trial is kept in the distributed cache, one value per installation.
  With Redis, the background re-read refreshes it for every API node, and the last answer survives a restart for 3
  days; without Redis, each process keeps its own copy. The period of the re-read is read as `d.hh:mm:ss`, one day by
  default, between a minute and a day (a third of the 3 days, so the last answer outlives one re-read that fails); a
  longer period configured before is cut to a day.
- Every write of the tariff (a saved change, a scheduled quantity, a deleted tariff) sends the new socket event
  `s:change-tariff` to the quota room `{tenant}-quota` of the portal, and of every active portal for the shared
  tariff of a self-hosted installation. It carries no data: the client reads the tariff again. A page loaded later
  needs no client change; a page that is already open reads the new tariff only once the frontend subscribes to the
  event in the `quota` room. The quota values a tariff write already sent (`change-quota-feature-value`) now go to
  the same portals the same way, instead of the portal of the current request only.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `branding-and-licensing`: "Whether a license is still required" (`getIsLicenseRequired`,
  `GET api/2.0/settings/license/required`), "Activating a license" (`acceptLicense`,
  `POST api/2.0/settings/license/accept`, and the wizard), "Uploading a license file" (`uploadLicense`,
  `POST api/2.0/settings/license`), "Refreshing the license" (`refreshLicense`, `GET api/2.0/settings/license/refresh`),
  "Audit trail of branding and license changes"; new requirement "Following the editing service's trial"
  (`completeWizard`, `getPortalTariff`, and the background re-read next to `refreshLicense`).
- `billing`: "Single tariff of a self-hosted installation" (`getPortalTariff`, `GET api/2.0/portal/tariff`); new
  requirement "Tariff writes are announced" (the socket events of every tariff write).
- `portal-lifecycle`: "First-run wizard of a server installation" (`completeWizard`,
  `PUT api/2.0/settings/wizard/complete`).

## Breaks current behaviour

- **branding-and-licensing "Whether a license is still required"**: `getIsLicenseRequired` answers 200 false
  instead of true on an Enterprise or Developer installation without a license file whose editing service runs an
  active trial, unless the installation's own trial already ended. The answer of the editing service may be up to 15
  minutes old; while the service does not answer, its last answer of the past 3 days is used (with Redis; without
  it, the last answer this process knows). Affected: the web client wizard (it skips the license upload step; no
  client change is needed), agents reading the published text. The OpenAPI document and the SDKs carry the new
  description after regeneration; no field, status code or route changes. Webhooks, socket events, backup and
  restore and the portal migration: none. Stored data: the call reads the tariff through its cache, and on a miss,
  when a trial license quota exists without a tariff, that read creates the 30-day trial, as the first read of any
  page does. Without Redis the answer may follow an end another process moved only once the tariff cache expires (up
  to 15 minutes).
- **branding-and-licensing "Activating a license"**: completing the setup wizard without a license file no longer
  applies a file: it keeps the installation's trial in force and records the acceptance without an audit event.
  `acceptLicense` itself is unchanged for its callers, except that a failure of any kind (not only a refused
  license) leaves the previous license file in place. Affected: the audit trail (no `LicenseKeyUploaded` entry for a
  wizard completed on the trial), the web client (none). Socket events: the quota events the tariff write already
  sends, as with a license, and the new `s:change-tariff`. Backup, restore, migration: none.
- **branding-and-licensing "Uploading a license file"** and **"Refreshing the license"**: a staged upload still does
  not change the tariff, but while it waits to be accepted the end of a trial of "Following the editing service's
  trial" may be brought closer by the re-read, so "the tariff is unchanged" no longer holds on such an installation.
  Affected: none beyond the trial's `dueDate`; no field, status code or route changes.
- **branding-and-licensing "Audit trail of branding and license changes"**: completing the setup wizard on the
  editing service's trial counts as accepting the license and writes no audit event (no `LicenseKeyUploaded`).
  Affected: the audit trail.
- **billing "Tariff writes are announced"** (new): every tariff write, and its deletion, sends `s:change-tariff`
  and the quota feature values (`change-quota-feature-value`) to the quota room of each portal the tariff applies to.
  Before, the quota feature values went to the portal of the current request only, and a write of the default tariff
  of a hosted installation sent them to that request's portal; now it sends nothing. Affected: the web client (the
  quota values it already handles; `s:change-tariff` once it subscribes).
- **branding-and-licensing "Following the editing service's trial"** (new, it covers the background re-read next to
  `refreshLicense`): the re-read now also runs without a license file and without acceptance, and changes the
  tariff's `dueDate` reported by `getPortalTariff` to the editing service's trial end when that is earlier.
  Affected: existing Enterprise installations on their 30-day trial without a license file (their trial can end
  earlier, or at once when the editing service's trial has expired), and an installation restored from a backup,
  whose license file the restore removes (it no longer gets 30 days when the editing service's own trial has
  expired). Socket events: the quota events of a tariff write, and the new `s:change-tariff` in the
  `{tenant}-quota` room of every active portal; a tariff that becomes not paid only because its day has passed sends
  no event. Webhooks, SDKs: none.
- **billing "Single tariff of a self-hosted installation"**: the 30-day trial tariff of an Enterprise installation
  without an activated licence ends no later than the editing service's trial, once the wizard was completed during
  the editing service's active trial or the daily re-read has run. Affected: `getPortalTariff` (`dueDate`, and
  `state` once that day has passed) and the web client's trial banner and payment page, which read them when the
  page loads; no client change is needed for that.
- **portal-lifecycle "First-run wizard of a server installation"**: `completeWizard` answers 200 instead of 500
  "You must enter a license key" on an Enterprise installation without a license file in two cases. Either its
  editing service runs an active trial and the installation's own trial has not ended; or the editing service does
  not answer, the installation's own trial is in force and has not ended, and the editing service last reported an
  active trial whose end, applied, would leave that trial in force today, or never answered. The silent path reads
  the tariff first, which on a fresh installation creates the 30-day trial before the call decides. Affected: the web
  client wizard (it no longer offers the upload step there); SDKs and OpenAPI: description only.

Deviation T4 (plain exceptions of the wizard answer 500): kept as it is. The change adds no new 500: an editing
service that does not answer at completion leads to the installation's own trial, not to an error.

## Impact

- Code (already on branch `feature/trial-license`): `DocumentServiceLicense` and `DocsTrialCache` (Files.Core),
  `LicenseReader`, `TariffService`, `SocketServiceClient` and `QuotaSocketManager` (Core.Common),
  `FirstTimeTenantSettings`, `LicenseController`, `SettingsController` (Web.Api), `RefreshLicenseService`
  (Files.Worker), the `change-tariff` route and hub function of the socket service (`common/ASC.Socket.IO`).
- Configuration: `files:refreshLicense:period` is read as an interval, `d.hh:mm:ss`, one day by default, at least a
  minute and at most a day; no configuration in the repository or in `buildtools/config` sets it.
- Socket events: new `s:change-tariff` in the `{tenant}-quota` room, no payload; the web client does not subscribe
  to it yet.
- API: no contract change; the descriptions of `getIsLicenseRequired`, `acceptLicense` and `completeWizard` and the
  200 response text of `getIsLicenseRequired` change, so the OpenAPI documents and SDKs need regeneration.
- External dependency: the `license` command of the editing service (its answer while it runs its own trial).
