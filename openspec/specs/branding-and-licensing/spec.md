# branding-and-licensing Specification

## Purpose

Defines how a portal is branded and how a self-hosted installation is licensed. Branding covers the white
label of a portal — the logo slots (`settings/whitelabel/logos/*`), the logo text (`settings/whitelabel/logotext/*`),
the installation-wide default branding, the company details and help links of the installation
(`settings/rebranding/company`, `settings/rebranding/additional`), the colour themes (`settings/colortheme`),
the greeting title (`settings/greetingsettings`), the legacy portal logo (`settings/logo`) and the gate that
decides whether branding may be changed at all (`settings/enablewhitelabel`). Licensing covers the editions
the server knows (hosted, Enterprise, Developer, open source), the license file of an Enterprise or Developer
installation (`settings/license`, `settings/license/accept`, `settings/license/refresh`,
`settings/license/required`, the trial license), what the license writes into the installation's quota and
tariff, the edition flags `getPortalTariff` reports, the payment gate a lapsed license closes, and which
server features are gated by a quota flag of the edition. Billing, wallet, payments and the catalogue of
hosted plans are the `billing` specification; the grading of `getPortalTariff` and `getPortalQuota` by role,
the count and storage limits of a tariff are `quotas`; the letters that carry the branding are `notifications`.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`02_Settings/Greeting`, `02_Settings/Common`
colour theme, portal logo and payment settings, `04_Portal/Quota`) and the TypeScript suite they were ported
from (`settings/rebranding`, `settings/greeting`, `settings/common`), the OpenAPI contract
`sdk/docspace-api-spec/docspace-backend.yaml` (operations named by `operationId`), the product code where the
tests stop, the runtime configuration of the installation, and the release notes (v2.6, v3.0, v3.0.1, v3.1.1,
v3.2.1, v3.3, v3.7, v4.0). Where the notes or the OpenAPI document and the server disagree this specification
follows the server; the evidence per scenario, the differences and the suspected defects are in
`verification.md` next to this file. HTTP status codes are stated where the tests assert them.

## Requirements

### Requirement: Editions of the installation

The server SHALL run in one of four editions decided by the installation configuration, never by the portal:
hosted (a base domain is configured), Enterprise (self-hosted with a license file path), Developer (Enterprise
whose configured license type is developer) and open source (self-hosted without a license file path).
`getSettings` SHALL report `standalone` for the self-hosted editions, and `getPortalTariff` SHALL report
`openSource`, `enterprise` and `developer` to a caller with the portal-settings right only.

#### Scenario: Owner reads the edition flags

- **WHEN** the owner or a DocSpace admin calls `getPortalTariff`
- **THEN** the answer carries `enterprise`, `developer` and `openSource`, exactly one of them true on a self-hosted installation and all three false on a hosted portal

#### Scenario: Member without the settings right

- **WHEN** a Room admin, a User or a Guest calls `getPortalTariff`
- **THEN** the system answers 200 with `state` and `id` 0 and without the edition flags

### Requirement: Whether branding may be changed

`getEnableWhitelabel` SHALL require the portal-settings right (403 "Access denied") and SHALL answer true only
when the branding section is not hidden by the installation configuration and the portal's current quota
carries the `customization` feature. While it is false, `saveWhiteLabelSettings`,
`saveWhiteLabelSettingsFromFiles` and `saveWhiteLabelLogoText` SHALL answer 402 "Your pricing plan does not
support this option" after the permission check; the restore operations and every read SHALL stay available.

#### Scenario: Plan without customization

- **WHEN** the owner of a portal whose plan has no customization calls `getEnableWhitelabel`
- **THEN** the system answers 200 false
- **AND** `saveWhiteLabelLogoText` with `logoText` "Autotest Brand" and `saveWhiteLabelSettings` with an empty `logo` list answer 402

#### Scenario: Permission is checked before the plan

- **WHEN** a User calls `saveWhiteLabelLogoText` or `getEnableWhitelabel`
- **THEN** the system answers 403 "Access denied" whatever the plan

#### Scenario: Restore stays open

- **WHEN** the owner of the same portal calls `restoreWhiteLabelLogoText` or `restoreWhiteLabelLogos`
- **THEN** the system answers 200 true

### Requirement: Logo slots and their public listing

