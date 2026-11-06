# Known deviations between the specifications and the server

Last updated 2026-11-06, after the v4.1.0 revision: statuses are taken from the `verification.md` of the 23
specs, checked range `d58816bece..3533ad8a89`.

The specifications in `specs/` describe the server as it behaves today (checked against the integration
tests, a static reading of the code and a live run against a local portal, October 2026). This file keeps
what was deliberately left out of them: behaviour that looks like a defect rather than a design decision,
and places where the product matrix ("Types and Roles. Access rights", "Shared link rules") describes
something the server does not do. Each item names the specification it would belong to; file:line evidence
stays in that specification's `verification.md`.

Status: **OPEN** (still open, or new in this revision), **FIXED** with the commit, **REFUTED**, or **not
re-checked** when no `verification.md` gives a verdict. FIXED and REFUTED rows are in "Closed by v4.1.0" at the
end.

## Suspected defects (not written into the specs)

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| D1 | vdr-room-roles, form-filling-room-roles | `PUT /api/2.0/files/file/{id}/manageformfilling` checks permissions only for the Start and Stop actions. Resume (1) on a stopped form answers 200 and resumes filling; Edit (3) on a started form in a Form filling room answers 200 and stops collecting. Both work for a Viewer, a Form Filler without a role, and for a User or Guest who is not a member of the room. | live run; `FileStorageService.cs` manage-form-filling actions | OPEN |
| D2 | shared-links | In a Public room a non-member (User or Guest) gets 200 on `PUT /api/2.0/files/folder/{id}/links` with `primary:true, access:2`, and a primary read link is created on a folder they cannot read. The token is not returned to them and they cannot read it back, so no content leaks; it is an unauthorized write. The same request on a Custom room is refused. | live run; `FileSharing.SetAceObjectAsync` skips the check for a primary read link in a Public room | OPEN |
| D3 | user-type-permissions | A Room admin promotes a guest invited by someone else to User through `POST /api/2.0/people/type` (200, job completes, type changes), while `PUT /api/2.0/people/type/User` refuses the same guest (403). | live run | OPEN |
| D4 | user-type-permissions | A Room admin cannot add a foreign guest to their room by id (200, nobody added) but can by email: the guest joins the room and the Room admin's guest list, after which file sharing with that guest works. | live run | OPEN |
| D5 | vdr-room-roles | A Form Filler who holds no role on a PDF form, and a Form Filler in a room where the form is not configured, cannot see the form in the listing but reads it by id (`GET files/file/{id}` 200, `presigneduri` 200, `openedit` 200). | live run; `FileSecurity` PDF branch | OPEN |
| D6 | form-filling-room-roles | A Form Filler copies a not-started form, hidden from them in the listing and 404 by id, to My documents through `fileops/copy` (200, copied). | live run | OPEN |
| D7 | user-type-permissions | `POST /api/2.0/files/@my/file` as a Guest answers 500 (`NullReferenceException` in `FileStorageService.CreateNewFileAsync`); `@my/text` and `@my/html` answer 404. | live run | OPEN |
| D8 | form-filling-room-roles | A docx uploaded or created in a Form filling room answers 500 with the "cannot be uploaded to this room" message; `fileops/copy` of the same docx answers 403. | live run | OPEN |
| D9 | user-type-permissions | `GET /api/2.0/portal/users/invite/{type}` never refuses: a disallowed pair answers 200 with an empty string, and for the Guest type it returns a real link to the Owner, a DocSpace admin and a Room admin, although `POST /portal/users/invitationlink` and the people endpoints refuse Guest for everyone. | live run | OPEN |
| D10 | user-type-permissions | `PUT /api/2.0/people/delete` by a DocSpace admin on a disabled DocSpace admin answers 200 with the account echoed and deletes nothing; `DELETE /people/{id}` answers 403. | live run | OPEN |
| D11 | shared-links | Files and folders in My documents, Custom, Collaboration and VDR rooms accept up to 6 *primary* links in addition to 6 additional ones (`primary:true` with a new link id succeeds up to 6 times). Rooms and Public-room entries keep one primary link. | live run; `FileSecurity`/`FileSharing` link limits | OPEN |
| D12 | shared-links | A primary link of a folder inside a Form filling room stores an expiration date on create; later updates and revocation are skipped unless the stored date is echoed. | live run | OPEN |
| D13 | form-filling-room-roles | The "only ONLYOFFICE PDF forms" rule is enforced by extension only (`.pdf`), so an ordinary PDF enters the room and is auto-started on move; the form-signature check exists only in the advisory `checkdestfolder`. | live run; `FileDao`, `CopyPermissionsCheck` | OPEN |

