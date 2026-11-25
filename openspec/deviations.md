# Known deviations between the specifications and the server

Last updated 2026-11-18: a test run on `2a64d461e6` of every test marked `Category=Bug` (248) and of every test
with a `Bug` trait and no `Category=Bug` (1 101). The rows it reaches name the run in their evidence; R4 and N4 moved
to the closed table. A first pass of the run built the worktree without the document templates that `CreateFile`
copies (`products/ASC.Files/Server/DocStore`); the 49 Files tests that failed in it were run again with the
templates in place, and the verdicts here are those of that rerun. Both passes were repeated the same evening on
`c245ce0160` (the release after the trial-license merge, templates in place: 244 and 1 167 test cases): every test
that ran in both runs gave the same verdict, except the two `DeleteMember` tests of bug 83188, which had failed with
a transport error and now pass; two letter tests new in the release pass. The same outcome is appended to the status (or evidence) cell of each
of these rows in its `verification.md`; the `[run: …]` marks of the requirement rows still come from the full run of
2026-11-14. Earlier the same day: an editorial pass (references to rows that left
the register removed, duplicates merged, row texts aligned with their `verification.md`). Before that: the 2026-11-14 test run and live client check, integration
tests for the CODE-ONLY rows (run 2026-11-10) and the revision of range `bb406b409b..6fb013e745` (verdicts read from
the commits, not run). Statuses come from the `verification.md` of the 30 specs (`room-types` has no defect section);
earlier checked ranges `1f1b052f7d..0ebdbb7c84` and `d58816bece..3533ad8a89`.

The specifications in `specs/` describe the server as it behaves today (checked against the integration
tests, a static reading of the code and a live run against a local portal, October 2026). This file keeps
what was deliberately left out of them: behaviour that looks like a defect rather than a design decision,
and places where the product matrix ("Types and Roles. Access rights", "Shared link rules") describes
something the server does not do. Each item names the specification it would belong to; file:line evidence
stays in that specification's `verification.md`.

Status: **OPEN** (still open, or new in this revision; a `verification.md` that says STILL OPEN is written OPEN
here), **OPEN (product decision)** when which side is wrong is not settled, **FIXED** with the commit, **REFUTED**,
**NOT REPRODUCED** when a test written for the defect passes and no fixing commit is known, **NOT A DEFECT** for
behaviour recorded so nobody files it, or **not re-checked** when no `verification.md` gives a verdict. FIXED and
REFUTED rows are in "Closed by v4.1.0" at the end. The `verification.md` of the seven new specs (people-and-groups, private-rooms, files-settings,
storage-and-integrations, oauth2-applications, portal-settings, access-settings) gives an evidence kind and no
status column; their rows are OPEN as new in this revision.

Defects that bypass access, leak data or secrets, or let a caller act for another account or portal are not listed here: they are tracked privately until a released version fixes them.

## Suspected defects (not written into the specs)

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| D7 | user-type-permissions | `POST /api/2.0/files/@my/file` as a Guest answers 500 (`NullReferenceException` in `FileStorageService.CreateNewFileAsync`); `@my/text` and `@my/html` answer 404. | live run | OPEN |
| D10 | user-type-permissions | `PUT /api/2.0/people/delete` by a DocSpace admin on a disabled DocSpace admin answers 200 with the account echoed and deletes nothing; `DELETE /people/{id}` answers 403. | live run | OPEN |
| D11 | shared-links | Files and folders in My documents, Custom, Collaboration and VDR rooms accept up to 6 *primary* links in addition to 6 additional ones (`primary:true` with a new link id succeeds up to 6 times). Rooms and Public-room entries keep one primary link. | live run; `FileSecurity`/`FileSharing` link limits | OPEN |
| D12 | shared-links | A primary link of a folder inside a Form filling room stores an expiration date on create; later updates and revocation are skipped unless the stored date is echoed. | live run | OPEN |
| D13 | form-filling-room-roles | The "only ONLYOFFICE PDF forms" rule is enforced by extension only (`.pdf`), so an ordinary PDF enters the room and is auto-started on move; the form-signature check exists only in the advisory `checkdestfolder`. | live run; `FileDao`, `CopyPermissionsCheck` | OPEN |

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
| vdr-room-roles | A Form Filler views only forms they take part in | Non-form files are refused | not re-checked |
| form-filling-room-roles | Only ONLYOFFICE PDF forms enter the room | Any PDF (see D13) | not re-checked |
| form-filling-room-roles | A Form Filler does not copy or move forms; does not delete "Complete" forms | Copies forms out; deletes own results in both folders (delete: code only) | not re-checked |
| form-filling-room-roles | Results sync by a Content creator "only own / organized" (resolved comment) | Any Content creator, any form | not re-checked |
| shared-links | Maximum 6 links per file or folder | 6 additional plus primary links (see D11) | not re-checked |
| shared-links | Form filling room link: filling only | The room link also accepts the read right; entries accept filling only | not re-checked |
| shared-links | Form filling room and Public room links are perpetual | Only the room's own primary link; entries and additional links keep an expiration date | not re-checked |
| shared-links | "Rejects" an audience or lifetime value that is not offered | Answers 200 and silently normalises or skips (see the spec) | not re-checked |
| shared-links | VDR: deny-download cannot be turned off on a link while the room setting is on | The flag is stored; the room setting still blocks the download | not re-checked |
| user-type-permissions | A Room admin promotes any Guest to User (Types row 63; help agrees) | Only Guests that Room admin invited | independent check 2026-11-14 |
| vdr-room-roles | A Form Filler sees all complete forms (VDR row 47) | Other members' complete forms are hidden | independent check 2026-11-14 |
| form-filling-room-roles | A Content creator cancels any collection (FFR row 43) | Only collections they started | independent check 2026-11-14 |
| file-sharing | Help: Recent keeps the latest 100 files for 90 days | Up to 1000 entries, no time limit | independent check 2026-11-14 |
| room-types | CHANGELOG 2.5.0: third-party storage for Public rooms only | Every room type except the AI room | independent check 2026-11-14 |
| room-types | Owner and DocSpace admins delete any room (Types row 55; help agrees) | DocSpace admins delete only rooms they own | independent check 2026-11-14 |

## Suspected defects (static findings)

Static findings collected from the `verification.md` of each spec; the numbers are the ones used there. A row
whose evidence names no run rests on the `verification.md` alone: its "red" is a statement of that file, not the
result of a run.

From 2026-11-10 many defects are backed by integration tests marked `Category=Bug` that fail on purpose until the
defect is fixed; their rows carry the evidence "red test, run 2026-11-10".
`form-filling-workflow` numbers two items F1 and F2 that have nothing to do with F1 and F2 of `file-operations`;
this register labels them FW1 and FW2, and its later numbers FW4 to FW7. Within a table, rows are ordered by how much they matter.

Evidence column:

- **red test, run 2026-11-18**: the test ran on `2a64d461e6` and failed at the step that states the defect (its assertion, or the call that is the defect, such as a 500), not while preparing its data; the code was not re-read.
- **tests green, run 2026-11-18** / **test pins, run 2026-11-18**: the test passed in that run. For a test that asserts the correct behaviour this closes the row; for one that pins the current behaviour it confirms the row.
- **red test, run 2026-11-10**: an integration test marked `Category=Bug` fails on purpose and the `verification.md` records the red run of 2026-11-10.
- **red test, run 365d87fe60**: an exception to "no test was run": the test failed in the full run of that commit and its failure text was read; it was not re-run.
- **red by code reading**: the assertion of a test was compared with the code; the test was not run.
- **red test**: the `verification.md` records the test as red or marked as failing; not run here.
- **red test by reading**: the `verification.md` read a test recorded as red; not run here.
- **bug test**: a test with a bug trait expects other behaviour; the `verification.md` does not say it is red.
- **per test**: the row rests on a test's comment or remarks; the code was not read or not fully traced.
- **per test comment**: as "per test", in the words of the new specs' `verification.md`.
- **test pins**: a test asserts the current behaviour.
- **static**: code or contract reading, with no statement about a test.
- **by code reading**: the code was read and no test assertion was compared with it.
- **per commit message**: the row rests on what a commit message says.
- **yaml text only**: the row rests on the contract text; the code was not read.
- **test, run <date>** / **full run <date>**: the test ran on that date (alone or in the full solution run) and its result is the one the row states.
- **test passes, run <commit>** / **probe test passes**: a test written to reproduce the defect passed; the row is NOT REPRODUCED.
- **commit read, not run** / **code and tests read, not run**: a FIXED row whose fix was read in the commit and the tests that pin it; nothing was run.
- **tests**, **test**, **yaml**, **route gone**: a FIXED or REFUTED row whose `verification.md` gives that evidence and no run.

### Access and integrity

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| W13 | billing | `topUpDeposit` answers false for a failed or unreadable billing answer, the same as a decline, although the deposit may have gone through; a client that retries can charge the payer twice. | red test, run 2026-11-18 | OPEN (left open on purpose by `a4c952dda4`) |
| SI1 | storage-and-integrations | `updateStorage` and `resetStorageToDefault` put the portal into Migrating and publish a request nothing in this repository subscribes to, so the portal looks stuck and the settings are never saved; a consumer outside the repository cannot be excluded. | red by code reading | OPEN |
| SI2 | storage-and-integrations | `updateCdnStorage` answers with the new `module` and `props` but never saves them, and nothing subscribes to the message it publishes. | red by code reading | OPEN |
| PR4 | private-rooms | Backup skips key pairs and file keys and a module restore assigns new user and file ids, so a restored private room's content cannot be opened, and nothing read warns about it. | red by code reading | OPEN |
| A16 | ai-agents | A chat round is not bound to the project for a read member: the context drops the folder and the fallback reads a null `type`, so the round runs on what the composer sent, without the project's model and sources. | red test, run 2026-11-18 (cause by code reading) | OPEN |
| A27 | ai-agents | An invalid tool mode from a Guest or while AI is off answers 400, not 403: validation comes first. | red test, run 2026-11-18 (bugs 84303, 84302) | OPEN |
| A28 | ai-agents | More attachments than the composer allows are answered 200 and stored whole (11 stored). | red test, run 2026-11-18 (bug 82894) | OPEN |
| A30 | ai-agents | A round with a draft, inline record, device file or image attachment ends healthy but no streamed request carries the text; the stand's part was not traced. | red test, run 2026-11-18 (bug 82773; the stand's part was not traced) | OPEN |
| R7 | shared-links | `getFilePrimaryExternalLink` answers 200 to a caller without access to the file. | test passes, run 365d87fe60 | NOT REPRODUCED; no fixing commit identified |
| H3 | history-and-audit | The owner check runs only when the login event has a `UserId`, but no caller can create such an event: a failed sign-in under an unknown login is stored with no row a stranger can close, so a User cannot close another's connection through the API. | probe test `LogOutActiveConnection_FailedSignInOfUnknownLogin_UserCannotCloseIt` passes, run 2026-11-10 | NOT REPRODUCED; the check stays conditional in the code |
| PR1 | private-rooms | Deleting a subfolder of a private room still sends it to Trash. | per commit message | OPEN |
| Q2 | quotas | Moving a file between two accounts' My documents silently raises the receiver's custom user quota by the file size. | red test, run 2026-11-18 | OPEN |
| S6 | webhooks-and-api-keys | The "limited Developer Tools access for Users" setting also blocks Room admins, and now drops the events of their subscriptions; notes and endpoint text say Users only. Intent unclear. | test pins, run 2026-11-18 (bug 81236) | OPEN, widened |
| P1 | plugins | A manifest of an installation-wide plugin with `"enabled": true` is reported as on for every portal without a stored row, and switching it off deletes the row so the manifest value returns: the plugin cannot be switched off; shipped manifest `createBy`/`createOn` are trusted. For a portal or personal plugin neither an upload nor a read of the storage takes the manifest's `enabled`, `settings` and `cssUrl` any more, a manifest stored before the fix included (`WebPluginManifestStateTests`, green, run 2026-11-25; `WebPluginStateTests.Update_ManifestDeclaringEnabled_PluginCanStillBeSwitchedOff`, green on 2026-11-23 for the upload). | red by code reading for installation-wide manifests | OPEN, narrowed to installation-wide plugins |
| M10 | data-import | `migratorName` of the body is trusted over the parsed job's migrator, so a wrong name re-parses the upload with the wrong migrator and fails late. | static | OPEN |
| N1 | notifications | The room-deleted letter checks the subscription and mute list of the deleter instead of each recipient. | red test, run 2026-11-18 | OPEN |
| N2, N8 | notifications | Form submitted/filling letters and the submitted push bypass the rooms-activity subscription and the room mute that every other room letter applies. | red test, run 2026-11-18 | OPEN |
| B4 | backup-and-restore | Restore from a Documents file demands the create right on the folder although reading is all it needs. | static | OPEN |
| F14 | file-operations | A session below `chunkUploadSize` still stores the file on the first part, so a truncated file is listed and each repeated part adds a version, while a second finalize, a part after finalize and an abort after finalize are fixed by `ae457bdff6`. | red test, run 2026-11-18 | OPEN, narrowed |
| F15 | file-operations | Finalize without any part, or of a 100-byte session that received 300 bytes, stores nothing though it reports a file; a short intermediate part leaves a file listed after finalize. | red test, run 2026-11-18 | OPEN |
| MD10 | metadata | A character outside the Basic Multilingual Plane in a template name, field name, option, string value or custom field fails on write with a 500 (`An error occurred while saving the entity changes`); the earlier "echoed intact, stored as `?`" was wrong on this run's MySQL stand. | red test, run 2026-11-18 | OPEN |
| F17 | file-operations | A version comment holding a character outside the Basic Multilingual Plane fails on write with a 500 from MySQL (`Incorrect string value`); the earlier "stored as `?`" was wrong. | red test, run 2026-11-18 | OPEN |
| F16 | file-operations | A terminate that arrives while the copy still waits for a free worker thread is dropped, so the copy runs to its end when the queue is busy. | test green in the run of 2026-11-18, red in run 365d87fe60; the defect needs a busy queue, so one green run does not show a fix | OPEN |
| F18 | file-operations | A batch move of several folders is refused or fails as a whole when one of them holds a file open in an editing session. | red test, run 2026-11-18 | OPEN |
| S11 | webhooks-and-api-keys | The room and agent quota endpoints accept the id of a plain folder (or an ordinary room for the agent ones) and write a quota on it. Belongs to `quotas` as well. | static (contract text) | OPEN |
| B18 | backup-and-restore | A webhook-journal statement that keeps failing after five attempts is skipped with a log line, so a dump restore can silently lose part of the journal while reporting success. | static | OPEN |
| AP3 | apps | The first write freezes the then-current configured `enabled` default into the row; a portal that only saved settings stops following later configuration changes of the default. | static | OPEN |
| MD8 | metadata | Copied values are pasted after stamping without the destination's conflict mode, so an overwrite-cascading destination does not overwrite a copy as it does a move. | static | OPEN |
| MD2 | metadata | The stamp pass after a move is invisible to `getCascadeProgress` and writes no audit. | static | OPEN |
| T3 | portal-lifecycle | Owner change promotes the new owner before `ownerId` is saved; a failure leaves a promoted member and the old owner. | static | OPEN |
| T5 | portal-lifecycle | Rename saves the alias, writes the audit and logs everyone out before the hosting cache is updated; a failure leaves a hosted portal reachable under neither name. The action is still absent from the contract. | static | OPEN |
| P2 | plugins | Plugin state is saved and the socket event sent before the CSP save; on the header-size overflow the caller gets 403 while the plugin is installed and enabled. A personal upload or switch-on over the member's header limit answers the same way; for personal plugins the change `personal-web-plugins` makes it the required behaviour (the change kept, 403, the portal header served), so the row stays open for portal plugins only. | red test, run 2026-11-18 | OPEN |
| FX6 | files-settings | `setFilesOrder` applies items one at a time with no transaction, so a later item's 403 or 404 leaves earlier items reordered while the caller sees an error. | red by code reading | OPEN |
| SI4 | storage-and-integrations | A storage migration, if it were started, marks the task failed on failure but leaves the portal status at Migrating. | red by code reading | OPEN |
| PG-2 | people-and-groups | `updateGroup` takes any non-null `groupName`, so a blank name would be stored while `addGroup` refuses it with 400; `SaveGroupInfoAsync` itself was not read. | red by code reading | OPEN |
| W6 | billing | Service-limit writes commit to accounting before the audit record and socket event; a late failure answers 500 with the limit already changed. | static | OPEN |
| W4 | billing | `moveSubscriptionToWallet` compares a price in the request region's currency with a USD wallet balance. | static | OPEN |
| W5 | billing | `setTenantWalletSettings.currency` is stored unchecked; auto top-up silently never fires when it matches no sub-account. | red test, run 2026-11-18 | OPEN |
| L1 | branding-and-licensing | Saving logos from image data returns instead of continuing on an empty entry, silently skipping every later slot while answering `true`. | static | OPEN |
| L3 | branding-and-licensing | Restoring built-in logos resets only the seven original slots; the eight editor slots keep `default: false` and their extension. | static | OPEN |
| L2 | branding-and-licensing | Embedded spreadsheet/PDF logo slots read the extension of the normal slot; with different formats the URL names a file that does not exist. | static | OPEN |
| M6 | data-import | Unreachable RoomAdmin branch: the paid-user socket update never fires for imported Room admins; an imported `Guest` gets no type group. | static | OPEN |
| M9 | data-import | Source users whose e-mail belongs to a disabled portal account are dropped silently without a log or an `errors` entry. | static | OPEN |
| B7 | backup-and-restore | Backup counters include removed records, so deleting a backup does not return the free allowance. | test pins | OPEN |
| B9 | backup-and-restore | Any billing or quota failure deletes the backup schedule outright; a temporarily empty wallet silently ends automatic backups. | static | OPEN |
| B10 | backup-and-restore | `getBackupHistory` soft-deletes records and skips unreachable storages, so the list can shrink between two reads. | static | OPEN |
| N9 | notifications | The not-paid skip (`TariffState.NotPaid <= state`) is applied in `SendMsgWhatsNewAsync` to the daily and the hourly digest alike (a portal inside its grace period still gets them); only the at-once room letters of `NotifyClient.cs` have no such check, so a lapsed portal keeps sending them. | static | OPEN, narrowed to the at-once letters |
| A9 | ai-agents | Export into a form-filling room or an out-of-quota room answers 202 and the worker drops the document without a trace. | red test, run 2026-11-18 (bugs 83070, 83071; cause by code reading) | OPEN |
| A37 | ai-agents | The draft cleanup pass and the deleted-thread pass return the size of the deleted files to the project in a step after the deletion is committed. A failure of that step or a stop of the process between the two leaves the project's used space too high for good. Every deletion path of Files does the same. | red by code reading | OPEN (left open on purpose: Known limitations of `feature/new-ai-concept`) |

