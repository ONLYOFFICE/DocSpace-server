# portal-lifecycle Specification

## Purpose

Defines the portal (tenant) as an object of the server, from birth to removal: how a portal is
registered through the system API (`apisystem`: `register`, `validateportalname`, `remove`,
`status`, `get`), what name it may carry, the first-run wizard a server installation finishes with
`completeWizard`, the information endpoints under `portal` (`getPortalInformation`, `getPortalPath`,
`getUserById`), the rename (`PUT portal/portalrename`), the owner change (`sendOwnerChangeInstructions`,
`updatePortalOwner`), the owner-driven suspension and deletion (`sendSuspendInstructions`,
`suspendPortal`, `continuePortal`, `sendDeleteInstructions`, `deletePortal`,
`DELETE portal/deleteportalimmediately`), the status values a portal can be in and what a portal in
each of them answers, the confirmation links these flows run on, and the retention policy of a hosted
installation that blocks an idle or unpaid portal, lets the owner unblock it (`PUT portal/unblock`) and
removes it at the end of its retention. Sign-in itself, billing, quotas (`getPortalUsedSpace`,
`getPortalUsersCount`, `getPortalQuota` are in `quotas`), invitation links (`invitations`), backup
(`backup-and-restore`) and the texts of the letters (`notifications`) are other capabilities and are
referenced only.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`04_Portal/Settings`, `04_Portal/Users`,
`02_Settings/Owner`) and the TypeScript suite they were ported from, the shared test harness that
registers one portal per test through the system API, the retention suite of
`common/Tests/ASC.Notify.Tests` (`Periodic/Retention`), the OpenAPI contracts
`sdk/docspace-api-spec/docspace-backend.yaml` (operations named by `operationId`) and the system API
document, the product code of the portal, owner, wizard and system-API controllers, the registration
and removal services and the retention job, and the release notes (v1.1, v3.0, v3.1, v3.1.1, v3.7).
Where the notes or the OpenAPI documents and the server disagree this specification follows the
server; the evidence per scenario, the differences and the suspected defects are in `verification.md`
next to this file.

## Requirements

### Requirement: Registering a portal through the system API

`register` SHALL create a portal with its owner from `portalName`, `firstName`, `lastName`, `email`,
`password` or `passwordHash`, `language`, `timeZoneName`, optional `region`/`awsRegion`, `spam`,
`calls`, `skipWelcome`, and answer 200 with `reference` (a sign-in link for the owner) and `tenant`
(`tenantId`, `portalName`, `domain`, `status` "Active", `ownerId`, `language`, time zone). The owner is
the only member and a DocSpace admin. 400 names the failing rule in `error`; 500 "registerNewTenantError"
when the portal cannot be created.

#### Scenario: Test harness registers a portal

- **WHEN** the system API receives `register` with a free `portalName`, owner name, e-mail, password, `language` "en-US", `timeZoneName` "UTC" and `skipWelcome` true
- **THEN** the answer is 200 with `tenant.tenantId` above zero, `tenant.ownerId` and `tenant.status` "Active", and the owner signs in to the new portal with that e-mail and password

#### Scenario: Weak password

- **WHEN** `password` is given and does not meet the installation's password policy
- **THEN** the answer is 400 with `error` "passPolicyError"

#### Scenario: Owner name missing

- **WHEN** `firstName` and `lastName` are both empty
- **THEN** the answer is 400 with `error` "error" and the message "name is required"

### Requirement: Portal name on a hosted and on a server installation

On a server installation the requested `portalName` SHALL be used, and an empty one replaced by a
generated name. On a hosted installation the requested name SHALL be ignored and a name of the form
`<prefix>-<6 random lower-case letters or digits>` generated, retried up to 100 times while taken;
`registerbyemail` and `registerbyoauth` always generate the name. When the generated name cannot be
made unique the answer SHALL be 400 "portalNameNotAvailable".

#### Scenario: Hosted registration with a chosen name

- **WHEN** a hosted installation receives `register` with `portalName` "my-company"
- **THEN** the portal is created under a generated name and `tenant.portalName` is not "my-company"

#### Scenario: Server registration with a chosen name