**Recorded contradiction, not resolved (hidden form read by id).** D6 and the live run of `form-filling-room-roles`
said 404 for a Form Filler reading a not-started form by id. `form-filling-workflow` and its test say 403 (FW2).
Today `specs/form-filling-room-roles/spec.md` itself says 403 (scenario "Form Filler does not see a form that is
not started") and no longer contains 404, its `verification.md` writes "403/404" for D6 and calls 403 the asserted
value, while FW2 still describes that spec as saying 404. Nothing has been run since the live run, so the D6 text
above stays as recorded.

## Matrix says one thing, server does another (specs follow the server)

| Where | Matrix | Server | Status |
|-------|--------|--------|--------|
| user-type-permissions | Users and Guests may invite to a room (cells marked "ships in the next version"); roles handed out are limited to their own | Inviting needs the Room manager role, which a User or Guest cannot hold; 403 | not re-checked |
| user-type-permissions | Nobody sees the links of someone else's public room | A non-member DocSpace admin reads the primary link | not re-checked |
| user-type-permissions | Owner and DocSpace admin see all rooms | Not private (end-to-end encrypted) rooms | not re-checked |
| user-type-permissions | A Room admin requests promotion of a User to Room admin; Owner or DocSpace admin confirms in Accounts | No such operation in the API | not re-checked |
| user-type-permissions | A Room admin shares files with own guests and guests of own rooms | Only guests the Room admin invited | not re-checked |
| user-type-permissions | Forms in rooms other than FFR/VDR are shared "for editing only" | Editing, comment and read links are offered; filling is refused | not re-checked |
| user-type-permissions | DocSpace admin manages every setting except portal deletion | Suspension and granting the DocSpace admin type are owner-only too | not re-checked |
| room-role-permissions | Every role invites users and assigns roles; Owner and Room manager "moderate" invitations | Only Owner and Room manager invite; no moderation exists | not re-checked |
| room-role-permissions | Nobody changes own role | A Room manager can lower own role; any member can leave | not re-checked |
| room-role-permissions | A Content creator locks only own files | Locks any unlocked file; cannot unlock someone else's lock | not re-checked |
| room-role-permissions, vdr-room-roles, form-filling-room-roles | Rename: Owner and Room manager only | A Content creator renames own entries | not re-checked |
| room-role-permissions, vdr-room-roles, form-filling-room-roles | Copy of other members' files: Owner and Room manager only; copy out of the room: Owner, Room manager, Content creator | Copy does not depend on authorship; every role copies out (a Viewer unless download is denied) | not re-checked |
| room-role-permissions | Copy out of an archived room: Owner, Room manager, Content creator | Every role | not re-checked |
| room-role-permissions | Form Filler is a role of a general room | Only VDR and Form filling rooms accept it; a Public room accepts only Content creator and Room manager | not re-checked |
| vdr-room-roles | A Form Filler sees a configured form only when their turn has come | Visible as soon as assigned; the turn governs filling | not re-checked |
| vdr-room-roles | A Content creator stops filling of own forms and forms they organized | Only forms they created | not re-checked |
| vdr-room-roles | A Form Filler views only forms they take part in | Non-form files are refused; see D5 for PDFs | not re-checked |
| form-filling-room-roles | Only ONLYOFFICE PDF forms enter the room | Any PDF (see D13) | not re-checked |
| form-filling-room-roles | A Form Filler does not copy or move forms; does not delete "Complete" forms | Copies forms out; deletes own results in both folders (delete: code only) | not re-checked |
| form-filling-room-roles | Results sync by a Content creator "only own / organized" (resolved comment) | Any Content creator, any form | not re-checked |
| shared-links | Maximum 6 links per file or folder | 6 additional plus primary links (see D11) | not re-checked |
| shared-links | Form filling room link: filling only | The room link also accepts the read right; entries accept filling only | not re-checked |
| shared-links | Form filling room and Public room links are perpetual | Only the room's own primary link; entries and additional links keep an expiration date | not re-checked |
| shared-links | "Rejects" an audience or lifetime value that is not offered | Answers 200 and silently normalises or skips (see the spec) | not re-checked |
| shared-links | VDR: deny-download cannot be turned off on a link while the room setting is on | The flag is stored; the room setting still blocks the download | not re-checked |

## Suspected defects (static findings)

Static findings collected from the `verification.md` of each spec; the numbers are the ones used there. **No test
was run in this revision**: every "red" below is a statement of the `verification.md`, not the result of a run.
`form-filling-workflow` numbers two items F1 and F2 that have nothing to do with F1 and F2 of `file-operations`;
this register labels them FW1 and FW2. Within a table, rows are ordered by how much they matter.

Evidence column:

- **red by code reading**: the assertion of a test was compared with the code; the test was not run.
- **red test**: the `verification.md` records the test as red or marked as failing; not run here.
- **bug test**: a test with a bug trait expects other behaviour; the `verification.md` does not say it is red.
- **per test**: the row rests on a test's comment or remarks; the code was not read or not fully traced.
- **test pins**: a test asserts the current behaviour.
- **static**: code or contract reading, with no statement about a test.

### Access and integrity

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| A15 | ai-agents | `aiProfilesList` hands the provider key to every member: Chat sends `forClient=true`, which the storage controller does not read, and the service does not strip it. | red by code reading | OPEN |
| A18 | ai-agents | `aiWebSearchGetActiveConfig` returns the stored web-search key to a User. | red by code reading | OPEN |
| A20 | ai-agents | The MCP read routes redact only system servers, so a custom server's Authorization header reaches a User and a Room admin. | red by code reading | OPEN |
| A19 | ai-agents | `aiAssignmentsResolveForAction` echoes the profile with its key. | per test (comment; not fully traced) | OPEN |
| R2 | user-type-permissions | A DocSpace admin can deactivate another DocSpace admin and the Owner. | red test (bug 79900) | OPEN |
| R1 | user-type-permissions | A private room is listed to a non-member Owner / DocSpace admin. | red test (bug 82956) | OPEN |
| L8 | branding-and-licensing | `acceptLicense` and `refreshLicense` demand no right: any signed-in member of any portal of the installation, a Guest included, can move a staged licence into place, rewrite the installation's quota and tariff and write the audit event under their name. | static | OPEN |
| T8 | portal-lifecycle | `deleteportalimmediately` lets any owner whose address matches the configured test-address pattern delete the portal with an ordinary session and no confirmation; a loose pattern turns every owner of a domain into a one-call deleter. | static | OPEN |
| T6 | portal-lifecycle | The rename answers with a confirmation sign-in link of type `Auth` for the caller in the body of a 200, i.e. a credential-bearing URL that proxies log. | static | OPEN |
| A16 | ai-agents | A chat round is not bound to the project for a read member: the context drops the folder and the fallback reads a null `type`, so the round runs on what the composer sent, without the project's model and sources. | red by code reading | OPEN |
| A17 | ai-agents | `aiThreadsCreate` never sees a project because of the same null `type`: a DocSpace admin outside it creates a thread, and no model answers the generic 404, not 409. | red by code reading | OPEN |
| F24 | file-operations | The upload part and finalize routes find a session by id alone: a caller without access to the target folder who holds the id writes into and finalizes the Owner's session; only abort checks the owner. Security smell, design decision pending. | static (by code reading; the test pins nothing) | OPEN |
| R7 | shared-links | `getFilePrimaryExternalLink` answers 200 to a caller without access to the file. | red test (bug 81572) | OPEN |
| R4 | room-role-permissions | `POST folder/{id}/link` by a Content creator, Viewer, non-member admin, Guest or on an archived room answers 200 with an empty list instead of 403. | red test (bug 81575) | OPEN |
| F23 | file-operations | `getFileVersionInfo` answers the version list to a member invited with Editing access, although the document says only managers and content creators read the history (see H6). | red test (bug 84774) | OPEN |
| H6 | history-and-audit | Same bug as F23, recorded differently: the version list is open to Editing through the read-history right, the new test contradicts the sibling `EditHistoryPermissionsTests`, and the spec follows the server. | bug test (84774) | OPEN |
| F21 | file-operations | `saveediting` into a file open in somebody else's editing session answers 200, replaces the content, grows the version and clears the editing flag; `startedit` on a file locked by somebody else is refused through a 500. | red test (bugs 84769, 84768) | OPEN |
| H3 | history-and-audit | `PUT security/activeconnections/logout/{loginEventId}` checks ownership only when the login event has a `UserId`; an event stored without one can be closed by any signed-in user. | static | OPEN |
| S2 | webhooks-and-api-keys | `PUT keys/{id}` on an expired key answers `200 false` before the ownership check: a non-owner learns that a foreign key exists and is expired instead of 403. | static | OPEN |
| S1 | webhooks-and-api-keys | Updating an API key with `permissions: ["*", "files:read"]` is stored as-is and the auth handler then grants unrestricted `*:write`; creation normalises the same input to "unrestricted". | static (no test) | OPEN |
| S8 | webhooks-and-api-keys | The generated C# SDK writes `"ssl": false` when the caller does not set `Ssl`, so an update through it switches certificate checking off; the server is right and the SDK has to be regenerated. | static (test remarks) | OPEN |
| F1 | file-operations | `POST files/thumbnails` is `[AllowAnonymous]`; an anonymous caller without a link is accepted although the API document says 401. | red test (bug 81268) | OPEN |
| F9 | file-operations | `markAsRead` has no per-item access check: a non-member marks a room file as read with 200; a file the caller cannot read is passed over in silence. | test pins | OPEN, design question |
| Q2 | quotas | Moving a file between two accounts' My documents silently raises the receiver's custom user quota by the file size. | static | OPEN |
| S6 | webhooks-and-api-keys | The "limited Developer Tools access for Users" setting also blocks Room admins, and now drops the events of their subscriptions; notes and endpoint text say Users only. Intent unclear. | test pins (bug 81236) | OPEN, widened |
| A1 | ai-agents | Vectorization answers `AlreadyProcessed` before any right check, so an already processed file answers 200 to anyone, queues nothing and shows the file exists; a pending file is refused. | static (by code reading, no .NET test) | OPEN, narrowed |
| A11 | ai-agents | Deleting another user's prompt answers success although nothing is deleted. | red by code reading (the engine between the two files was not read; the test comment says "used to") | OPEN |
| P5 | plugins | `getWebPlugins`/`getWebPlugin`, callable by any member incl. Guests, rewrite the portal plugin settings and emit a socket event when a stored value fails to decrypt: a GET mutates portal settings. | static | OPEN |
| P1 | plugins | A manifest with `"enabled": true` is reported as on for every portal without a stored row, and switching it off deletes the row so the manifest value returns: the plugin cannot be switched off; manifest `createBy`/`createOn` are trusted. | static | OPEN |
| M1 | data-import | `migrationFileUpload.ashx` answers 200 for every outcome incl. "Access denied." and an upload without `Init`; no size limit; outside OpenAPI. | static | OPEN |
| M10 | data-import | `migratorName` of the body is trusted over the parsed job's migrator, so a wrong name re-parses the upload with the wrong migrator and fails late. | static | OPEN |
| MD7 | metadata | Section and rooms listings parse the metadata filter before any access check, so any member probes template and field ids through the 400 texts. | static | OPEN |
| N1 | notifications | The room-deleted letter checks the subscription and mute list of the deleter instead of each recipient. | static | OPEN |
| N2, N8 | notifications | Form submitted/filling letters and the submitted push bypass the rooms-activity subscription and the room mute that every other room letter applies. | static | OPEN |
| B4 | backup-and-restore | Restore from a Documents file demands the create right on the folder although reading is all it needs. | static | OPEN |
| B11 | backup-and-restore | `backup/backuptmp` returns the worker's absolute temporary folder to any DocSpace admin. | static | OPEN |
| B12 | backup-and-restore | The portal-wide 403 while restoring is a path-suffix allowlist (`portal`, `settings`, …); any route ending in one of those words passes. | static | OPEN |
| F14 | file-operations | A session below `chunkUploadSize` stores the file on the first part: a truncated file is listed, finalize answers 201 twice, a part after finalize is accepted, abort after finalize answers 200, and each repeated part adds a version. | red by code reading (`Category=Bug`, no bug number) | OPEN |
| F15 | file-operations | Finalize without any part, or of a 100-byte session that received 300 bytes, answers 201 with `uploaded` true and file id 0 and stores nothing; a short intermediate part makes finalize answer 500. | red by code reading (the 500 rests on the test's summary, not traced) | OPEN |
| FW1 | form-filling-workflow | `POST files/{folderId}/file`, `/text` and `/html` answer 200 and create a non-PDF in a Form filling room; the test asserts a 4xx. Conflicts with F19 and D8, which report a 500 for the same creation (bug 84840). | red test (bug 84840) | OPEN |
| MD10 | metadata | A character outside the Basic Multilingual Plane in a template name, field name, option, string value or custom field is echoed intact but stored as `?`: the columns are three-byte `utf8` and the write does not refuse it. | red test (four tests, `Category=Bug`) | OPEN |
| F17 | file-operations | A version comment holding a character outside the Basic Multilingual Plane is echoed as sent but stored as `?`. | red test (`Category=Bug`; the column was not re-read) | OPEN |
| F16 | file-operations | A terminate that arrives while the copy still waits for a free worker thread is dropped, so the copy runs to its end when the queue is busy. | red test (per the test's remarks, flaky by load; the queue was not re-read) | OPEN |
| F18 | file-operations | A batch move of several folders is refused or fails as a whole when one of them holds a file open in an editing session. | red test (bug 84725; the operation was not traced) | OPEN |
| S11 | webhooks-and-api-keys | The room and agent quota endpoints accept the id of a plain folder (or an ordinary room for the agent ones) and write a quota on it. Belongs to `quotas` as well. | static (contract text) | OPEN |
| B18 | backup-and-restore | A webhook-journal statement that keeps failing after five attempts is skipped with a log line, so a dump restore can silently lose part of the journal while reporting success. | static | OPEN |
| AP3 | apps | The first write freezes the then-current configured `enabled` default into the row; a portal that only saved settings stops following later configuration changes of the default. | static | OPEN |
| MD8 | metadata | Copied values are pasted after stamping without the destination's conflict mode, so an overwrite-cascading destination does not overwrite a copy as it does a move. | static | OPEN |
| MD2 | metadata | The stamp pass after a move is invisible to `getCascadeProgress` and writes no audit. | static | OPEN |
| T3 | portal-lifecycle | Owner change promotes the new owner before `ownerId` is saved; a failure leaves a promoted member and the old owner. | static | OPEN |
| T5 | portal-lifecycle | Rename saves the alias, writes the audit and logs everyone out before the hosting cache is updated; a failure leaves a hosted portal reachable under neither name. The action is still absent from the contract. | static | OPEN |
| P2 | plugins | Plugin state is saved and the socket event sent before the CSP save; on the header-size overflow the caller gets 403 while the plugin is installed and enabled. | static | OPEN |
| W6 | billing | Service-limit writes commit to accounting before the audit record and socket event; a late failure answers 500 with the limit already changed. | static | OPEN |
| W4 | billing | `moveSubscriptionToWallet` compares a price in the request region's currency with a USD wallet balance. | static | OPEN |
| W5 | billing | `setTenantWalletSettings.currency` is stored unchecked; auto top-up silently never fires when it matches no sub-account. | static | OPEN |
| L1 | branding-and-licensing | Saving logos from image data returns instead of continuing on an empty entry, silently skipping every later slot while answering `true`. | static | OPEN |
| L3 | branding-and-licensing | Restoring built-in logos resets only the seven original slots; the eight editor slots keep `default: false` and their extension. | static | OPEN |
| L2 | branding-and-licensing | Embedded spreadsheet/PDF logo slots read the extension of the normal slot; with different formats the URL names a file that does not exist. | static | OPEN |
| M6 | data-import | Unreachable RoomAdmin branch: the paid-user socket update never fires for imported Room admins; an imported `Guest` gets no type group. | static | OPEN |
| M9 | data-import | Source users whose e-mail belongs to a disabled portal account are dropped silently without a log or an `errors` entry. | static | OPEN |
| B7 | backup-and-restore | Backup counters include removed records, so deleting a backup does not return the free allowance. | test pins | OPEN |
| B9 | backup-and-restore | Any billing or quota failure deletes the backup schedule outright; a temporarily empty wallet silently ends automatic backups. | static | OPEN |
| B10 | backup-and-restore | `getBackupHistory` soft-deletes records and skips unreachable storages, so the list can shrink between two reads. | static | OPEN |
| N9 | notifications | The not-paid skip exists only for the daily digest; the hourly digest and at-once room letters keep going on a lapsed portal. | static | OPEN |
| A9 | ai-agents | Export into a form-filling room or an out-of-quota room answers 202 and the worker drops the document without a trace. | red by code reading | OPEN |

### Crashes (500) and wrong status codes

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| H1 | history-and-audit | `GET security/audit/events/filter` with any non-default `moduleType` dereferences a null module mapper → 500. | static (no test) | OPEN |
| Q1 | quotas | A default room or AI-agent quota above the total storage throws a plain `Exception` → 500; the user-quota siblings answer 400 for the same condition. | static | OPEN |
| A23 | ai-agents | `aiThreadsOpenOrCreate` with no `threadId` and no `firstMessage` answers 500 from the engine. | per test (comment, bug 82826) | OPEN |
| F19 | file-operations | Creating a `.docx`, a text file or an HTML file directly in a Form filling room is refused through a plain `Exception` → 500, the uploaded file being deleted first; same root as D8. Conflicts with FW1, which reports 200 for the same creation (bug 84840). | red test (bug 84840) | OPEN |
| F20 | file-operations | `changeVersionHistory` on a file locked by somebody else answers 500; on a file open in somebody else's editing session it answers 200 and changes nothing. | red test (bugs 84771, 84772) | OPEN |
| F22 | file-operations | `updateFile` on a file locked by somebody else is refused through a 500 instead of a client error; the title stays. | red test (bug 84842) | OPEN |
| F12, F10, H2 | file-operations, history-and-audit | 403 instead of 404 for a missing folder in `createFile` and a missing file in `getFileVersionInfo`; 403 instead of 401 for an anonymous caller of `file/{id}/history`; the sibling endpoints answer 404 / correctly. | test pins | OPEN |
| F6 | file-operations | `GET fileops/convert` and `fileops/import` → 400 instead of an empty list; `GET fileops/move` is shadowed by the check endpoint. | bug test (82225) | OPEN |
| Q3 | quotas | `setTenantQuotaSettings` reports a non-admin caller or a hosted portal with `NotSupportedException` → 415 instead of 403. | static | OPEN |
| T2 | portal-lifecycle | Confirming the owner change reports every business refusal ("User not found", "Access denied") as 500; the sibling `sendOwnerChangeInstructions` uses 400/403. | static | OPEN |
| T4 | portal-lifecycle | The "last full-access space" rule and the "wizard passed" check throw plain exceptions and answer 500; the yaml documents the 500 as intended. | static | OPEN |
| T7 | portal-lifecycle | System-API registration on a server installation validates `portalName` only inside the tenant registration: a taken or malformed name answers 500 instead of the 400 codes of `validateportalname`. | static | OPEN |
| M3 | data-import | `finishMigration` with `isSendWelcomeEmail: true` after a parse-only job: NullReferenceException, 500. | static | OPEN |
| M4 | data-import | Cancel/clear read the operation of a not-yet-started task: NRE in the event handler, or an unmatched parse that keeps running while its upload folder is deleted. | bug test (84792; M13 is its concrete form) | OPEN |
| M11 | data-import | An unknown `migratorName` does not produce "No such migration provider": the job ends with the null-reference message as `error` and `operation` null; the same for `startMigration` with an empty body. | static (the test asserts only non-empty) | OPEN |
| M12 | data-import | A parse that failed before doing anything still answers 200 with its log; the test expects 404. | bug test (84791) | OPEN |
| M2 | data-import | `getMigrationStatus` swallows every exception and returns the empty result; an outage looks like "no job". | static | OPEN |
| W12 | billing | `getCustomerDeposits` swallows every accounting failure into an empty result, so "no top-ups" looks like "service down"; the sibling `getDepositInvoice` answers 402. | static (unit test) | OPEN |
| N3 | notifications | `getNotificationSettings` with an undefined but bindable `type` (e.g. 7) answers 500 "Incorrect parameters" instead of 400; `setNotificationSettings` with it answers 200 and stores nothing. | static | OPEN |
| N10 | notifications | `linkTelegram` without a bot publishes a token and answers an empty link, then `checkTelegram` reports status 2 for 20 minutes although no link exists; the test expects 0. | bug test (84692) | OPEN |
| L4, L5 | branding-and-licensing | A logo file whose name does not start with the slot number, or a `data:` URI in an unsupported format, answers 500 instead of 400. | static | OPEN |
| L9 | branding-and-licensing | An unparsable licence or one lacking customer id/signature answers 500 "License is not correct" instead of a client error. | static | OPEN |
| L10 | branding-and-licensing | Greeting title save answers 500 on a `localhost` installation with more than one portal (alias rewritten to `localhost`, unique index); the .NET tests are commented out. | static | OPEN |
| A24 | ai-agents | In a concurrent MCP add of one name the losers answer 403, not the soft "already registered". | per test (comment) | OPEN |
| P4 | plugins | Every failure after the size checks, storage I/O included, is wrapped as 400 "Wrong plugin archive. …". | static | OPEN |
| W9 | billing | `getPaymentUrl` quantity below 1 is validated after the configuration and admin checks: 403 without billing, 400 with it. | static | OPEN |
| W2 | billing | `getCustomerInfo`/`getRestrictedAiModels` answer 200-empty to Guests when billing/AI is not configured and 403 when it is; the rule depends on the installation, not the caller. | static | OPEN |
| W8, W10 | billing | "No limits" vs "not a customer" are indistinguishable on the participants page; a failed balance read is cached 10 minutes as "no customer". | static | OPEN |
| B3 | backup-and-restore | `startBackupRestore` with the all-zero GUID queues a restore with no source that fails only in the worker (200). | static (observed by the test suite, which refuses to send it) | OPEN |
| B13 | backup-and-restore | The schedule refuses an unwritable storage with 400, but `startBackup` accepts the same body with 200 and lets the job fail in the worker, occupying the single queue slot. | static | OPEN |
| B5 | backup-and-restore | `backupFileUpload.ashx` answers 200 with `success:false` for every failure, permission refusals included. | static | OPEN |
| B2 | backup-and-restore | The documented 400 "dump on a non-standalone portal" is dead: the space-access demand already answers 403. | static | OPEN |
| M8 | data-import | Progress steps use integer division: with more than 30 users or 20 groups the progress does not move during that stage. | static | OPEN |
| L6 | branding-and-licensing | A colour theme beyond the limit is dropped and the call answers 200 with the unchanged list. | static | OPEN |
| R6 | shared-links | `getFolderPrimaryExternalLink` on a folder inside a Form filling room throws, while `PUT` on the same folder creates a primary link (D12): GET and PUT disagree. | test pins (no status asserted) | OPEN |
| R8 | shared-links | The link-limit refusal is produced as an operation warning with the entry skipped, not as an error; invalid `linkType`/`access` answer 403 instead of 400. | bug test (82370, 82371) | OPEN |
| R3 | user-type-permissions | The invitation-link endpoint refuses the Guest type with 400 "EmployeeType" while the people endpoints answer 403 for the same refusal. | static | OPEN |
| R9 | shared-links | Revoking a Custom room's primary link without the `primary` field leaves `getRoomsPrimaryExternalLink` at 404 for good; a revocation with `primary:true` lets it be minted again. | red test (bug 84873) | OPEN |
| R10 | shared-links | After a file's primary link is revoked, `createFilePrimaryExternalLink` answers 404 from then on, while `setFileExternalLink` can still make one. | red test (bug 84872) | OPEN |
| AP2 | apps | An application id longer than 64 characters in the configuration reads fine but every write fails at the database → 500; no start-up validation. | static | OPEN |
| MD3 | metadata | Raw exception text is returned in the cascade `error` field to any reader. | static | OPEN |
| MD1 | metadata | Retyping single ↔ multi choice is refused like any retype, and retyping a choice field requires `options: []`. | static | OPEN |

### Inconsistencies, messages and documentation

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| F11 | file-operations | `createNewIfExist` is inverted between text/HTML creation (`true` = return the existing file) and every upload call (`true` = keep both). | test pins | OPEN |
| FW2 | form-filling-workflow | By-id read of a hidden form: this spec, the code and the test say 403, while it describes the roles spec and D6 as saying 404. See the recorded contradiction after the D table. | test pins (documentation item) | OPEN |
| Q5 | quotas | A file that does not fit the remaining portal allowance is refused with "The maximum file size is exceeded (<remaining>)", quoting free space as a per-file limit. | static | OPEN |
| Q4 | quotas | `FolderQuotaChangeAsync` has an unreachable second over-limit check with a "folder not found" message; both cases are 403. | static | OPEN |
| Q6 | quotas | The `maxTotalSize = 0` branch of `getPortalQuota` for a User is dead code behind the permission demand, yet documented in the yaml. | static | OPEN |
| S3 | webhooks-and-api-keys | `ApiKeyManager.DeleteApiKeyAsync` returns `result >= 0`, i.e. always true. | static | OPEN |
| S4 | webhooks-and-api-keys | `PUT settings/webhook/enable` demands `name` and `uri` in a body it ignores. | static | OPEN |
| S7 | webhooks-and-api-keys | OpenAPI: the `WebhookTrigger` description still stops at `form.stopped` while the enum lists all 38 members; the signature-header part is fixed. | static | OPEN, partly fixed |
| S9 | webhooks-and-api-keys | Unlocking a file of the portal storage raises `file.updated` twice, and the lock call raises it even when nothing changes. | static (documented in the contract) | OPEN |
| S10 | webhooks-and-api-keys | `room.updated`, `agent.updated` and `user.updated` fire on every successful call even without a change, while `group.updated` was narrowed to real changes. | static (test covers the group side) | OPEN |
| S12 | webhooks-and-api-keys | `WebhooksLogDto.route` is the subscription's current URL, so the journal cannot show where a past delivery went. Design smell, documented. | static | OPEN |
| H4 | history-and-audit | A missing body on `POST security/audit/settings/lifetime` is reported as "LoginHistoryLifeTime" 400. | static | OPEN |
| H5 | history-and-audit | Audit and login filters are silently dropped on a plan without the audit option: 20 most recent events, 200. Design smell, documented. | static | OPEN |
| A14 | ai-agents | The `aiTools` build field and its route are in the code but not in `docspace-backend.yaml`; the rest of the old contract drift is fixed (yaml `5ea4dc6`). | static | OPEN, partly fixed |
| P7, L12 | plugins, branding-and-licensing | `refreshWebPlugin` and `POST settings/license/trial` are published by the controllers but hidden from OpenAPI and the SDKs. | static | OPEN |
| B6, B14, B15 | backup-and-restore | `cancelBackup` answers `true` for a job not yet registered (which then runs to the end), for a finished job, and dequeues a cancelled job so its `Canceled` status is mostly unobservable. | test pins | OPEN |
| B16 | backup-and-restore | `getBackupProgress` can show `isCompleted: true` with `status` Running and before `error` is visible; the fields are published separately. | test pins (transient observation) | OPEN |
| B8 | backup-and-restore | The free-allowance self-check is a no-op left from the free-to-paid transition (TODO to remove). | static | OPEN |
| B17 | backup-and-restore | A webhook delivery record with attempts but no answer is restored unscheduled with an empty response and no note, so it cannot be told from one whose receiver answered with an empty body. | test pins | OPEN |
| P3, P6 | plugins | A portal plugin shadowed by a same-name installation-wide plugin can be neither listed nor deleted; the count limit counts the de-duplicated list, re-upload bypasses it, the list is silently truncated. | static | OPEN |
| M5 | data-import | The upload folder path is cached for one day; a later parse ends with a generic "file not found" and the folder is never cleaned; deletion is fire-and-forget. | static | OPEN |
| M7 | data-import | The group loop returns instead of continuing; the per-group `shouldImport` of the contract is dead. | static | OPEN |
| M13 | data-import | `clearMigration` removes only completed jobs whose `operation` is "migration", so a failed parse stays queued until a new parse replaces it, and `cancelMigration` skips an unknown-migrator job too. | bug test (84792) | OPEN |
| N4 | notifications | A file updated or renamed by somebody else is not reported as new. | bug test (81713; TS `test.fail`) | OPEN |
| N5 | notifications | `setRoomsNotificationStatus` stores any id forever; no cleanup. | static | OPEN |
| N6 | notifications | SMTP save accepts and drops `useNtlm`; validation is only DTO attributes (TODO in the controller). | static | OPEN |
| N7 | notifications | Telegram texts declared for portal_deactivate, portal_delete, user_has_join, self_profile_updated, backup_failed are never sent; the Files `subject_*_tg` keys are unreferenced. | static | OPEN |
| L7 | branding-and-licensing | `feedbackAndSupportUrl` of the payment settings is always null. | static | OPEN |
| L11 | branding-and-licensing | The licensor flag of the company details is always stored as `false`. | static | OPEN |
| T9 | portal-lifecycle | A suspended portal answers 404 to `getPortalInformation`, so a client cannot read `status` to learn why, although the yaml tells it to. | static | OPEN |
| T10 | portal-lifecycle | `validateportalname` variants leak `<alias>_deleted` aliases of removed portals. | static | OPEN |
| A13 | ai-agents | `assertAiAccess` borrows the AI-off gate through a throwaway tool-prefs read instead of a gate of its own, now on four routes. | static | OPEN |
| A21 | ai-agents | In an export `/` in a title is a path separator (`Notes 12/31` → `31.docx`), and two simultaneous exports with one title get one name. | per test (bugs 82711, 84459; code not read) | OPEN |
| A22 | ai-agents | An export keeps TeX source in the document, and a stored provider failure stays English under any culture. | per test (bugs 83038, 83046; code not read) | OPEN |
| A25 | ai-agents | The profile create answer has `createdAt` milliseconds no later read reproduces. | per test (comment) | OPEN |
| W7 | billing | `setParticipantServiceLimits` does up to 1000 sequential user lookups per call. | static | OPEN |
| W11 | billing | `getDepositInvoice` walks every top-up of the portal, 1000 a page, until the id turns up; an id that does not exist costs the whole walk. | static (unit test) | OPEN |
| AP5 | apps | A bindable body without the `settings` member drops the stored document with 200, indistinguishable from an explicit reset. | test pins | OPEN |
| AP4 | apps | The JSON re-parse and its 400 in the settings write are unreachable; the real 400 comes from model binding. | static | OPEN |
| AP6 | apps | A socket event is sent on switching an application, none on saving its settings; the controller text documents it as intended. | static | OPEN |
| AP7 | apps | A switch of an application leaves an audit event since bug 84747, but `setSettings` leaves none, including the accidental drop of AP5. | static (by code reading) | OPEN |
| MD4, MD5, MD6 | metadata | Empty `templateIds` on a file still writes audit and socket (the folder variant returns early); cascade-off writes no audit while on/unassign do; a no-op custom-field removal still audits, sockets and re-indexes. | static | OPEN |
| MD9 | metadata | The yaml lacks the 403 row of `getCascadeProgress`; the number-limit text and the SDK listing models are fixed (`7d641bf729`). | static | OPEN, partly fixed |
| R5 | room-role-permissions | `updateFileComment` (version comment) is refused for an Editor; design question, kept outside the Comment row. | static | OPEN |
| FS1 | file-sharing | **Behaviour, not a defect**, recorded so nobody files it as one: `addFavorites` / `deleteFavoritesFromBody` skip a file the caller cannot read and answer `true`, the same as for a readable one; the spec states the skip. | test pins | NOT A DEFECT |
| — | user-type-permissions | Matrix row "copy of an archived room" is allowed by the matrix but `room-types` refuses duplication of an archived room; kept MATRIX-ONLY and flagged, neither spec changed. | static | not re-checked |

## Closed by v4.1.0

Rows whose `verification.md` verdict is FIXED or REFUTED, including those that were found and fixed within the
range. The fixes were read, not run. "Submodule" is a commit of `sdk/docspace-api-spec`, the regenerated contract.

| # | Spec | What the server did | Evidence | Status |
|---|------|---------------------|----------|--------|
| W1 | billing | A request header `Payment-Info` with any value other than `true` skipped the 402 gate of a not-paid portal. | static | FIXED `dbb446ada2` |
| S5 | webhooks-and-api-keys | `PUT settings/webhook` validated the target, incl. an outbound HEAD request, before checking ownership. | two tests cover it | FIXED `9e317df40d` |
| A12 | ai-agents | A server-executed DocSpace tool answered with another tenant's folder: a client-supplied `Mcp-Session-Id` reached the shared container (BUG 83172). | commit message | FIXED `916074e486` |
| A2 | ai-agents | `aiPreferencesGetToolPermissionMode` was served from an ungated snapshot, so a Guest read it past the AI switch. | tests | FIXED `4b380e5ee7`, `2a4975b142` |
| A7 | ai-agents | Web-search configure / set-active / test-connection ran validation and the provider probe before the Guest and AI-switch checks. | tests | FIXED `0dc046cacd`, `857671bb6d` |
| AP1 | apps | The `app_settings` table was in no backup module, so a backup dropped every application flag and settings document. Backups taken before the fix still hold no rows; the full cycle has no test. | test prepares the row | FIXED `9e696831d5` (bug 84721) |
| F8 | file-operations | `markAsRead` on the My documents root cleared the "new" marks of an unrelated room. | tests (bug 83509) | FIXED `73aa4e8ab8` |
| F7 | file-operations | A room member with Editing rights got 403 from `checkConversionStatus`. | tests (bug 81825) | FIXED `73aa4e8ab8` |
| F5 | file-operations | Aborting an upload session that never existed answered 500 instead of 404. | tests (bug 82278) | FIXED `73aa4e8ab8` |
| F3 | file-operations | `copyBatchItems` with a folder id that does not exist answered 500. | test asserts 404 (bug 84435) | FIXED `184d90789e` |
| F27 | file-operations | `moveBatchItems` with a folder id that does not exist answered 500. New row, fixed through the shared check of F3; no commit names bug 84436. | code and test read, not run | FIXED `184d90789e` |
| F4 | file-operations | `moveBatchItems` with a file id that does not exist answered 403 instead of 404. | tests; the test's "403 today" comment is stale | FIXED `01d0b7713d` (bug 82204) |
| F28 | file-operations | A file moved with `Duplicate` onto a same-named file kept the clashing title. New row. | code and test read, not run (bug 84437) | FIXED `77dd46d96a` |
| F2 | file-operations | `POST files/thumbnails` without `fileIds` answered 400 with a stack trace instead of 200 and an empty list. Fixed before the base; the earlier row was stale. | tests | FIXED `a79b825223` |
| F25 | file-operations | `checkMoveOrCopyDestFolder` counted a repeated id as refused, `checkMoveOrCopyBatchItems` ignored `content`, and both accepted the roots the copy refuses. New row. | guards read in the code, not run | FIXED `e9cfda8d43` |
| F26 | file-operations | An `outputType` the portal cannot produce was queued and silently converted to the internal format. New row. | guards read in the code, not run | FIXED `e9cfda8d43` |
| F29 | file-operations | A numbered part without a file part answered 500, part 0 or -1 was stored under a name never read, and `fileSize` -1 opened a session that could never finish. New row. | guards read in the code, not run | FIXED `e9cfda8d43` |
| F13 | file-operations | The move/copy pre-checks and `deleteFileVersions` could not be called through the generated SDK clients. | tests are typed now (bug 84727) | FIXED `06eecdbd77`, `632c2fc262` |
| A3 | ai-agents | Agent create left a room behind when the binding failed. | route gone | FIXED by removal `ecf76402b3` |
| A4 | ai-agents | An image-only Default slot was handed to Chat; Default still takes it, but resolve answers 404 and try-resolve null. | tests | FIXED `d59923d4a0` |
| A5 | ai-agents | `aiThreadsRegenerateTitle` answered 500 on a thread with a real conversation; the TS `test.fail` is stale. | test comment | FIXED `876875d79d` |
| A8 | ai-agents | MCP replace-all stored two names differing only in case. | test comment "used to" | FIXED `b6d11f1999` (library update) |
| A10 | ai-agents | OpenAI passthrough answered 500 with a stack trace on a large body and on malformed image requests. | commit and code; no .NET test | FIXED `9768f0a6f3`, `e84932692d` |
| B1 | backup-and-restore | `deleteBackup` of an unknown id answered 500; it now answers 404 and the yaml still says 500. | test | FIXED `61ff4b1ee4` (bug 80605) |
| T1 | portal-lifecycle | `Blocked` was missing from the published `TenantStatus`, `PortalUnblock` from `ConfirmType` and `PUT portal/unblock` from the paths. `portal/portalrename` is still absent: see T5. | yaml | FIXED submodule `5ea4dc6` |
| W3 | billing | Nine `servicelimit` operations and the `aiServicesLimit` profile field were missing from the published document and the SDKs. | yaml | FIXED submodule `5ea4dc6` |
| FS2 | file-sharing | `changeDefaultAccessRights` accepted an out-of-range number (bug 82641); the request model now answers 400. The `verification.md` calls it "not a defect" and names no commit. | green test by its assertion, not run | FIXED, no commit named |
| A6 | ai-agents | A file id and the Trash root are accepted as thread `entityId`: by design, with a reopen decision on bug 82719. | test | REFUTED |
