# Spec Delta

## ADDED Requirements

### Requirement: Following the editing service's trial

With no license file at the license path (a staged upload aside) and no license from a file in force, an Enterprise
or Developer installation SHALL end the trial of its first tariff read no later than the editing service's trial
ends, never moving the end later, and by yesterday once that trial expired. The daily re-read SHALL apply this,
accepted or not, and so SHALL a setup wizard completed during that trial, counting as acceptance. A license from a
file SHALL never be replaced by it.

#### Scenario: Wizard completed on the editing service's trial

- **WHEN** `completeWizard` is called on an Enterprise installation without a license file whose editing service reports an active trial ending on day D, and the earlier of day D and the installation's own trial end is today or later
- **THEN** the answer is 200 with `completed` true and `getPortalTariff` reports a `dueDate` on or before day D
- **AND** the license counts as accepted and no audit event is written

#### Scenario: Installation trial already ended

- **WHEN** `completeWizard` is called on such an installation whose own trial ended before today, for example after the editing service was reinstalled with a fresh trial
- **THEN** nothing is written, the call fails as without the editing service's trial, and the wizard stays open until a license file is given

#### Scenario: Editing service silent at completion

- **WHEN** `completeWizard` is called on such an installation, the editing service does not answer, the installation's own trial is in force and has not ended, and the last known answer of the editing service was an active trial whose end, applied, would leave that trial in force today, or none is known
- **THEN** the answer is 200 with `completed` true and the installation keeps its own trial tariff with its current end

#### Scenario: Editing service silent, a license file still required

- **WHEN** `completeWizard` is called without a license file, the editing service does not answer, and either no trial of the installation is in force, it has ended, or the last known answer was no trial, an ended one, or an active trial that would leave no trial in force today once applied
- **THEN** the call fails as without the editing service's trial, and the wizard stays open until a license file is given

#### Scenario: Before the first re-read

- **WHEN** an Enterprise installation without a license file runs its own trial, the wizard was not completed during an active trial of the editing service, and the daily background re-read has not run yet
- **THEN** `getPortalTariff` reports the installation's own trial end, which may be later than the editing service's

#### Scenario: No license file, before acceptance

- **WHEN** the periodic re-read runs on an Enterprise installation without a license file, before the wizard is completed, and the editing service reports a trial ending before the installation's trial
- **THEN** `getPortalTariff` reports the editing service's trial end day as `dueDate`

#### Scenario: Trial activated on request

- **WHEN** the daily background re-read runs on an installation that runs the trial activated on request, not the one created on the first tariff read
- **THEN** the tariff is unchanged

#### Scenario: Tariff not read yet

- **WHEN** the daily background re-read runs on an Enterprise installation whose tariff was never read, so it has no trial yet
- **THEN** nothing is written, and the trial created on the first read is followed from the next re-read on

#### Scenario: Editing service trial ends later

- **WHEN** the editing service reports a trial ending later than the installation's trial, for example after the editing service was reinstalled
- **THEN** `getPortalTariff` keeps the earlier `dueDate`

#### Scenario: Same day

- **WHEN** the installation's trial ends later on the same day the editing service's trial ends
- **THEN** `getPortalTariff` keeps the installation's `dueDate`

#### Scenario: Editing service trial expired

- **WHEN** the daily background re-read runs and the editing service reports its trial as expired
- **THEN** `getPortalTariff` reports as `dueDate` the earliest of the installation's trial end, the day the editing service's trial ended and yesterday, and the installation is not paid

#### Scenario: Editing service does not answer the re-read

- **WHEN** the daily background re-read runs and the editing service does not answer or its answer cannot be read
- **THEN** the tariff is unchanged

#### Scenario: License from a file in force

- **WHEN** a license file was accepted and the editing service reports a trial
- **THEN** `getPortalTariff` keeps the license's `dueDate` and `customerId`

## MODIFIED Requirements

### Requirement: Whether a license is still required

`getIsLicenseRequired` SHALL need no authentication and answer true only when the installation is Enterprise
or Developer, its tariff page is enabled (not an anyone-access portal, not an image-based deployment), no
license file exists at the configured path and no trial of "Following the editing service's trial" would be in force
today; false in every other case, including a hosted portal. The editing service's answer MAY be 15 minutes old, or 3
days while it does not answer.

#### Scenario: Fresh Enterprise installation

- **WHEN** the wizard page of a new Enterprise installation whose editing service runs no trial calls `getIsLicenseRequired`
- **THEN** the system answers 200 true until a license has been accepted

#### Scenario: Fresh Enterprise installation on the editing service's trial

- **WHEN** the wizard page of a new Enterprise installation without a license file calls `getIsLicenseRequired` while the editing service reports an active trial
- **THEN** the system answers 200 false

#### Scenario: Installation trial already ended

- **WHEN** `getIsLicenseRequired` is called on such an installation while the editing service reports an active trial, but the installation's own trial ended before today
- **THEN** the system answers 200 true

#### Scenario: Editing service does not answer