The portal SHALL have sixteen logo slots: the compact header logo, the login-page logo, the favicon, the left
menu logo, the About-page logo, the letter logo, and a normal and an embedded logo for each of the five
editors (text, spreadsheet, presentation, PDF, diagram). `getWhiteLabelLogos` SHALL need no authentication
and answer, in slot order, every slot but the letter one with `type`, `name`, `size` (`width`, `height` in
pixels) and `path` (`light`, `dark` absolute URLs); `isDark` fills one theme only, otherwise `dark` is empty
when it equals `light`.

#### Scenario: Anonymous lists the slots

- **WHEN** an anonymous caller calls `getWhiteLabelLogos` on an unbranded portal
- **THEN** the system answers 200 with 15 entries, none named `Notification`, each `path.light` pointing at the built-in image

#### Scenario: Slot sizes

- **WHEN** the list is read
- **THEN** the header logo is 422x48, the login-page logo 772x88, the favicon 32x32, the left menu logo 56x56, the About-page logo 442x48 and every editor logo 172x40

#### Scenario: Dark theme only

- **WHEN** `getWhiteLabelLogos` is called with `isDark=true`
- **THEN** every entry carries only `path.dark`

### Requirement: Saving logos from image data

`saveWhiteLabelSettings` SHALL require the portal-settings right and the branding gate and take `logo`, a list
of `key`/`value` pairs: `key` the numeric slot `type`, `value.light` and `value.dark` each a `data:` URI of a
PNG, JPEG or SVG image or the name of an image staged through the logo uploader. A slot not listed SHALL keep
its image; a dark image SHALL be stored only for the header, login-page, left menu, About-page and letter
slots; saving the login-page slot SHALL also rebuild the letter logo from it. `logoText` in the same body is
validated (400 over 40 characters) but not stored. Answer: 200 true.

#### Scenario: Header logo in both themes

- **WHEN** the owner of a portal with customization posts `logo` with one entry for the header slot carrying a light and a dark PNG data URI
- **THEN** the system answers 200 true, `getWhiteLabelLogos` returns portal URLs for both themes of that slot and the other slots are unchanged

#### Scenario: Dark image for a slot without a dark variant

- **WHEN** the entry names the favicon or an editor slot and carries `dark`
- **THEN** the light image is stored, the dark one is ignored and the slot is marked as customised

#### Scenario: Login-page logo rebuilds the letter logo

- **WHEN** the login-page slot is saved
- **THEN** the letter logo is regenerated from the new image and the cached letter logo is dropped, so the next letter carries it

### Requirement: Saving logos from uploaded files

`saveWhiteLabelSettingsFromFiles` SHALL require the portal-settings right and the branding gate and take a
`multipart/form-data` request whose file names carry the slot and the extension, as in `2.png`; a name
containing `dark` SHALL be stored as the dark image of that slot. A request without a file SHALL answer 403
"No input files" before the gate. The staging upload (`/logoUploader.ashx`, form fields `logotype`, `width`,
`height` and one image) SHALL accept `.bmp`, `.jpg`, `.jpeg`, `.ico`, `.png`, `.svg` not larger than the slot
and return the temporary file name for `saveWhiteLabelSettings`.

#### Scenario: No file in the form

- **WHEN** the owner posts `saveWhiteLabelSettingsFromFiles` without a file
- **THEN** the system answers 403

#### Scenario: Light and dark file for the left menu

- **WHEN** the owner posts files `6.svg` and `6.dark.svg`
- **THEN** the system answers 200 true and the left menu slot serves the first as `light` and the second as `dark`

### Requirement: Which slots hold a custom image

`getIsDefaultWhiteLabelLogos` SHALL require the portal-settings right (403 for a User) and answer one entry
per slot — all sixteen, the letter slot included — with `name` and `default`: true while the slot has never
been written, false once an image was stored for either theme. The letter slot SHALL report the state of the
login-page slot. `isDefault=true` SHALL inspect the installation-wide default branding instead.

#### Scenario: Fresh portal

- **WHEN** the owner calls `getIsDefaultWhiteLabelLogos` on a portal that never saved a logo
- **THEN** the system answers 200 with 16 entries, `Notification` among them, every `default` true

#### Scenario: After a save

- **WHEN** the login-page slot was saved
- **THEN** the entries `LoginPage` and `Notification` report `default` false and the others true

### Requirement: Restoring the built-in logos