- **WHEN** a server installation receives `register` with `portalName` "my-company" and the name is free
- **THEN** `tenant.portalName` is "my-company" and `tenant.domain` starts with it

### Requirement: Rules a portal name has to pass

`validateportalname` and every registration SHALL trim and lower-case the name and refuse it with 400
and `error` "portalNameEmpty" when empty, "tooShortError" when shorter than 6 or longer than 63
characters, "portalNameIncorrect" when it is not lower-case letters, digits and hyphens starting and
ending with a letter or digit, or looks like punycode, and "portalNameExist" with `variants` (the taken
names sharing the stem) when a portal, a mapped domain or a reserved address already uses it. A
free name answers 200 "portalNameReadyToRegister".

#### Scenario: Name already taken

- **WHEN** `validateportalname` is called with the alias of an existing portal
- **THEN** the answer is 400 with `error` "portalNameExist", the message "Portal already exists" and `variants` listing the taken names

#### Scenario: Name too short

- **WHEN** `validateportalname` is called with "abc"
- **THEN** the answer is 400 with `error` "tooShortError"

#### Scenario: Name with an underscore or a capital letter

- **WHEN** `validateportalname` is called with "My_Portal"
- **THEN** the lower-cased "my_portal" is refused with 400 "portalNameIncorrect"

### Requirement: Abuse protection of registration

Registrations and portal sign-ins from one network address SHALL be counted in a sliding window; above
the installation's limit the answer is 400 "tooMuchAttempts" (sign-in: 403 without a CAPTCHA answer,
401 with a wrong one). On an installation that requires a CAPTCHA, `register` and `provision` SHALL
refuse a missing or wrong `recaptchaResponse` with 400 "recaptchaInvalid" unless a known `appKey` is
sent. An owner address matching the installation's test-address pattern SHALL bypass both.

#### Scenario: Test suite registers hundreds of portals

- **WHEN** the integration tests register one portal per test with the owner address the installation lists as a test address
- **THEN** no registration is refused for too many attempts and no CAPTCHA is demanded

### Requirement: Trial tariff, cache and welcome of a new portal

Right after creation the server SHALL set the portal's Content Security Policy for a new portal, on a
hosted installation record the portal's domain and region in the hosting cache and its billing key,
apply the installation's trial tariff when one is configured, and, when a password was supplied,
hand the owner the welcome mail through the portal (the `reference` then carries `first=true`; with
`skipWelcome` the mail address is returned as `referenceWelcome` instead of being sent). A portal
registered without a password SHALL start with the wizard pending.

#### Scenario: Registration without a password on a server installation

- **WHEN** a server installation registers a portal with neither `password` nor `passwordHash`
- **THEN** the portal is created with the wizard not completed and the owner has to finish it before working

### Requirement: First-run wizard of a server installation

While the wizard is not completed `getPortalSettings` SHALL hand an anonymous caller a `wizardToken`,
a confirmation link of type `Wizard` for the owner. `completeWizard` SHALL be reached with that link
only, require the portal-settings right (403), take `email`, `passwordHash`, `lng`, `timeZone`,
`amiId`, `subscribeFromSite`, set the owner's password and e-mail (a changed address becomes
unconfirmed), the portal language and time zone, accept the licence on an Enterprise installation,
mark the wizard completed, sign the owner in and answer the settings with `completed` true.

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

### Requirement: Portal information

`getPortalInformation` SHALL answer 200 to every signed-in member and 401 to an anonymous caller. The
owner and a DocSpace admin get the whole record: `tenantId`, `tenantAlias`, `name`, `ownerId`,
`language`, `timeZone`, `industry`, `status`, `statusChangeDate`, `creationDateTime`, `lastModified`,
`version`, `mappedDomain`, trusted domains, `region` (hosted installation with the cache on). A Room
admin, a User or a Guest get an object in which only `tenantId` is filled. The call SHALL keep
answering while the portal's payment has lapsed.

#### Scenario: Owner or DocSpace admin reads the portal

- **WHEN** the owner or a DocSpace admin calls `getPortalInformation`
- **THEN** the answer is 200 with `tenantId` above zero, a non-empty `ownerId`, `language` and `timeZone`, and `creationDateTime` and `lastModified` set