- **WHEN** `getIsLicenseRequired` is called on such an installation and the editing service does not answer
- **THEN** the system answers from the editing service's last answer of the past 3 days, and with none 200 true

#### Scenario: Trial end moved by another process

- **WHEN** the background re-read in another process brings the installation's trial end closer, and either there is no Redis to share the tariff cache or a read of the tariff that started before the write fills the cache after it
- **THEN** `getIsLicenseRequired` may answer from the previous, later end until the tariff cache of the process that answers expires, up to 15 minutes

#### Scenario: Hosted portal

- **WHEN** `getIsLicenseRequired` is called on a hosted portal
- **THEN** the system answers 200 false

### Requirement: Activating a license

`acceptLicense` SHALL, on an Enterprise or Developer installation, record the acceptance, write an audit event and
apply the staged license: the current file is backed up, the staged one moved into place, confirmed by the editing
service when reachable and written as the installation's quota and tariff; on failure the previous file is restored.
The answer is 200 with an empty string on success, else a sentence naming the failure. The setup wizard SHALL do the
same with a license file.

#### Scenario: Accepted after upload

- **WHEN** a license was uploaded and `acceptLicense` is called
- **THEN** the answer is "" and `getPortalTariff` reports the license's end date as `dueDate`, its customer as `customerId` and `state` Paid

#### Scenario: Accepted during the editing service's trial

- **WHEN** the installation runs the trial of "Following the editing service's trial", and a license is uploaded and `acceptLicense` is called
- **THEN** the answer is "" and `getPortalTariff` reports the license's end date as `dueDate`, which later background re-reads keep

#### Scenario: Other edition

- **WHEN** `acceptLicense` is called on a hosted portal or an open-source installation
- **THEN** the answer is 200 "Your pricing plan does not support this option"

#### Scenario: Nothing staged

- **WHEN** `acceptLicense` is called with no license file on disk
- **THEN** the answer is 200 "You must enter a license key"

#### Scenario: Editing service rejects the license

- **WHEN** the editing service reports a different customer id or an error for the license
- **THEN** the answer is 200 "License key is not correct" and the previous license file is back in place

### Requirement: Uploading a license file

`uploadLicense` SHALL be open to a DocSpace admin or to the setup-wizard confirmation link (403 once the wizard is
completed), only on an Enterprise or Developer installation (415 otherwise), and take `multipart/form-data` with the
license file in `files` (400 when empty). The file SHALL be parsed as JSON and staged beside the license path, not
applied: it needs a customer id and a signature (500 "License is not correct") and a start date not in the future
(400). It answers a confirmation text.

#### Scenario: Valid license staged

- **WHEN** a DocSpace admin uploads a license whose end date lies ahead
- **THEN** the system answers 200 "Uploaded successfully" and the upload does not change the installation's tariff; only accepting the license does, while the end of a trial of "Following the editing service's trial" may still be brought closer

#### Scenario: Overdue license

- **WHEN** the uploaded license's end date lies in the past
- **THEN** the system answers 200 with "Uploaded successfully. Support and updates are not available for this license since <date>."

#### Scenario: Hosted or open-source installation

- **WHEN** `uploadLicense` is called where no license file path is configured
- **THEN** the system answers 415

### Requirement: Refreshing the license

`refreshLicense` SHALL answer 200 false on an installation that is not Enterprise or Developer and otherwise re-read
the license file and rewrite the quota and tariff from it, answering true; a missing or unreadable file or an
editing-service refusal SHALL answer 402, a start date in the future 400. After acceptance a background re-read of the
license in force SHALL run daily by default, so a replaced file takes effect without a call; it SHALL NOT apply an
uploaded license.

#### Scenario: Open-source installation

- **WHEN** `refreshLicense` is called on an installation without a license path
- **THEN** the system answers 200 false

#### Scenario: File replaced on disk

- **WHEN** the license file is replaced by one with a later end date and the periodic re-read runs
- **THEN** `getPortalTariff` reports the new `dueDate`

#### Scenario: Uploaded license not accepted

- **WHEN** a license was uploaded with `uploadLicense` and not accepted, and the periodic re-read runs
- **THEN** the uploaded license stays staged and does not change the tariff; only `acceptLicense` or `refreshLicense` puts it in force, while the end of a trial of "Following the editing service's trial" may still be brought closer

### Requirement: Audit trail of branding and license changes

Saving or restoring logos, saving or restoring the logo text, saving or deleting the company details, saving
or deleting the help links, changing the selected colour theme, saving the greeting title, and accepting or
starting a license SHALL each write one audit event; adding a colour theme without selecting it, deleting an
unselected theme, completing the setup wizard on the editing service's trial and every read SHALL write none.

#### Scenario: Theme selected

- **WHEN** the owner saves `selected` with an existing theme id
- **THEN** the audit trail shows a colour theme change by the owner

#### Scenario: Theme added only

- **WHEN** the owner saves a `theme` without `selected`
- **THEN** no audit event is written

#### Scenario: Wizard completed on the editing service's trial

- **WHEN** `completeWizard` completes without a license file on the trial of "Following the editing service's trial"
- **THEN** the license counts as accepted and no audit event is written