`restoreWhiteLabelLogos` SHALL require the portal-settings right (403 "Access denied"), SHALL not depend on
the branding gate, SHALL delete every logo file stored for the portal, mark every slot as default, reset the
legacy portal logo and drop the cached letter logo, and answer 200 true also when nothing was stored. All
slots are reset together; the logo text is untouched.

#### Scenario: Owner restores an unbranded portal

- **WHEN** the owner calls `restoreWhiteLabelLogos` with nothing stored
- **THEN** the system answers 200 true

#### Scenario: Restore after a save

- **WHEN** logos were saved for the header and login-page slots and the owner calls `restoreWhiteLabelLogos`
- **THEN** `getWhiteLabelLogos` serves the built-in images again and `getIsDefaultWhiteLabelLogos` reports `default` true for the header, login-page and letter slots

### Requirement: Saving and clearing the logo text

`saveWhiteLabelLogoText` SHALL require the portal-settings right and the branding gate and store `logoText`
of at most 40 characters (400 when longer); an empty or blank value and the built-in wordmark `ONLYOFFICE`
SHALL clear the stored text instead. `restoreWhiteLabelLogoText` SHALL clear it without the gate. Both SHALL
answer 200 true, leave the logo images alone and write an audit event; the stored text is not translated.

#### Scenario: Custom wordmark

- **WHEN** the owner of a portal with customization saves `logoText` "Autotest Brand"
- **THEN** `getWhiteLabelLogoText` answers "Autotest Brand" and `getIsDefaultWhiteLabelLogoText` answers `name` `logotext`, `default` false

#### Scenario: Saving the built-in wordmark

- **WHEN** the owner saves `logoText` "ONLYOFFICE" or "   "
- **THEN** the system answers 200 true and `getIsDefaultWhiteLabelLogoText` still reports `default` true

#### Scenario: Text too long

- **WHEN** `logoText` has 41 characters
- **THEN** the system answers 400 and the stored text is unchanged

### Requirement: Reading the logo text

`getWhiteLabelLogoText` and `getIsDefaultWhiteLabelLogoText` SHALL require the portal-settings right (403 for
a User). The first SHALL answer the portal's stored text or `ONLYOFFICE` when none is stored — it SHALL NOT
fall back to the installation-wide default wordmark unless `isDefault=true` asks for it; the second SHALL
answer `default` true while the portal stores no text. `getSettings.logoText` SHALL carry the effective
wordmark: the portal's text, else the installation's default, else `ONLYOFFICE`.

#### Scenario: Owner reads an unbranded portal

- **WHEN** the owner calls `getWhiteLabelLogoText` and `getIsDefaultWhiteLabelLogoText`
- **THEN** the answers are "ONLYOFFICE" and `{ name: "logotext", default: true }`

#### Scenario: User is refused

- **WHEN** a User calls either operation
- **THEN** the system answers 403

### Requirement: Installation-wide default branding

The save and restore operations of the logos and the logo text SHALL accept `isDefault=true` to address the
default branding of the whole installation, which every portal inherits for the slots and the text it has not
customised. That SHALL be allowed only on a self-hosted installation whose space access is not limited and
that is not in custom mode (otherwise 403 "Access denied"); the saves additionally need the branding gate.
`getWhiteLabelLogos`, `getIsDefaultWhiteLabelLogos`, `getWhiteLabelLogoText` and `getIsDefaultWhiteLabelLogoText`
SHALL read it with the same query.

#### Scenario: Hosted portal asks for the installation branding

- **WHEN** the owner of a hosted portal calls `saveWhiteLabelLogoText` or `restoreWhiteLabelLogos` with `isDefault=true`
- **THEN** the system answers 403 "Access denied"

#### Scenario: Portal inherits the installation wordmark

- **WHEN** the installation's default `logoText` is "Reseller" and a portal has stored none
- **THEN** that portal's letters and `getSettings.logoText` say "Reseller" while `getWhiteLabelLogoText` without `isDefault` still answers "ONLYOFFICE"

### Requirement: Company details of the installation

`getCompanyWhiteLabelSettings` SHALL answer any signed-in member (401 anonymous), also on a portal whose
payment lapsed, with `companyName`, `site`, `email`, `address`, `phone`, `isLicensor`, `hideAbout` and
`isDefault`, honouring `If-Modified-Since` (304). `getLicensorData` SHALL answer a list holding the details
in effect and, when they were replaced and are not themselves marked as the licensor, the built-in vendor as
a second item. The values are installation-wide.

