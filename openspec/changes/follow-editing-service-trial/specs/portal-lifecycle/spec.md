# Spec Delta

## MODIFIED Requirements

### Requirement: First-run wizard of a server installation

While the wizard is not completed `getPortalSettings` SHALL hand an anonymous caller a `wizardToken`,
a confirmation link of type `Wizard` for the owner. `completeWizard` SHALL be reached with that link
only, require the portal-settings right (403), set the owner's password and e-mail and the portal
language and time zone, mark the wizard completed, sign the owner in and answer the settings with
`completed` true.

#### Scenario: Body of the wizard call

- **WHEN** `completeWizard` receives `email`, `passwordHash`, `lng`, `timeZone`, `amiId` and `subscribeFromSite`
- **THEN** the owner's password and e-mail are set, and a changed address becomes unconfirmed
- **AND** the portal language and time zone are taken from `lng` and `timeZone`
- **AND** on an Enterprise installation the licence is accepted

#### Scenario: Enterprise installation without a licence file

- **WHEN** `completeWizard` is called on an Enterprise installation without a licence file whose editing service runs an active trial, and the earlier of its end and the installation's own trial end is today or later
- **THEN** the answer is 200 with `completed` true, the licence is accepted and the trial of `branding-and-licensing` "Following the editing service's trial" is in force

#### Scenario: Enterprise installation without a licence file, editing service silent

- **WHEN** `completeWizard` is called on such an installation, the editing service does not answer, the installation's own trial is in force and has not ended, and the editing service last reported a trial or never answered
- **THEN** the answer is 200 with `completed` true, the licence is accepted and the installation keeps its own trial with its current end

#### Scenario: Enterprise installation without a licence file, file still required

- **WHEN** `completeWizard` is called on such an installation in any other case
- **THEN** the call fails as before and the wizard stays open

#### Scenario: Wizard completed

- **WHEN** the owner opens the wizard link and calls `completeWizard` with a valid `email`, a `passwordHash`, `lng` "de-DE" and `timeZone` "Europe/Berlin"
- **THEN** the answer is 200 with `completed` true, `getPortalInformation` reports `language` "de-DE" and the owner is signed in

#### Scenario: Wizard already completed

- **WHEN** `completeWizard` is called on a portal whose wizard is completed
- **THEN** the answer is 500 "Wizard passed." and nothing changes

#### Scenario: Empty password or malformed e-mail

- **WHEN** `completeWizard` is called with `passwordHash` set to an empty string
- **THEN** the answer is 500 with the empty-password message and the wizard stays open
- **AND** a body whose `email` is malformed or missing, or that has no `passwordHash` at all, is refused with 400