#### Scenario: Lower types get the id only

- **WHEN** a Room admin, a User or a Guest calls `getPortalInformation`
- **THEN** the answer is 200 with `tenantId` above zero, `calls` false, an empty `ownerId` and default `creationDateTime` and `lastModified`

#### Scenario: Anonymous caller

- **WHEN** nobody is signed in and `getPortalInformation` is called
- **THEN** the answer is 401

### Requirement: Portal path

`getPortalPath` SHALL answer, to every signed-in member, 200 with the absolute address of the portal
followed by the optional `virtualPath`; without `virtualPath` the portal's own address. Anonymous: 401.

#### Scenario: Owner asks for a section address

- **WHEN** the owner calls `getPortalPath` with `virtualPath` "/rooms"
- **THEN** the answer is 200 with an `http(s)://<portal>/rooms` string

#### Scenario: Every member may read it

- **WHEN** a DocSpace admin, a Room admin, a User or a Guest calls `getPortalPath` without a path
- **THEN** the answer is 200 with an `http(s)://` address; an anonymous caller gets 401

### Requirement: Looking up a portal account

`getUserById` (`portal/users/{userId}`) SHALL return the account in the internal user format to a
caller allowed to view that account, answer 403 "Access denied" to a User or a Guest for any id but
their own and to a Room admin for a guest they have no relation with, and 404 "User not found" for a
system account. The counts `getPortalUsersCount` and `getPortalUsedSpace` need the portal-settings
right (403 "Access denied", anonymous 401) and are specified in `quotas`.

#### Scenario: Counts need the settings right

- **WHEN** a Room admin, a User or a Guest calls `getPortalUsersCount`
- **THEN** the answer is 403 "Access denied", while the owner and a DocSpace admin get the number of active accounts

### Requirement: Renaming a portal