#### Scenario: Built-in vendor

- **WHEN** the owner reads `getCompanyWhiteLabelSettings` on an installation that never changed them
- **THEN** the answer carries the built-in vendor with `isLicensor` true and `isDefault` true, and `getLicensorData` is a single-item list

#### Scenario: User reads, anonymous is refused

- **WHEN** a User calls `getCompanyWhiteLabelSettings`, `getAdditionalWhiteLabelSettings` or `getLicensorData`
- **THEN** the system answers 200
- **AND** an anonymous caller gets 401

### Requirement: Changing and resetting the company details

`saveCompanyWhiteLabelSettings` SHALL require the portal-settings right, a self-hosted installation without
limited space access and not in custom mode (403 "Access denied"; a hosted portal is always refused), and the
branding gate (402). `settings` with `email` and `site` SHALL be required and valid (400); every field is
replaced, `isLicensor` is stored false, `hideAbout` is kept only when the quota carries `branding`. The answer
is 200 true. `deleteCompanyWhiteLabelSettings` SHALL need the same installation check but no gate and answer
the built-in details.

#### Scenario: Hosted owner

- **WHEN** the owner of a hosted portal calls `saveCompanyWhiteLabelSettings` with valid details or `deleteCompanyWhiteLabelSettings`
- **THEN** the system answers 403

#### Scenario: Hiding the About page needs the branding flag

- **WHEN** a self-hosted owner with customization saves `hideAbout` true while the quota has no `branding`
- **THEN** the system answers 200 true and `hideAbout` reads back false

#### Scenario: Reseller details

- **WHEN** valid details are saved on an installation whose plan allows it
- **THEN** `getCompanyWhiteLabelSettings` returns them with `isLicensor` false and `isDefault` false, and `getLicensorData` lists them first and the built-in vendor second

### Requirement: Help and community links of the installation

`getAdditionalWhiteLabelSettings` SHALL answer any signed-in member with `startDocsEnabled`,
`helpCenterEnabled`, `feedbackAndSupportEnabled`, `userForumEnabled`, `videoGuidesEnabled`,
`licenseAgreementsEnabled` and `isDefault`; the default of each link flag is true when the installation has
an address for it. `saveAdditionalWhiteLabelSettings` SHALL have the gates of the company details, require
`settings` (400), replace the whole set — an omitted flag becomes false — and answer true;
`deleteAdditionalWhiteLabelSettings` SHALL reset to the defaults and answer them.

#### Scenario: Defaults on a configured installation

- **WHEN** the owner reads `getAdditionalWhiteLabelSettings` on an installation with every link configured
- **THEN** all six flags are true and `isDefault` is true

#### Scenario: Hosted owner

- **WHEN** the owner of a hosted portal calls `saveAdditionalWhiteLabelSettings` with all flags false, or `deleteAdditionalWhiteLabelSettings`
- **THEN** the system answers 403

### Requirement: Where the branding appears and how it is served

The system SHALL use the slot images everywhere it draws a logo: the editors get the logo of their type and
embed mode, letters get the letter logo (the uploaded one, else the installation's default, else the built-in
image) and the effective logo text, `getSettings` carries `logoText` and `displayAbout` (false only on a
self-hosted installation whose quota carries `branding` and whose company details hide the About page).
Portal logo URLs SHALL carry a content hash and are cached immutably for a year by the proxy; built-in logos
carry the product version.

#### Scenario: Letter after a logo text change

- **WHEN** the portal's logo text is changed and a letter is sent afterwards
- **THEN** the letter names the new text and carries the letter logo of the portal

#### Scenario: About page hidden

- **WHEN** a self-hosted installation with the `branding` flag stores company details with `hideAbout` true
- **THEN** `getSettings.displayAbout` is false; on a hosted portal it is always true

#### Scenario: Replaced logo gets a new address

- **WHEN** the header logo is saved twice with different images
- **THEN** the two `path.light` URLs differ in their hash query, so a browser that cached the first one fetches the second

### Requirement: Reading the colour themes