### Crashes (500) and wrong status codes

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| H1 | history-and-audit | `GET security/audit/events/filter` with any non-default `moduleType` dereferences a null module mapper → 500. | red test, run 2026-11-18 | OPEN |
| SI11 | storage-and-integrations | `getThirdPartyCode` for Docusign, Wordpress and Github on an installation without their OAuth application does not build the consent URL: a null dereference (Wordpress, Github) or an unparsable address (Docusign) answers 500, where a provider outside the seven answers 200 without a URL. It may depend on configuration, but the failure is unhandled. | red test, run 2026-11-18 (`OA/ThirdPartyOAuthProvidersTests.cs:68`) | OPEN |
| Q1 | quotas | A default room or AI-agent quota above the total storage throws a plain `Exception` → 500; the user-quota siblings answer 400 for the same condition. | red test, run 2026-11-18 | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| A23 | ai-agents | `aiThreadsOpenOrCreate` with no `threadId` and no `firstMessage` answers 500 from the engine. | red test, run 2026-11-18 (bug 82826) | OPEN |
| A32 | ai-agents | `aiProfilesListModels` on a saved profile whose provider accepts the connection and closes it without an answer is answered 200 with an empty model list instead of 502, so the form shows no models instead of the failure. | red test, run 2026-11-18 | OPEN |
| F20 | file-operations | `changeVersionHistory` on a file open in somebody else's editing session answers 200 and changes nothing (bug 84772); the 500 on a file locked by another account is fixed in `bb406b409b..6fb013e745`. | red test, run 2026-11-18 (bug 84772; the locked-file test of bug 84771 is green) | OPEN, narrowed |
| F12, F10, H2 | file-operations, history-and-audit | 403 instead of 404 for a missing folder in `createFile` and a missing file in `getFileVersionInfo`; 403 instead of 401 for an anonymous caller of `file/{id}/history`; the sibling endpoints answer 404 / correctly. | red test, run 2026-11-18 for F10 and H2; F12 has no Bug-marked test and was not run | OPEN |
| F6 | file-operations | `GET fileops/move` is shadowed by the check endpoint. `GET fileops/convert` and `fileops/import` now answer an empty list instead of 400. | bug tests (82225) green for convert and import, run 2026-11-18; the move part was not run | OPEN, narrowed |
| Q3 | quotas | `setTenantQuotaSettings` reports a non-admin caller or a hosted portal with `NotSupportedException` → 415 instead of 403. | red test, run 2026-11-18 | OPEN |
| PS-6 | portal-settings | `closeAdminHelper` gives a signed-in non-administrator of a self-hosted portal 415 instead of 403, one branch for a role and for an edition; the yaml documents it. | test, run 2026-11-11 (`AdminHelperTests.cs`) | OPEN |
| T2 | portal-lifecycle | Confirming the owner change reports every business refusal ("User not found", "Access denied") as 500; the sibling `sendOwnerChangeInstructions` uses 400/403. | static | OPEN |
| T4 | portal-lifecycle | The "last full-access space" rule and the "wizard passed" check throw plain exceptions and answer 500. | static | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| T7 | portal-lifecycle | System-API registration on a server installation validates `portalName` only inside the tenant registration: a taken or malformed name answers 500 instead of the 400 codes of `validateportalname`. | static | OPEN |
| M3 | data-import | `finishMigration` with `isSendWelcomeEmail: true` after a parse-only job: NullReferenceException, 500. | red test, run 2026-11-18 | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| M4 | data-import | Cancel/clear read the operation of a not-yet-started task: NRE in the event handler, or an unmatched parse that keeps running while its upload folder is deleted. | red test, run 2026-11-18 (bug 84792; M13 is its concrete form) | OPEN |
| M11 | data-import | An unknown `migratorName` does not produce "No such migration provider": the job ends with the null-reference message as `error` and `operation` null; the same for `startMigration` with an empty body. | static (the test asserts only non-empty) | OPEN |
| M12 | data-import | A parse that failed before doing anything still answers 200 with its log; the test expects 404. | red test, run 2026-11-18 (bug 84791) | OPEN |
| M2 | data-import | `getMigrationStatus` swallows every exception and returns the empty result; an outage looks like "no job". | static | OPEN |
| W12 | billing | `getCustomerDeposits` swallows every accounting failure into an empty result, so "no top-ups" looks like "service down"; the sibling `getDepositInvoice` answers 402. | red test, run 2026-11-18 | OPEN |
| SI10 | storage-and-integrations | `getStorageEncryptionSettings` catches every exception, so missing rights, a hosted portal and real failures all look like 200 with an empty body; the yaml documents this. | red test, run 2026-11-18 | OPEN |
| N3 | notifications | `getNotificationSettings` with an undefined but bindable `type` (e.g. 7) answers 500 "Incorrect parameters" instead of 400; `setNotificationSettings` with it answers 200 and stores nothing. | static | OPEN |
| N10 | notifications | `linkTelegram` without a bot publishes a token and answers an empty link, then `checkTelegram` reports status 2 for 20 minutes although no link exists; the test expects 0. | red test, run 2026-11-18 | OPEN |
| L4, L5 | branding-and-licensing | A logo file whose name does not start with the slot number, or a `data:` URI in an unsupported format, answers 500 instead of 400. | static | OPEN |
| L9 | branding-and-licensing | An unparsable licence or one lacking customer id/signature answers 500 "License is not correct" instead of a client error. | static | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| L10 | branding-and-licensing | Greeting title save answers 500 on a `localhost` installation with more than one portal (alias rewritten to `localhost`, unique index); the .NET tests are commented out. | static | OPEN |
| SI3 | storage-and-integrations | `getStorageProgress` on a server installation dereferences a null cache value when no migration is remembered: 500, and with SI1 that is every call. | red test, run 2026-11-18 | OPEN |
| SI7 | storage-and-integrations | `updateStorage` and `updateCdnStorage` read `props` unconditionally although it is not required: 500 instead of 400 for a configured module with no `props`. | red by code reading | OPEN |
| SI9 | storage-and-integrations | `startStorageEncryption` reports a running backup by a bare exception: 500 with no message for an expected condition. | red by code reading | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| FX8 | files-settings | `setDefaultTemplate` with a numeric `selectedFile` that is not a 32-bit integer answers 500. | red by code reading | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| FX7 | files-settings | `createRoomTag` with a name holding a character outside the BMP is refused by the database write with 500, not validated as 400; the test comment says bug 81682 was closed as by-design. | per test comment; `CreateRoomTag_NameOutsideBmp_Rejected` passes in the run of 2026-11-18 but asserts only that the name is refused, not the status code | OPEN |
| PS-3 | portal-settings | `configureDeepLink` with a body without `deepLinkSettings` fails on a null reference: 500 for a request fault. | red by code reading | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| PS-4 | portal-settings | `configureCsp` with a `null` entry in `domains` fails on a null reference: 500 for a request fault. | red by code reading | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| PS-5 | portal-settings | `saveDnsSettings` answers 500 through plain exceptions for a malformed or taken name and a hidden section, not 400 or 402 as the sibling settings do. | test, run 2026-11-11 (`DnsSettingsTests.cs`) | OPEN (the yaml stopped documenting the 500 on 2026-11-14; the code still answers it) |
| A24 | ai-agents | In a concurrent MCP add of one name the losers answer 403, not the soft "already registered". | red test, run 2026-11-18 | OPEN |
| P4 | plugins | Every failure after the size checks, storage I/O included, is wrapped as 400 "Wrong plugin archive. …". Narrowed on `feature/personal-plugins`: a refusal by the installed plugins, the plugin limit or a personal `cspDomains` value answers 400 with its reason alone. | static | OPEN, narrowed |
| W9 | billing | `getPaymentUrl` quantity below 1 is validated after the configuration and admin checks: 403 without billing, 400 with it. | red test, run 2026-11-18 | OPEN |
| AC-8 | access-settings | The 403 of `updateTfaSettings` for naming the owner depends on the `id` field, not the caller's role, so an owner listing themselves in `mandatoryUsers` with no `id` gets 403. | test, run 2026-11-11 (`TfaOwnerGuardTests.cs:89`) | OPEN |
| AC-9 | access-settings | `unlinkTfaApp` never reaches the branch that treats an empty `id` as the caller: an empty `id` resolves to no account and answers 403 first. | by code reading | OPEN |
| W2 | billing | `getCustomerInfo`/`getRestrictedAiModels` answer 200-empty to Guests when billing/AI is not configured and 403 when it is; the rule depends on the installation, not the caller. | red test, run 2026-11-18 | OPEN |
| W8, W10 | billing | "No limits" vs "not a customer" are indistinguishable on the participants page; a failed balance read is cached 10 minutes as "no customer" (`c2f48e709e` only documents W10). | static | OPEN |
| B3 | backup-and-restore | `startBackupRestore` with the all-zero GUID queues a restore with no source that fails only in the worker (200). | static (observed by the test suite, which refuses to send it) | OPEN |
| B13 | backup-and-restore | The schedule refuses an unwritable storage with 400, but `startBackup` accepts the same body with 200 and lets the job fail in the worker, occupying the single queue slot. | static | OPEN |
| B5 | backup-and-restore | `backupFileUpload.ashx` answers 200 with `success:false` for every failure, permission refusals included. | static | OPEN |
| B2 | backup-and-restore | The documented 400 "dump on a non-standalone portal" is dead: the space-access demand already answers 403. | static | OPEN |
| M8 | data-import | Progress steps use integer division: with more than 30 users or 20 groups the progress does not move during that stage. | static | OPEN |
| L6 | branding-and-licensing | A colour theme beyond the limit is dropped and the call answers 200 with the unchanged list. | red test, run 2026-11-18 | OPEN |
| R6 | shared-links | `getFolderPrimaryExternalLink` on a folder inside a Form filling room throws, while `PUT` on the same folder creates a primary link (D12): GET and PUT disagree. | test pins (no status asserted) | OPEN |
| R8 | shared-links | The link-limit refusal is produced as an operation warning with the entry skipped, not as an error. Invalid `linkType`/`access` now answer 400 (bugs 82370, 82371). | bug tests green for the 400 part, run 2026-11-18; the link-limit part was not run | OPEN, narrowed |
| OA3 | oauth2-applications | `getConsents` answers a Guest with their own signature 200 and an empty page, as the yaml documents; the earlier 500 came from the SDK failing on the null cursor. Bug 81729 asks for 403, so the test stays red: which side is wrong is a product decision. | red test, run 2026-11-18 | OPEN (product decision) |
| R3 | user-type-permissions | The invitation-link endpoint refuses the Guest type with 400 "EmployeeType" while the people endpoints answer 403 for the same refusal. | static | OPEN |
| R9 | shared-links | Revoking a Custom room's primary link without the `primary` field leaves `getRoomsPrimaryExternalLink` at 404 for good; a revocation with `primary:true` lets it be minted again. | red test, run 2026-11-18 (bug 84873) | OPEN |
| FX4 | files-settings | `get*PrimaryExternalLink` checks the revocation before the sharing right, so a caller with no access to a revoked entry gets 404, while an unrevoked entry gives 403. Refuted for `create*PrimaryExternalLink`, which no longer runs the revocation check. | red by code reading | OPEN, narrowed |
| AP2 | apps | An application id longer than 64 characters in the configuration reads fine but every write fails at the database → 500; no start-up validation. | static | OPEN |
| MD1 | metadata | Retyping single ↔ multi choice is refused like any retype, and retyping a choice field requires `options: []`. | static | OPEN |
| FW6 | form-filling-workflow | `PUT files/file/{id}/manageformfilling` with `Resume` on a form that was never started answers 500 (no filling properties); since `0ff3031b59` the yaml no longer declares that 500. Spec number F6. | test (`FormFillingStateTests.cs:252`) | OPEN |
| OA4 | oauth2-applications | `DELETE api/2.0/clients` (`deleteUserClients`) by a DocSpace admin answers 500 "Something went wrong"; it passed on 2026-11-10. The release brought Identity migration `V1_9_1__group_replication_keys.sql` (invisible primary keys on the client child tables), the likely cause, not proven. | full run 2026-11-14 and alone (`ClientWriteAccessTests.cs:237`); the other client writes failed in the full run with the same 500 and passed alone | OPEN (regression, cause not established) |