`PUT portal/portalrename` with `alias` SHALL require the portal-settings right (403), a server
installation or a tariff with customization (else 402 "Your pricing plan does not support this
option"), and a non-empty `alias` without whitespace (400). The trimmed, lower-cased name SHALL pass the
portal-name rules (400 with the rule's message), else the alias changes, the audit event "Portal
renamed" is written, the Content Security Policy and hosting cache follow the new domain, every other
active member is signed out, the caller's session ends and a sign-in link to the new address is
returned. The same name again returns an empty string.

#### Scenario: Owner renames the portal

- **WHEN** the owner calls `PUT portal/portalrename` with `alias` "New-Name" and the name is free
- **THEN** the answer is 200 with a confirmation sign-in link under `new-name.<base domain>`, `getPortalInformation` on the new address reports `tenantAlias` "new-name" and every other member has been signed out

#### Scenario: Name taken

- **WHEN** the owner renames the portal to the alias of another portal
- **THEN** the answer is 400 and the alias is unchanged

#### Scenario: Hosted portal without customization

- **WHEN** the owner of a hosted portal on a tariff without customization calls `PUT portal/portalrename`
- **THEN** the answer is 402 "Your pricing plan does not support this option"

### Requirement: Status values of a portal

A portal SHALL be in exactly one status: Active, Suspended, Remove pending, Transfering, Restoring,
Migrating, Encryption or Blocked; `getPortalInformation` SHALL report it as `status` and the moment it
last changed as `statusChangeDate`, and `getPortalSettings` as `tenantStatus`. A Suspended or
Remove-pending portal SHALL answer 404 to every request but the ones marked as allowed while suspended
(`continuePortal`, the colour theme); a Blocked portal 404 but the ones allowed while blocked
(`unblockPortal`, the colour theme); Transfering, Restoring and Encryption 403 but the progress,
settings, theme, logo, build and `@self` paths.

#### Scenario: Blocked portal answers almost nothing

- **WHEN** a portal is Blocked and a client calls `getSupportedCultures` and `getPortalColorTheme`
- **THEN** the cultures answer 404 and the colour theme 200

#### Scenario: Suspended portal

- **WHEN** a portal is Suspended and a member calls `getPortalInformation`
- **THEN** the answer is 404 until `continuePortal` is called with the owner's link

### Requirement: Confirmation links of the lifecycle flows

The lifecycle operations that change a portal SHALL be authorized by a confirmation link mailed to
the portal owner rather than by a session: `suspendPortal` by a PortalSuspend link, `continuePortal`
by PortalContinue, `deletePortal` by PortalRemove, `updatePortalOwner` by PortalOwnerChange,
`unblockPortal` by PortalUnblock and `completeWizard` by Wizard. The link's `type`, `key`, `uid` and
`encemail` travel in the `confirm` header, the link is valid for the installation's e-mail-link period
(7 days by default), signs its holder in as the owner for that call, and a missing or wrong link
SHALL answer 401; an ordinary sign-in token 403.

#### Scenario: Ordinary token instead of the link

- **WHEN** the owner, signed in normally, calls `updatePortalOwner`, `unblockPortal` or `suspendPortal` without a confirmation link
- **THEN** the answer is 403, and 401 when nobody is signed in

#### Scenario: Link of another kind

- **WHEN** a PortalContinue link is sent to `suspendPortal`
- **THEN** the answer is 401

### Requirement: Asking for the portal to be suspended

`sendSuspendInstructions` SHALL require the portal-settings right and the caller to be the portal
owner: a DocSpace admin, a Room admin, a User or a Guest get 403 "Access denied", anonymous 401. It
SHALL mail the owner the deactivation letter with the PortalSuspend and PortalContinue links, write
the audit event "Owner sent portal deactivation instructions", change nothing else and answer 200
without a body; it keeps working while the payment has lapsed and is limited to 5 calls per 15
minutes (429). On a server installation the last space with full access cannot be suspended (500).

#### Scenario: Owner asks

- **WHEN** the owner calls `sendSuspendInstructions`
- **THEN** the answer is 200 and the portal stays Active

#### Scenario: DocSpace admin asks

- **WHEN** a DocSpace admin calls `sendSuspendInstructions`
- **THEN** the answer is 403 "Access denied"

#### Scenario: Other members and anonymous

- **WHEN** a Room admin, a User or a Guest calls `sendSuspendInstructions`, or nobody is signed in
- **THEN** the answer is 403 "Access denied", 401 for the anonymous caller

### Requirement: Suspending and continuing a portal

`suspendPortal` with the PortalSuspend link SHALL check again that the link's account is the owner
(403 "Access denied") and that it is not the last full-access space of a server installation (500),
set the status to Suspended with the current `statusChangeDate`, write "Portal deactivated" and
refresh the portal's base domain; content, rooms and accounts stay. `continuePortal` with the
PortalContinue link SHALL set the status back to Active, re-apply the Content Security Policy and
answer 200; an already active portal is left active. Both answer without a body.

#### Scenario: Owner suspends and brings the portal back

- **WHEN** the owner opens the suspend link, then the continue link from the same letter
- **THEN** after the first call `getPortalInformation` answers 404 and after the second it answers 200 with `status` Active and the content unchanged

#### Scenario: Repeating the suspend link

- **WHEN** the suspend link is used a second time within its validity
- **THEN** the portal is Suspended again with a new `statusChangeDate`

### Requirement: Asking for the portal to be deleted

`sendDeleteInstructions` SHALL require the portal-settings right and the owner: a DocSpace admin, a
Room admin, a User or a Guest get 403 "Access denied", anonymous 401; the last full-access space of a
server installation 500. It SHALL mail the owner the deletion letter with the PortalRemove link (with
the auto-renew note when a hosted portal has payments and is not on trial), change nothing, write no
audit event, answer 200 without a body, keep working while the payment has lapsed and be limited to 5
calls per 15 minutes.

#### Scenario: Owner asks

- **WHEN** the owner calls `sendDeleteInstructions`
- **THEN** the answer is 200 and the portal stays Active

#### Scenario: DocSpace admin asks

- **WHEN** a DocSpace admin, a Room admin, a User or a Guest calls `sendDeleteInstructions`
- **THEN** the answer is 403 "Access denied"; an anonymous caller gets 401

### Requirement: Deleting a portal

`deletePortal` with the PortalRemove link SHALL check the owner (403) and the last-space rule (500),
remove the portal's OAuth clients, rename the alias to `<alias>_deleted` (a number appended while
taken) and set the status Remove pending, drop the domain from the hosting cache and the CSP cache,
mail the owner "has been deleted" and, for a paying hosted portal, the support address, write "Portal
deleted", and queue the purge: every storage module, the database row and the search index of the
portal. It SHALL answer 200 with the address of the "registration cancelled" feedback page. Nothing
can undo it.

#### Scenario: Owner confirms the deletion

- **WHEN** the owner opens the PortalRemove link
- **THEN** the answer is 200 with the feedback address, the old address answers 404 at once, and shortly afterwards the portal's files, database rows and search entries are gone

#### Scenario: Letter failure does not stop the removal

- **WHEN** the deletion letters cannot be sent
- **THEN** the portal is still set to Remove pending and the purge still starts

### Requirement: Immediate deletion for test portals

`DELETE portal/deleteportalimmediately` SHALL let a signed-in owner delete the portal without a link
when the owner's e-mail matches the installation's test-address pattern, doing what `deletePortal`
does and answering 200 with the feedback address. Any other owner SHALL get 403 "Access denied", a
non-owner 403, anonymous 401. The operation is not in the published API document.

#### Scenario: Test owner removes the portal

- **WHEN** the signed-in owner of a portal registered with a test address calls `DELETE portal/deleteportalimmediately`
- **THEN** the answer is 200 and the portal is Remove pending

#### Scenario: Ordinary owner

- **WHEN** an owner whose address is not a test address calls it
- **THEN** the answer is 403 "Access denied" and nothing changes

### Requirement: Removing and switching a portal through the system API

On a server installation `remove` (by `portalName` or `tenantId`) SHALL delete a portal whose wizard
is not completed at once and otherwise mail the owner the PortalRemove link, answering 200 with the
`tenant` and `removed` (true only for the immediate case); the last full-access space answers 400
"The last full access space cannot be deleted.", a missing or unknown name 400. `status` SHALL set
Active for `status` Active and Suspended for anything else. `get` SHALL list Active portals (with
owner, usage and wizard state when `statistics` is true). On a hosted installation all three answer
403 "Method for server edition only.".

#### Scenario: Portal that never finished the wizard

- **WHEN** `remove` names a server-installation portal whose wizard is not completed
- **THEN** the answer is 200 with `removed` true and the portal is gone without a letter

#### Scenario: Working portal

- **WHEN** `remove` names a portal whose wizard is completed
- **THEN** the answer is 200 with `removed` false, the owner receives the deletion letter and the portal stays Active

#### Scenario: Hosted installation

- **WHEN** a hosted installation receives `remove`, `status` or `get`
- **THEN** the answer is 403 "Method for server edition only."

### Requirement: Asking for the owner to be changed

`sendOwnerChangeInstructions` with `ownerId` SHALL require the portal-settings right (Room admin,
User, Guest 403; anonymous 401) and refuse a guest as the new owner with 403 and an owner whose own
e-mail is not confirmed with 400. Only the owner naming an active, existing member starts the
transfer: the PortalOwnerChange letter goes to the owner's address, "Owner sent change owner
instructions" is written and the answer is 200 with `status` 1 and a `message` carrying the address
in a `mailto:` anchor. A DocSpace admin, or an inactive or unknown member, get 200 with `status` 0
and the localized refusal.

#### Scenario: Room admin, User or Guest

- **WHEN** a Room admin, a User or a Guest calls `sendOwnerChangeInstructions` naming a User
- **THEN** the answer is 403

#### Scenario: Anonymous

- **WHEN** nobody is signed in and `sendOwnerChangeInstructions` is called
- **THEN** the answer is 401

#### Scenario: DocSpace admin who is not the owner

- **WHEN** a DocSpace admin calls `sendOwnerChangeInstructions` naming an active User
- **THEN** the answer is 200 with `status` 0 and "Access denied" as `message`, and no letter is sent

### Requirement: Confirming the owner change

`updatePortalOwner` with `ownerId` SHALL be reached with the PortalOwnerChange link only (403 with an
ordinary token, 401 without one), require the link's address to still be the owner's (500 "Access
denied" otherwise, which is what a replayed link gets) and the named member to exist, be active and
not a guest (500). A member who is not a DocSpace admin is promoted first (402 when no paid seat is
left); then the portal's `ownerId` changes, "Owner updated" is written and the answer is 200 without a
body. The previous owner keeps the account as a DocSpace admin.

#### Scenario: Room admin, User or Guest with a token

- **WHEN** a Room admin, a User or a Guest calls `updatePortalOwner`
- **THEN** the answer is 403

#### Scenario: Anonymous

- **WHEN** nobody is signed in and `updatePortalOwner` is called
- **THEN** the answer is 401

#### Scenario: Owner confirms

- **WHEN** the owner opens the link and `updatePortalOwner` names the active User the letter was issued for
- **THEN** the answer is 200, `getPortalInformation` reports that user as `ownerId`, their type is DocSpace admin and the previous owner is a DocSpace admin

### Requirement: Blocking an idle or unpaid portal on a hosted installation

A daily job SHALL block a free portal with no sign-in or audit activity for 60 days, a portal whose
paid tariff lapsed 90 days after the due date, and either one with money left in its wallet after
365 days, counting from the activity, the due date, the last status change or the policy start,
whichever is later; a portal with a paid tariff or a paid add-on, and one on a reserved domain,
SHALL be left alone. Blocking sets the status Blocked, writes "Portal blocked" and mails the owner (and
support for a paying category); the warning letters are in `notifications`.

#### Scenario: Free portal idle for two months

- **WHEN** a free portal has had no activity for 60 days and the job runs
- **THEN** the portal is Blocked, the owner receives the blocked letter and the portal answers 404 to ordinary requests

#### Scenario: Paying portal

- **WHEN** a portal on a paid tariff has had no activity for a year
- **THEN** nothing happens to it

#### Scenario: Activity restarts the count

- **WHEN** a member signs in to a warned free portal
- **THEN** the count starts again from that day and the earlier warning no longer leads to a block

### Requirement: Unblocking a portal with the owner's link

`unblockPortal` (`PUT portal/unblock`) SHALL be reached with the PortalUnblock link from the blocked
letter, which is issued to the owner only and tied to the day of the block. On a Blocked portal it
SHALL set the status Active, reset the retention record, re-apply the Content Security Policy, write
"Portal unblocked" and answer 200 without a body. A link for another block, a link on a portal that is
not blocked, and a missing link SHALL answer 401; an ordinary sign-in token 403.

#### Scenario: Owner unblocks the portal

- **WHEN** the owner opens the unblock link of the current block
- **THEN** the answer is 200, the portal is Active and answers as before, and the same link used again answers 401

#### Scenario: Link of an earlier block

- **WHEN** an unblock link mailed for an earlier block is used while the portal is blocked again
- **THEN** the answer is 401 and the portal stays Blocked

#### Scenario: Not blocked

- **WHEN** an unblock link is used on an Active portal, or `unblockPortal` is called with the owner's ordinary token
- **THEN** the answers are 401 and 403

### Requirement: Removing a blocked portal at the end of its retention

A Blocked portal nobody unblocks SHALL be removed by the daily job 30 days after the block (free) or
90 days (lapsed tariff or wallet balance), the owner having been reminded a week (and, for the 90-day
cases, a month) before; a late reminder moves the removal back so the week is kept. The removal does
what `deletePortal` does with the alias `<alias>_auto_deleted`, the "deleted" letter and, for a lapsed
tariff, the support letter. A portal unblocked after the job read it SHALL NOT be removed, and the
retention of a block SHALL NOT change when the wallet empties.

#### Scenario: Free portal nobody unblocks

- **WHEN** a free portal has been Blocked for 30 days
- **THEN** the portal becomes Remove pending, its purge starts and the owner receives the deleted letter

#### Scenario: Unblocked meanwhile

- **WHEN** the owner unblocks the portal after the job read it as Blocked on its removal day
- **THEN** the removal is skipped and the portal stays Active