`getPortalColorTheme` SHALL need no authentication and answer also on a suspended, blocked or not-paid
portal with `themes` (each `id`, `name`, `main.accent`, `main.buttons`, `text.accent`, `text.buttons`),
`selected` and `limit`, honouring `If-Modified-Since` (304). A fresh portal SHALL have six built-in themes
(blue, orange, green, red, purple, light-blue) with the first selected; `limit` is the installation's cap on
the number of themes, 0 meaning unlimited.

#### Scenario: Owner and members read the themes

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest calls `getPortalColorTheme`
- **THEN** the system answers 200 with a non-empty `themes`, `selected` of 0 or more and `limit` of 0 or more

### Requirement: Adding, changing and selecting a colour theme

`savePortalColorTheme` SHALL require the portal-settings right (403 "Access denied") and take `theme` and/or
`selected`. A `theme` whose `id` is a stored custom theme SHALL have its `main` and `text` replaced; any other
`id`, a built-in one included, SHALL add a new custom theme with the next free id and an empty `name` — but
only while the number of stored themes, built-in ones counted, is below `limit`; beyond it the theme is
dropped without an error. `selected` SHALL be applied only when that id exists. The answer is the full
configuration.

#### Scenario: Owner adds a theme

- **WHEN** the owner saves `theme` with `main` `#0F4071`/`#5299E0` and `text` `#FFFFFF`/`#FFFFFF`
- **THEN** the system answers 200 with `themes` one longer, the new theme carrying the highest `id`, and `limit` unchanged

#### Scenario: DocSpace admin adds a theme

- **WHEN** a DocSpace admin saves a theme
- **THEN** the system answers 200 and the theme is listed

#### Scenario: Limit reached

- **WHEN** the portal already holds `limit` themes and the owner saves a new one
- **THEN** the system answers 200 with the list unchanged

### Requirement: Deleting a colour theme

`deletePortalColorTheme` SHALL require the portal-settings right, take the theme `id` as a query parameter,
leave a built-in theme untouched (200 with the unchanged configuration), remove a custom theme and, when it
was the selected one, select the lowest remaining id. The answer is the configuration after the change.

#### Scenario: Owner deletes a custom theme

- **WHEN** the owner adds a theme and deletes it by its `id`
- **THEN** the system answers 200 with one theme fewer and no theme of that `id`

#### Scenario: DocSpace admin deletes a theme the owner added

- **WHEN** a DocSpace admin deletes a custom theme
- **THEN** the system answers 200 and the theme is gone

### Requirement: Greeting title of the portal

`getGreetingSettings` SHALL answer any signed-in member (401 anonymous) with the portal's title, or the
built-in caption when the portal has none; `getIsDefaultGreetingSettings` SHALL answer true exactly when the
portal has no title of its own. `getSettings` SHALL carry the same text as `greetingSettings`.

#### Scenario: Members read the title

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest calls `getGreetingSettings`
- **THEN** the system answers 200 with a non-empty string

#### Scenario: Fresh portal is default

- **WHEN** the owner or a DocSpace admin calls `getIsDefaultGreetingSettings` on a fresh portal
- **THEN** the system answers 200 true

#### Scenario: Anonymous

- **WHEN** an anonymous caller calls `getGreetingSettings` or `getIsDefaultGreetingSettings`
- **THEN** the system answers 401

### Requirement: Changing and restoring the greeting title

`saveGreetingSettings` SHALL require the portal-settings right (403 "Access denied", 401 anonymous), take a
required `title` of at most 255 characters (400), on a hosted free or trial portal refuse a title that breaks
the installation's character rule (400), store it as the portal name, write an audit event and answer a
confirmation message. `restoreGreetingSettings` SHALL need the same right and set the title back to the
installation's default portal name, answering the title now in force.

#### Scenario: Non-admin members are refused

- **WHEN** a Room admin, a User or a Guest calls `saveGreetingSettings` or `restoreGreetingSettings`
- **THEN** the system answers 403 "Access denied"

#### Scenario: Owner sets and restores a title

- **WHEN** the owner saves `title` "Custom Portal Greeting"
- **THEN** `getGreetingSettings` answers that text and `getIsDefaultGreetingSettings` false
- **AND** after `restoreGreetingSettings` the title is the default again and `getIsDefaultGreetingSettings` is true

### Requirement: Legacy portal logo

`getPortalLogo` SHALL answer any signed-in member with the absolute URL of the portal's company logo — the
built-in letter image when none was uploaded — honouring `If-Modified-Since` (304). `restoreWhiteLabelLogos`
SHALL reset it together with the slots.