### Inconsistencies, messages and documentation

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| F11 | file-operations | `createNewIfExist` is inverted between text/HTML creation (`true` = return the existing file) and every upload call (`true` = keep both). | test pins | OPEN |
| P8 | plugins | The published `WebPluginDto` lists `image`, `cssUrl`, `settings` and the other optional manifest fields as required while the server leaves a null field out of the answer, so the generated C# client throws "Required property 'image' not found in JSON" on `getWebPlugin` / `getWebPlugins` for nearly every plugin. | red test, run 2026-11-18 (`WebPluginReadTests.cs:181-196`) | OPEN |
| S13 | webhooks-and-api-keys | `ApiKeyManager.UpdateApiKeyAsync` stores `["*"]` as no list (`null`), where the creation stores an empty one; the listing then leaves the required `permissions` field out and the generated C# client fails to read the whole `GET keys` answer. The key itself is unrestricted as intended. | red test, run 2026-11-18 (`UpdateApiKeyFieldTests.cs:89`) | OPEN |
| M14 | data-import | The OpenAPI document does not describe the `text/plain` body of `getMigrationLogs`, so the generated .NET SDK (4.1.0) parses the log as JSON and throws `ApiException` on every successful download. A contract/SDK defect; the server answers correctly. | test reads it over raw HTTP (`MigrationParseTests.cs:222`) | OPEN |
| FW4 | form-filling-workflow | A room owner may Duplicate a draft in a per-form "In process" folder: `FileSecurity` refuses Duplicate for every file of such a folder, yet `GET file/{id}` answers `security.Duplicate` true for the portal owner (the draft's creator gets false). | red test, rerun 2026-11-18 with the document templates in place (`DraftInInProcessSubfolder_CannotBeDuplicatedByTheRoomOwner`, `Category=Bug`); its verification calls it F4 | OPEN |
| FW5 | form-filling-workflow | A PDF copied into a Form filling room is not started: the move branch stores `startFilling` true, the copy branch writes no filling state, so the copy stays hidden from the form fillers while a moved, created or uploaded PDF is started. | red test, rerun 2026-11-18 with the document templates in place (`CopyBatchItems_PdfFromMyDocumentsIntoFillingRoom_StartsTheCopy`, `Category=Bug`); its verification calls it F5 | OPEN |
| A34 | ai-agents | The description of `getRoomAiFolder` says the rooms with skills are listed with `GET api/2.0/files/rooms?withAiFolder=true`, but without `searchArea=AiHub` the flag filters the Rooms section and the answer is empty; with `searchArea=AiHub` it lists the Instructions rooms only. | red test, run 2026-11-18 (`AiRoomOutsiderAccessTests.cs:129`, `Category=Bug`) | OPEN |
| PS-1 | portal-settings | `getSocketSettings` reads the key `web:hub`, a section with no value of its own in the default configuration, so the answer's `url` is empty while `getPortalSettings` reads `web:hub:url`; deployments that set a flat `web:hub` were not checked. | red by code reading | OPEN |
| PS-2 | portal-settings | `saveMailDomainSettings` stores the request's `inviteUsersAsVisitors` in a setting named `InviteAsUsers`, an opposite meaning, and the CodeGraph index shows no reader of it (LSP not run). | red by code reading | OPEN |
| AC-10 | access-settings | `updateTfaAppCodes` does not check that the method is on at portal level while `getTfaAppCodes` does, so with the method off and an application still linked the two disagree. | by code reading | OPEN |
| PG-1 | people-and-groups | `displayName` of a profile comes back HTML-encoded (`O&#39;Hara`) while `firstName`, `lastName` and the stored columns are not, so feeding it back to a search finds nothing. | per test comment | OPEN |
| SI5 | storage-and-integrations | The Yandex entry of `getAllProviders` has key `WebDav` without `requiredConnectionUrl`, while a WebDAV connection without an address is refused (the refusal read only as a comment). | red by code reading | OPEN |
| SI8 | storage-and-integrations | A second `saveThirdPartyBackup` stores the new title without the invalid-character cleaning a new account gets. | red by code reading | OPEN |
| PR2 | private-rooms | A private room or its subfolder can be added to Favorites. | per commit message | OPEN |
| FW2 | form-filling-workflow | By-id read of a hidden form: this spec, the code, the test and `form-filling-room-roles` say 403; an older live run recorded 404 and has not been repeated. | test pins (documentation item) | OPEN |
| Q5 | quotas | A file that does not fit the remaining portal allowance is refused with "The maximum file size is exceeded (<remaining>)", quoting free space as a per-file limit. | static | OPEN |
| FX2 | files-settings | A second person refused by `startEditFile` is told "This document is being edited by you in another tab". | pinned by a test; wording judged by reading | OPEN |
| Q4 | quotas | `FolderQuotaChangeAsync` refuses a quota above `maxTotalSize` or the Standalone portal quota with `InvalidOperationException` (403) and the "greater than the total storage" message, the status of an access refusal; the user-quota sibling answers 400 (`ArgumentException`). The earlier second, unreachable "folder not found" refusal is gone. | red test, run 2026-11-18 | OPEN |
| Q6 | quotas | The `maxTotalSize = 0` branch of `getPortalQuota` for a User is dead code behind the permission demand, yet documented in the yaml. | static | OPEN |
| S3 | webhooks-and-api-keys | `ApiKeyManager.DeleteApiKeyAsync` returns `result >= 0`, i.e. always true. | static | OPEN |
| S4 | webhooks-and-api-keys | `PUT settings/webhook/enable` demands `name` and `uri` in a body it ignores. | static | OPEN |
| S7 | webhooks-and-api-keys | OpenAPI: the `WebhookTrigger` description still stops at `form.stopped` while the enum lists all 38 members; the signature-header part is fixed. | static | OPEN, partly fixed |
| S9 | webhooks-and-api-keys | Unlocking a file of the portal storage raises `file.updated` twice, and the lock call raises it even when nothing changes. | static (documented in the contract) | OPEN |
| S10 | webhooks-and-api-keys | `room.updated`, `agent.updated` and `user.updated` fire on every successful call even without a change, while `group.updated` was narrowed to real changes. | static (test covers the group side) | OPEN |
| S12 | webhooks-and-api-keys | `WebhooksLogDto.route` is the subscription's current URL, so the journal cannot show where a past delivery went. Design smell, documented. | static | OPEN |
| H4 | history-and-audit | A missing body on `POST security/audit/settings/lifetime` is reported as "LoginHistoryLifeTime" 400. | red test, run 2026-11-18 | OPEN |
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
| M13 | data-import | `clearMigration` removes only completed jobs whose `operation` is "migration", so a failed parse stays queued until a new parse replaces it, and `cancelMigration` skips an unknown-migrator job too. | red test, run 2026-11-18 (bug 84792) | OPEN |
| N5 | notifications | `setRoomsNotificationStatus` stores any id forever; no cleanup. | static | OPEN |
| N6 | notifications | SMTP save accepts and drops `useNtlm`; validation is only DTO attributes (TODO in the controller). | static | OPEN |
| N7 | notifications | Telegram texts declared for portal_deactivate, portal_delete, user_has_join, self_profile_updated, backup_failed are never sent; the Files `subject_*_tg` keys are unreferenced. | static | OPEN |
| L7 | branding-and-licensing | `feedbackAndSupportUrl` of the payment settings is always null. | static | OPEN |
| L11 | branding-and-licensing | The licensor flag of the company details is always stored as `false`. | static | OPEN |
| PR6 | private-rooms | The neutral room title exists in the default culture only, so a letter in another language names the room in that default language. | per commit message | OPEN |
| T9 | portal-lifecycle | A suspended portal answers 404 to `getPortalInformation`, so a client cannot read `status` to learn why, although the yaml tells it to. | static | OPEN |
| A13 | ai-agents | `assertAiAccess` borrows the AI-off gate through a throwaway tool-prefs read instead of a gate of its own, now on four routes. | static | OPEN |
| A21 | ai-agents | In an export, `/`, `\` and the other characters the Files API replaces in a title are not replaced the same way (`Notes 12/31` → `31.docx`; bug 82711). Two simultaneous exports with one title now get distinct names (bug 84459). | red test, run 2026-11-18 (bug 82711); the test of bug 84459 is green | OPEN, narrowed |
| A22 | ai-agents | An export keeps TeX source in the document, and a stored provider failure stays English under any culture. | red test, run 2026-11-18 (bugs 83038, 83046; code not read) | OPEN |
| A25 | ai-agents | The profile create answer has `createdAt` milliseconds no later read reproduces. | red test, run 2026-11-18 (`ProfileReadTests.cs:47`) | OPEN |
| W7 | billing | `setParticipantServiceLimits` does up to 1000 sequential user lookups per call. | static | OPEN |
| W11 | billing | `getDepositInvoice` walks every top-up of the portal, 1000 a page, until the id turns up; an id that does not exist costs the whole walk. | static (unit test) | OPEN |
| AP5 | apps | A bindable body without the `settings` member drops the stored document with 200, indistinguishable from an explicit reset. | test pins | OPEN |
| AP4 | apps | The JSON re-parse and its 400 in the settings write are unreachable; the real 400 comes from model binding. | static | OPEN |
| AP6 | apps | A socket event is sent on switching an application, none on saving its settings; the controller text documents it as intended. | static | OPEN |
| AP7 | apps | A switch of an application leaves an audit event since bug 84747, but `setSettings` leaves none, including the accidental drop of AP5. | test pins, run 2026-11-18 (`AppsAuditTrailTests.SetSettings_SaveAndDrop_LeaveNoEvent`) | OPEN |
| MD4, MD5, MD6 | metadata | Empty `templateIds` on a file still writes audit and socket (the folder variant returns early); cascade-off writes no audit while on/unassign do; a no-op custom-field removal still audits, sockets and re-indexes. | static | OPEN |
| MD9 | metadata | The yaml lacks the 403 row of `getCascadeProgress`; the number-limit text and the SDK listing models are fixed (`7d641bf729`). | static | OPEN, partly fixed |
| FX9 | files-settings | `filterValue` with an uppercase letter matches nothing in `getGroupsMembersWithFolderSecurity` and `getGroupsMembersWithFileSecurity`, per the yaml text. | yaml text only | OPEN |
| R5 | room-role-permissions | `updateFileComment` (version comment) is refused for an Editor; design question, kept outside the Comment row. | static | OPEN |
| FS1 | file-sharing | **Behaviour, not a defect**, recorded so nobody files it as one: `addFavorites` / `deleteFavoritesFromBody` skip a file the caller cannot read and answer `true`, the same as for a readable one; the spec states the skip. | test pins | NOT A DEFECT |
| — | user-type-permissions | Matrix row "copy of an archived room" is allowed by the matrix but `room-types` refuses duplication of an archived room; kept MATRIX-ONLY and flagged, neither spec changed. | static | not re-checked |
| AC-11 | access-settings | `getIpRestrictions` and `getSupportedCultures` compute the ETag by hashing a `MemoryStream` that is never rewound, so every list gets the same ETag (MD5 of zero bytes): after the IP list changes, a request with the old ETag still gets 304 and the client keeps the stale list. | red test, run 2026-11-18 | OPEN |
| PS-7 | portal-settings | A refused `saveMailDomainSettings` (Custom list with a bad domain) and a refused `saveDnsSettings` (name mapped by another portal) change the cached portal object before the check: the database keeps the old value, but the portal then reports the refused value until the cache expires. | red test, run 2026-11-18 | OPEN |
| R11 | room-role-permissions | Since `fcd31cbadf` the yaml says `archiveRoom`/`unarchiveRoom` need the room manager role and refuse a non-invited DocSpace admin; the server refuses an invited Room manager on both (403) and lets a non-member DocSpace admin restore. The yaml text is wrong. | tests (`RoomArchiveRoomManagerTests.cs:46,62`, `RoomArchivePermissionsTests.cs:200`) | OPEN (documentation) |
| A11, A35 | ai-agents | `aiPromptsDelete` answers success for another user's prompt and does not delete it (bug 82809); the yaml describes it as intended. One defect under two numbers of the `verification.md`. | red test, run 2026-11-18 (bug 82809, `PromptIsolationTests.cs:108`) | OPEN |
| FW7 | form-filling-workflow | The release's own unit test `GetTableColumns_DecidedVersionWithoutSubmissions_AddsNoColumns` fails: `FormFillingReportCreator.MatchFields` returns null for a field added in a decided, never-filled version, so the rename into it gets no column. | `FormEditorSessionsTests.cs:325` red in the full run and alone, deterministic (unit test, commit `c330e3222e`) | OPEN (release regression) |
| F30 | file-operations | `deleteRecent` with a folder id sends `delete` of that folder to the parent folder's socket room, so open clients take the folder off its list although it stays there. | `SocketFolderEventsTests.cs:113` red in the 2026-11-07 and 2026-11-14 runs, also alone; no bug mark yet | OPEN |

## Red in the run of 2026-11-18, no row yet

These Bug-marked tests failed in the run of 2026-11-18 at the step that states the defect, and no row of this file
names them; the Files tests among them failed again in the rerun 2026-11-18 with the document templates in place. None was analysed: each is a
candidate for a row with a spec number, or for a fix of the test.

| Spec | Test (bug) | What fails |
|---|---|---|
| webhooks-and-api-keys | `CreateApiKey_NonBmpName_IsStoredUnchanged`, `UpdateApiKey_NonBmpName_IsStoredUnchanged` (84484) | A key name with a character outside the BMP answers 500 "An error occurred while saving the entity changes". |
| invitations | `CheckConfirm_EmpInviteWithoutEmployeeType_DoesNotSurfaceInternalException` (84502) | The answer carries the internal message "Nullable object must have a value". |
| access-settings | `GetIsAuthentificated_MalformedBasicHeader_ReturnsFalse` (84500) | A malformed Basic header answers an error instead of 200 with `false`. |
| file-operations | `GetFolder_ByMemberWithoutOpeningTheFile_KeepsTheFilesNewMark` (84797) | The "new" mark of the file is gone after the member only opened the folder. |
| ai-agents | `Ask_ASavedAllowAlways_Wins` (84298) | A saved "allow always" does not win over "ask". |
| ai-agents | `Send_OtherProfileInProject_CannotOverrideTheFixedModel` (82914), `Create_ThreadInProjectOnAnotherModel_DoesNotStoreIt` (82915) | A round or a thread of a project runs on a model other than the project's; may be part of A16. |
| ai-agents | `Create_ProjectOnAPortalWithNoModelAnywhere_IsRefusedForWantOfAModel` (`Category=Bug`) | No 409 when the portal has no model for the project. |
| history-and-audit | `CsvReport_Owner_HasNoCountryCityOrPageColumn` (`Category=Bug`) | The owner's CSV report of a folder's history has Country, City and Page columns. |

## Web client vs server (live check 2026-11-14, report only)

The web client decides these actions by its own rule, not by a server flag. Checked on the `test` stand through the UI (Playwright) and the API. The server side follows the specs; the fix belongs to the client.

| # | Spec | Client | Server | Verdict |
|---|---|---|---|---|
| CL1 | invitations | A Room admin is offered "Invite again" for a pending Room admin invited by the Owner. | `resendUserInvites` answers 200 and lists the account, but sends no letter; the same resend by a DocSpace admin sends one. | CLIENT BUG (offers what silently does nothing) |
| CL2 | invitations | A DocSpace admin is offered "Invite again" for a pending DocSpace admin. | 200, no letter; the Owner's resend sends one. | CLIENT BUG (offers what silently does nothing) |
| CL3 | shared-links | The link edit panel of a Form filling room always preselects FillForms: setting only a password on a Read link and saving rewrites its access to FillForms. | The API keeps the access it is sent. | CLIENT BUG (changes data) |
| CL4 | room-types | The room owner's context menu of a Form filling room has no "Archive". | `PUT rooms/{id}/archive` by the same user answers 200 and the room moves to the Archive. | CLIENT BUG (hides what works) |
| CL5 | shared-links | The link edit panel of a Form filling room has no deny-download toggle. | The API accepts `denyDownload=true` on that link (200, stored). | CLIENT BUG (hides what works) |
| CL6 | people-and-groups | The Owner's menu of a disabled Guest has no "Reassign data". | `POST people/reassign/start` answers 200 and the job completes with no error. | CLIENT BUG (hides what works) |

Checked and agreeing: a DocSpace admin changes a Guest's email (spec corrected), the Owner demotes a Room admin and a DocSpace admin to Guest (spec corrected), a non-member DocSpace admin gets the share action of an empty Public room from the header only, and a demoted DocSpace admin's settings page leaves for /error/401 within about 6 s while a save answers 403.

## Closed by v4.1.0

Rows whose `verification.md` verdict is FIXED or REFUTED, including those that were found and fixed within the
range, and R4 and N4, whose bug tests turned green in the run of 2026-11-18. Rows whose evidence ends with "tests
green, run 2026-11-18" were confirmed by that run; the others were read, not run. "Submodule" is a commit of `sdk/docspace-api-spec`, the regenerated contract.

| # | Spec | What the server did | Evidence | Status |
|---|------|---------------------|----------|--------|
| P9 | plugins | At the plugin count limit the upload looked the new name up with `GetWebPluginByNameAsync`, which threw, so the caller got 400 "Wrong plugin archive. Plugin not found" and never the `ErrorWebPluginMaxCount` text. The limit check now compares the new name with the names of the installed plugins. | test green on 2026-11-23 against "Wrong plugin archive. The number of plugins exceeds the limit"; rewritten for the text without the prefix (`fa8d565c1b`) and green again on 2026-11-25 (`WebPluginReuploadTests.Upload_PortalAtThePluginLimit_SaysTheLimitIsReached`) | FIXED `410d6a20f6`, text `fa8d565c1b` |
| AC-1 to AC-4 | access-settings | `saveSsoSettingsV2` answered 500 for no IdP entity ID (bug 84685), a missing or invalid SSO URL (84686), an incomplete field mapping (84687) and a malformed SLO URL (84688); each now answers 400. | commit read, not run; tests green, run 2026-11-18 | FIXED `f3a52c35dd` |
| AC-5 | access-settings | `sendSmsCode` with valid credentials and SMS off for the account answered 500, the thrown exception type having no mapping (bug 84503). | commit read, not run; tests green, run 2026-11-18 | FIXED `f5f2735e30` |
| F22 | file-operations | `updateFile` on a file locked by somebody else was refused through a 500 instead of a client error; it now answers 403 (bug 84842). | tests (`LockedFileStateTests.cs:147`, `FileRenameLockedTests.cs:110`); tests green, run 2026-11-18 | FIXED `77db3a6128` |
| R10 | shared-links | After a file's primary link was revoked, `createFilePrimaryExternalLink` answered 404 from then on, while `setFileExternalLink` could still make one (bug 84872). | commit read, not run; tests green, run 2026-11-18 | FIXED `62419af921` |
| FX3 | files-settings | Same bug as R10, on `createFilePrimaryExternalLink` and the folder one: create shared the read path, so after a revocation the POST answered 404 (bug 84872). | commit read, not run; tests green, run 2026-11-18 | FIXED `62419af921` |
| AP1 | apps | The `app_settings` table was in no backup module, so a backup dropped every application flag and settings document. Backups taken before the fix still hold no rows; the full cycle has no test. | test prepares the row | FIXED `9e696831d5` (bug 84721) |
| F8 | file-operations | `markAsRead` on the My documents root cleared the "new" marks of an unrelated room. | tests (bug 83509); tests green, run 2026-11-18 | FIXED `73aa4e8ab8` |
| F7 | file-operations | A room member with Editing rights got 403 from `checkConversionStatus`. | tests (bug 81825); tests green, run 2026-11-18 | FIXED `73aa4e8ab8` |
| F5 | file-operations | Aborting an upload session that never existed answered 500 instead of 404. | tests (bug 82278) | FIXED `73aa4e8ab8` |
| F19 | file-operations | Creating a `.docx`, a text file or an HTML file directly in a Form filling room was refused through a plain `Exception` → 500; it now answers 403 and stores nothing, on the upload and session routes as well. | code and tests read, not run (bug 84840); tests green, run 2026-11-18 | FIXED `1a1d539775` |
| FW1 | form-filling-workflow | `POST files/{folderId}/file`, `/text` and `/html` with a non-PDF in a Form filling room were not refused with a client error; they now answer 403 and store nothing. | code and test read, not run (bug 84840); tests green, run 2026-11-18 | FIXED `1a1d539775` |
| D8 | form-filling-room-roles | A docx uploaded or created in a Form filling room answered 500; it now answers 403, the same as `fileops/copy` of that docx. | code and tests read, not run; same root as F19 | FIXED `1a1d539775` |
| F3 | file-operations | `copyBatchItems` with a folder id that does not exist answered 500. | test asserts 404 (bug 84435); tests green, run 2026-11-18 | FIXED `184d90789e` |
| F27 | file-operations | `moveBatchItems` with a folder id that does not exist answered 500. New row, fixed through the shared check of F3; no commit names bug 84436. | code and test read, not run; tests green, run 2026-11-18 | FIXED `184d90789e` |
| F4 | file-operations | `moveBatchItems` with a file id that does not exist answered 403 instead of 404. | tests; the test's "403 today" comment is stale; tests green, run 2026-11-18 | FIXED `01d0b7713d` (bug 82204) |
| F28 | file-operations | A file moved with `Duplicate` onto a same-named file kept the clashing title. New row. | code and test read (bug 84437); test green on the rerun 2026-11-18 with the document templates in place | FIXED `77dd46d96a` |
| F2 | file-operations | `POST files/thumbnails` without `fileIds` answered 400 with a stack trace instead of 200 and an empty list. Fixed before the base; the earlier row was stale. | tests | FIXED `a79b825223` |
| F25 | file-operations | `checkMoveOrCopyDestFolder` counted a repeated id as refused, `checkMoveOrCopyBatchItems` ignored `content`, and both accepted the roots the copy refuses. New row. | guards read in the code, not run | FIXED `e9cfda8d43` |
| F26 | file-operations | An `outputType` the portal cannot produce was queued and silently converted to the internal format. New row. | guards read in the code, not run | FIXED `e9cfda8d43` |
| F29 | file-operations | A numbered part without a file part answered 500, part 0 or -1 was stored under a name never read, and `fileSize` -1 opened a session that could never finish. New row. | guards read in the code, not run | FIXED `e9cfda8d43` |
| F13 | file-operations | The move/copy pre-checks and `deleteFileVersions` could not be called through the generated SDK clients. | tests are typed now (bug 84727); tests green, run 2026-11-18 | FIXED `06eecdbd77`, `632c2fc262` |
| A3 | ai-agents | Agent create left a room behind when the binding failed. | route gone | FIXED by removal `ecf76402b3` |
| A4 | ai-agents | An image-only Default slot was handed to Chat; Default still takes it, but resolve answers 404 and try-resolve null. | tests | FIXED `d59923d4a0` |
| A5 | ai-agents | `aiThreadsRegenerateTitle` answered 500 on a thread with a real conversation; the TS `test.fail` is stale. | test comment | FIXED `876875d79d` |
| A8 | ai-agents | MCP replace-all stored two names differing only in case. | test comment "used to" | FIXED `b6d11f1999` (library update) |
| A10 | ai-agents | OpenAI passthrough answered 500 with a stack trace on a large body and on malformed image requests. | commit and code; no .NET test | FIXED `9768f0a6f3`, `e84932692d` |
| A36 | ai-agents | The draft cleanup pass read the files of unlinked drafts first and deleted them by id afterwards, with no lock against `aiAttachmentsLinkToMessage`: a link between the two steps kept the attachment on the message and lost its file, or answered 500 when the move found the file gone. The pass now marks the drafts deleted with a conditional update before it reads their files, and reads and deletes only marked drafts, which a link no longer takes. | code read, not run; the interleaving has no test | FIXED `a1f6ce3988` |
| B1 | backup-and-restore | `deleteBackup` of an unknown id answered 500; it now answers 404 and the yaml still says 500. | test | FIXED `61ff4b1ee4` (bug 80605) |
| T1 | portal-lifecycle | `Blocked` was missing from the published `TenantStatus`, `PortalUnblock` from `ConfirmType` and `PUT portal/unblock` from the paths. `portal/portalrename` is still absent: see T5. | yaml | FIXED submodule `5ea4dc6` |
| W3 | billing | Nine `servicelimit` operations and the `aiServicesLimit` profile field were missing from the published document and the SDKs. | yaml | FIXED submodule `5ea4dc6` |
| FS2 | file-sharing | `changeDefaultAccessRights` accepted an out-of-range number (bug 82641); the request model now answers 400. The `verification.md` calls it "not a defect" and names no commit. | green test by its assertion, not run; tests green, run 2026-11-18 | FIXED, no commit named |
| A6 | ai-agents | A file id and the Trash root are accepted as thread `entityId`: by design, with a reopen decision on bug 82719. | test; tests green, run 2026-11-18 | REFUTED |
| OA1 | oauth2-applications | `updateClient` accepted a `name` outside 3 to 256 characters (200), while `createClient` refused it and the yaml declared the same bound on the update (bug 81670). | per test comment; tests green, run 2026-11-18 | FIXED (tests green 2026-11-10, no commit named) |
| OA2 | oauth2-applications | `getPublicClientInfo` with an unknown id did not answer the 404 the yaml declares (bug 81728). | per test comment; tests green, run 2026-11-18 | FIXED (tests green 2026-11-10, no commit named) |
| R4 | room-role-permissions | `POST folder/{id}/link` by a Content creator, Viewer, non-member admin, Guest or on an archived room answered 200 with an empty list instead of 403 (bug 81575). | tests green, run 2026-11-18, and already in run 365d87fe60; the fix of 2026-05-24 refuses with 403 when the caller may not manage the access (`FileStorageService.cs:4540-4543`); the row stayed open on the tests' stale "currently answers 200" remark | FIXED `021004d5bd` |
| N4 | notifications | A file renamed by somebody else (`updateFile` with a new title) was not reported as new (bug 81713). | bug tests green, run 2026-11-18; by code reading, a rename has marked the file new for the others since `aab44c81f8` (`FileStorageService.cs:2436-2440`), a commit that names other bugs; both bug tests rename, a content update by somebody else has no bug test | FIXED `aab44c81f8` |