#### Scenario: Every role reads the logo

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest calls `getPortalLogo`
- **THEN** the system answers 200 with a URL

### Requirement: Uploading a license file

`uploadLicense` SHALL be open to a DocSpace admin or to the setup-wizard confirmation link (403 when the link
is used after the wizard was completed), only on an Enterprise or Developer installation (415 otherwise), and
take a `multipart/form-data` request with the license file in `files` (400 when empty). The file SHALL be
parsed as JSON and staged next to the configured license path without being applied: it needs a customer id
and a signature (500 "License is not correct") and a start date not in the future (400). The answer is a
confirmation naming the support end when the end date already passed.

#### Scenario: Valid license staged

- **WHEN** a DocSpace admin uploads a license whose end date lies ahead
- **THEN** the system answers 200 "Uploaded successfully" and the installation's tariff is unchanged until the license is accepted

#### Scenario: Overdue license

- **WHEN** the uploaded license's end date lies in the past
- **THEN** the system answers 200 with "Uploaded successfully. Support and updates are not available for this license since <date>."

#### Scenario: Hosted or open-source installation

- **WHEN** `uploadLicense` is called where no license file path is configured
- **THEN** the system answers 415

### Requirement: Activating a license

`acceptLicense` SHALL, on an Enterprise or Developer installation, record the acceptance for the installation,
write an audit event and apply the staged license: the current file is backed up, the staged one moved into
place, confirmed by the editing service when it is reachable (the resource key and customer id must match and
the service must report success), and written as the installation's quota and tariff; on failure the previous
file is restored. The answer is 200 with an empty string on success or a sentence naming the failure; on
another edition the sentence says the plan does not support it. Completing the setup wizard SHALL do the same.

#### Scenario: Accepted after upload

- **WHEN** a license was uploaded and `acceptLicense` is called
- **THEN** the answer is "" and `getPortalTariff` reports the license's end date as `dueDate`, its customer as `customerId` and `state` Paid

#### Scenario: Nothing staged

- **WHEN** `acceptLicense` is called with no license file on disk
- **THEN** the answer is 200 "You must enter a license key"

#### Scenario: Editing service rejects the license

- **WHEN** the editing service reports a different customer id or an error for the license
- **THEN** the answer is 200 "License key is not correct" and the previous license file is back in place

### Requirement: What the license grants

Applying a license SHALL replace the installation's quota with one that has no count limits, keeps the
installation's file size cap, switches on `audit`, `ldap`, `sso`, `thirdparty`, `restore`, `oauth`,
`contentsearch`, `docs` and `statistic`, takes `branding`, `customization`, the automation API and `trial`
from the license, marks `lifetime` when the license is neither trial nor time-limited, and SHALL set the
tariff's `dueDate` to the license end date. The quota applies to every portal of the installation.

#### Scenario: Developer license

- **WHEN** a license with customization is applied
- **THEN** `getPortalQuota` reports `customization` true and `getEnableWhitelabel` answers true when the branding section is not hidden

#### Scenario: Enterprise license without customization

- **WHEN** a license without customization is applied
- **THEN** `getEnableWhitelabel` answers false and the branding saves answer 402

### Requirement: Refreshing the license

`refreshLicense` SHALL answer 200 false on an installation that is not Enterprise or Developer and otherwise
re-read the license file and rewrite the quota and tariff from it, answering true; a missing or unreadable
file, a validation failure by the editing service SHALL answer 402, a start date in the future 400. The
Files worker SHALL perform the same re-read periodically (once a day unless configured otherwise) whenever a
license path is configured, so a license file replaced on disk takes effect without a call.

#### Scenario: Open-source installation

- **WHEN** `refreshLicense` is called on an installation without a license path
- **THEN** the system answers 200 false

#### Scenario: File replaced on disk

- **WHEN** the license file is replaced by one with a later end date and the periodic re-read runs
- **THEN** `getPortalTariff` reports the new `dueDate`

### Requirement: Whether a license is still required

`getIsLicenseRequired` SHALL need no authentication and answer true only when the installation is Enterprise
or Developer, its tariff page is enabled (not an anyone-access portal, not an image-based deployment) and no
license file exists at the configured path; false in every other case, including a hosted portal.

#### Scenario: Fresh Enterprise installation

- **WHEN** the wizard page of a new Enterprise installation calls `getIsLicenseRequired`
- **THEN** the system answers 200 true until a license has been accepted

#### Scenario: Hosted portal

- **WHEN** `getIsLicenseRequired` is called on a hosted portal
- **THEN** the system answers 200 false

### Requirement: Trial license of a self-hosted installation

`POST /api/2.0/settings/license/trial` (not in the published contract) SHALL be refused on a hosted portal
(415) and for a caller who is not a DocSpace admin (403), SHALL answer false when the installation already
runs a trial, a non-default quota or a tariff with an end date, and otherwise SHALL create a 30-day trial
quota copying the current limits and features, set it as the installation's tariff, write an audit event and
answer true.

#### Scenario: Trial started once

- **WHEN** a DocSpace admin of a self-hosted installation on the default quota calls the trial operation twice
- **THEN** the first call answers true and `getPortalTariff` reports `state` Trial with a `dueDate` 30 days ahead; the second call answers false

### Requirement: Tariff state of a licensed installation and the payment gate

For a self-hosted installation the tariff SHALL be one for all portals: `state` Paid while the license end
date lies ahead, Delay for the configured grace period after it (`delayDueDate` = end date plus the period),
then NotPaid; a `lifetime` quota SHALL never leave Paid and a trial quota SHALL report Trial. While the state
is NotPaid and the tariff page is enabled, every operation not marked as allowed without payment SHALL answer
402 unless the request carries the `Payment-Info: false` header; `getPortalTariff`, the license operations and
the branding reads stay available.

#### Scenario: Grace period

- **WHEN** the license end date passed yesterday
- **THEN** `getPortalTariff` reports `state` Delay and `delayDueDate` ten days after the end date on the default configuration

#### Scenario: Grace period over

- **WHEN** `delayDueDate` has passed
- **THEN** `getPortalTariff` reports `state` NotPaid and a plain read such as `getPortalColorTheme` still answers while `getPortalQuota` answers 402

#### Scenario: Perpetual license

- **WHEN** the installation runs a license that is neither trial nor time-limited and its end date passed
- **THEN** `state` stays Paid

### Requirement: Payment settings

`getPaymentSettings` SHALL require the portal-settings right (403), stay available while the payment lapsed,
and answer `salesEmail`, `buyUrl` (the purchase address of the installation's edition), `standalone`,
`currentLicense` with `trial` and `dueDate` of the current tariff, and `max`, the largest quantity the
installation sells (999 unless configured).

#### Scenario: Owner and DocSpace admin read the settings

- **WHEN** the owner or a DocSpace admin calls `getPaymentSettings`
- **THEN** the system answers 200 with `salesEmail`, `buyUrl` and `currentLicense` present and `max` above 0

### Requirement: Features gated by a quota flag of the edition

The server SHALL gate a feature by a flag of the portal's current quota, never by the edition name:
`customization` gates the white-label saves (402), `branding` gates hiding the About page, `sso` gates the SSO
settings of a hosted portal (402), `thirdparty` gates the authorisation keys of paid third-party services on a
hosted portal, `statistic` the usage statistics; `getQuotaPaymentInformation` lists the flags as features. A
self-hosted installation SHALL carry the flags its license wrote; plugins and the colour themes SHALL have no
quota gate at all.

#### Scenario: SSO on a hosted plan without it

- **WHEN** the owner of a hosted portal whose quota lacks `sso` reads the SSO settings
- **THEN** the system answers 402

#### Scenario: Open-source installation

- **WHEN** the owner of an open-source installation calls `getEnableWhitelabel`
- **THEN** the answer follows the `customization` flag of the installation's default quota, not the edition

### Requirement: Audit trail of branding and license changes

Saving or restoring logos, saving or restoring the logo text, saving or deleting the company details, saving
or deleting the help links, changing the selected colour theme, saving the greeting title, and accepting or
starting a license SHALL each write one audit event; adding a colour theme without selecting it, deleting an
unselected theme and every read SHALL write none.

#### Scenario: Theme selected

- **WHEN** the owner saves `selected` with an existing theme id
- **THEN** the audit trail shows a colour theme change by the owner

#### Scenario: Theme added only

- **WHEN** the owner saves a `theme` without `selected`
- **THEN** no audit event is written
