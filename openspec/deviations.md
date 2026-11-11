# Known deviations between the specifications and the server

Last updated 2026-11-10: integration tests for the CODE-ONLY rows (test agents, run 2026-11-10) on top of the revision of
range `bb406b409b..6fb013e745` (verdicts read from the commits, not run). Statuses otherwise come from the `verification.md` of the 30 specs (`room-types` has no defect section);
earlier checked ranges `1f1b052f7d..0ebdbb7c84` and `d58816bece..3533ad8a89`.

The specifications in `specs/` describe the server as it behaves today (checked against the integration
tests, a static reading of the code and a live run against a local portal, October 2026). This file keeps
what was deliberately left out of them: behaviour that looks like a defect rather than a design decision,
and places where the product matrix ("Types and Roles. Access rights", "Shared link rules") describes
something the server does not do. Each item names the specification it would belong to; file:line evidence
stays in that specification's `verification.md`.

Status: **OPEN** (still open, or new in this revision), **FIXED** with the commit, **REFUTED**, or **not
re-checked** when no `verification.md` gives a verdict. FIXED and REFUTED rows are in "Closed by v4.1.0" at the
end. The `verification.md` of the seven new specs (people-and-groups, private-rooms, files-settings,
storage-and-integrations, oauth2-applications, portal-settings, access-settings) gives an evidence kind and no
status column; their rows are OPEN as new in this revision.

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
| D9 | user-type-permissions | `GET /api/2.0/portal/users/invite/{type}` never refuses: a disallowed pair answers 200 with an empty string, and for the Guest type it returns a real link to the Owner, a DocSpace admin and a Room admin, although `POST /portal/users/invitationlink` and the people endpoints refuse Guest for everyone. | live run | OPEN |
| D10 | user-type-permissions | `PUT /api/2.0/people/delete` by a DocSpace admin on a disabled DocSpace admin answers 200 with the account echoed and deletes nothing; `DELETE /people/{id}` answers 403. | live run | OPEN |
| D11 | shared-links | Files and folders in My documents, Custom, Collaboration and VDR rooms accept up to 6 *primary* links in addition to 6 additional ones (`primary:true` with a new link id succeeds up to 6 times). Rooms and Public-room entries keep one primary link. | live run; `FileSecurity`/`FileSharing` link limits | OPEN |
| D12 | shared-links | A primary link of a folder inside a Form filling room stores an expiration date on create; later updates and revocation are skipped unless the stored date is echoed. | live run | OPEN |
| D13 | form-filling-room-roles | The "only ONLYOFFICE PDF forms" rule is enforced by extension only (`.pdf`), so an ordinary PDF enters the room and is auto-started on move; the form-signature check exists only in the advisory `checkdestfolder`. | live run; `FileDao`, `CopyPermissionsCheck` | OPEN |

**Recorded contradiction, narrowed (hidden form read by id).** `form-filling-workflow` and its test (FW2) and
`specs/form-filling-room-roles/spec.md` (scenario "Form Filler does not see a form that is not started") say 403,
and the `verification.md` of the roles spec calls 403 the asserted value. The contradiction left is only with the
old live-run text of D6, which said 404. Nothing has been run since the live run, so the D6 text above stays as
recorded.

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

From 2026-11-10 many defects are backed by integration tests marked `Category=Bug` that fail on purpose until the
defect is fixed; their rows carry the evidence "red test, run 2026-11-10".
`form-filling-workflow` numbers two items F1 and F2 that have nothing to do with F1 and F2 of `file-operations`;
this register labels them FW1 and FW2, and its F3 is FW3 here. Within a table, rows are ordered by how much they matter.

Evidence column:

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

### Access and integrity

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| A15 | ai-agents | `aiProfilesList` hands the provider key to every member: Chat sends `forClient=true`, which the storage controller does not read, and the service does not strip it. | red test, run 2026-11-10 | OPEN |
| H8 | history-and-audit | A CSV folder report (`POST files/folder/{id}/log/report`) carries Country, City and Page, and the IP, browser and platform of every event even for a room member who is not a DocSpace admin: `AuditCsvReportWriter` maps every column and ignores the column set built for the folder report; only the XLSX writer applies it. | red test, run 2026-11-10 (`FolderHistoryReportColumnsTests.cs`, `Category=Bug`) | OPEN |
| A33 | ai-agents | A DocSpace admin who is not a member of an AI project reads its model, sources and skills (200), while the requirement bars him and the chat service refuses him a round in the project; the project read check lets him through. Either the Files check or the requirement is wrong. | red test, run 2026-11-10 (`AiRoomOutsiderAccessTests.cs:55`, `Category=Bug`) | OPEN |
| A18 | ai-agents | `aiWebSearchGetActiveConfig` returns the stored web-search key to a User. | red test, run 2026-11-10 | OPEN |
| A20 | ai-agents | The MCP read routes redact only system servers, so a custom server's Authorization header reaches a User and a Room admin. | red test, run 2026-11-10 (`get-custom-server` only; `list-custom-servers` did not carry the header) | OPEN |
| A19 | ai-agents | `aiAssignmentsResolveForAction` echoes the profile with its key. | red test, run 2026-11-10 | OPEN |
| A31 | ai-agents | A BYOK key on a model restricted through `setRestrictedAiModels` adds the model back to the catalogue; the AI service applies no restriction of its own. | red by code reading | OPEN |
| W13 | billing | `topUpDeposit` answers false for a failed or unreadable billing answer, the same as a decline, although the deposit may have gone through; a client that retries can charge the payer twice. | red test, run 2026-11-10 | OPEN (left open on purpose by `a4c952dda4`) |
| R2 | user-type-permissions | A DocSpace admin can deactivate another DocSpace admin and the Owner. | red test (bug 79900) | OPEN |
| L8 | branding-and-licensing | `acceptLicense` and `refreshLicense` demand no right: any signed-in member of any portal of the installation, a Guest included, can move a staged licence into place, rewrite the installation's quota and tariff and write the audit event under their name. | red test, run 2026-11-10 | OPEN |
| T8 | portal-lifecycle | `deleteportalimmediately` lets any owner whose address matches the configured test-address pattern delete the portal with an ordinary session and no confirmation; a loose pattern turns every owner of a domain into a one-call deleter. | static | OPEN |
| T6 | portal-lifecycle | The rename answers with a confirmation sign-in link of type `Auth` for the caller in the body of a 200, i.e. a credential-bearing URL that proxies log. | static | OPEN |
| SI1 | storage-and-integrations | `updateStorage` and `resetStorageToDefault` put the portal into Migrating and publish a request nothing in this repository subscribes to, so the portal looks stuck and the settings are never saved; a consumer outside the repository cannot be excluded. | red by code reading | OPEN |
| SI2 | storage-and-integrations | `updateCdnStorage` answers with the new `module` and `props` but never saves them, and nothing subscribes to the message it publishes. | red by code reading | OPEN |
| PR4 | private-rooms | Backup skips key pairs and file keys and a module restore assigns new user and file ids, so a restored private room's content cannot be opened, and nothing read warns about it. | red by code reading | OPEN |
| PR5 | private-rooms | A per-user security check on one entry instance reuses the share record of the previous user; the private-room paths reset it and the core is unchanged, so another caller checking several users answers for the wrong one. | per commit message | OPEN |
| AC-6 | access-settings | `updateTfaSettings` with a numeric `type` outside the known values takes the `None` branch, so two-factor is switched off and the answer is 200 false. | by code reading | OPEN |
| AC-7 | access-settings | `getTfaSettings` has no role check: any signed-in role receives `trustedIps`, `mandatoryUsers` and `mandatoryGroups`. | by code reading | OPEN |
| PG-3 | people-and-groups | `getRemoveProgress` and `terminateRemove` demand only the edit-user permission, so a DocSpace admin can read or cancel a removal of another DocSpace admin, the gap bugs 78957 and 79320 closed for reassignment. | red by code reading | OPEN |
| PG-4 | people-and-groups | `sendCongratulations` accepts the key of any account, not only the Owner's, and then switches the configured second factor on for the whole portal; whether a non-owner can hold such a key was not checked. | red by code reading | OPEN |
| PG-5 | people-and-groups | `signupThirdPartyAccount` downloads the provider's avatar URL with a plain client, without the URL validation `updateMemberPhoto` and `addMember` apply (bug 81491). | red by code reading | OPEN |
| SI6 | storage-and-integrations | `saveThirdParty` does not check a WebDAV `url` for loopback, link-local or private ranges before the outbound request (bug 82560, test skipped while it is open). | per test comment | OPEN |
| FX5 | files-settings | The anonymous answer of `getDocServiceUrl` includes the internal Document Server address, the portal address as the service sees it, the signature header name and the SSL flag. | red by code reading | OPEN |
| FX1 | files-settings | Any refused `startEditFile` drops the file's whole tracker, so a second person's refused claim or a reader's refused start ends the first person's editing mark and the next claim succeeds. | red by code reading (no test covers the state after the refusal) | OPEN |
| A16 | ai-agents | A chat round is not bound to the project for a read member: the context drops the folder and the fallback reads a null `type`, so the round runs on what the composer sent, without the project's model and sources. | red test, run 365d87fe60 (cause by code reading) | OPEN |
| A27 | ai-agents | An invalid tool mode from a Guest or while AI is off answers 400, not 403: validation comes first. | red test, run 365d87fe60 (bugs 84303, 84302) | OPEN |
| A28 | ai-agents | More attachments than the composer allows are answered 200 and stored whole (11 stored). | red test, run 365d87fe60 (bug 82894) | OPEN |
| A29 | ai-agents | `aiThreadsCreate` naming another profile stores it on a project thread. | red test, run 365d87fe60 (bug 82915) | OPEN |
| A30 | ai-agents | A round with a draft, inline record, device file or image attachment ends healthy but no streamed request carries the text; the stand's part was not traced. | red test, run 365d87fe60 (bug 82773) | OPEN |
| A17 | ai-agents | `aiThreadsCreate` never sees a project because of the same null `type`: a DocSpace admin outside it creates a thread, and no model answers the generic 404, not 409. | red by code reading | OPEN |
| F24 | file-operations | The upload part and finalize routes find a session by id alone: a caller without access to the target folder who holds the id writes into and finalizes the Owner's session; only abort checks the owner. Security smell, design decision pending. | red test (`Category=Bug`); run date not stated | OPEN |
| R7 | shared-links | `getFilePrimaryExternalLink` answers 200 to a caller without access to the file. | test passes, run 365d87fe60 | NOT REPRODUCED; no fixing commit identified |
| R4 | room-role-permissions | `POST folder/{id}/link` by a Content creator, Viewer, non-member admin, Guest or on an archived room answers 200 with an empty list instead of 403. | red test (bug 81575) | OPEN |
| F23 | file-operations | `getFileVersionInfo` answers the version list to a member invited with Editing access, although the document says only managers and content creators read the history (see H6). | red test, run 2026-11-10 | OPEN |
| H6 | history-and-audit | Same bug as F23, recorded differently: the version list is open to Editing through the read-history right, the new test contradicts the sibling `EditHistoryPermissionsTests`, and the spec follows the server. | red test, run 365d87fe60 | OPEN |
| F21 | file-operations | `saveediting` into a file open in somebody else's editing session answers 200, replaces the content, grows the version and clears the editing flag (bug 84769); the 500 of `startedit` on a file locked by somebody else is fixed in `bb406b409b..6fb013e745`. | red test, run 2026-11-10 (`saveediting` failed on the bytes first; the 200, version and flag rest on the test's summary) | OPEN, narrowed |
| H3 | history-and-audit | The owner check runs only when the login event has a `UserId`, but no caller can create such an event: a failed sign-in under an unknown login is stored with no row a stranger can close, so a User cannot close another's connection through the API. | probe test `LogOutActiveConnection_FailedSignInOfUnknownLogin_UserCannotCloseIt` passes, run 2026-11-10 | NOT REPRODUCED; the check stays conditional in the code |
| S2 | webhooks-and-api-keys | `PUT keys/{id}` on an expired key answers `200 false` before the ownership check: a non-owner learns that a foreign key exists and is expired instead of 403. | red test, run 2026-11-10 | OPEN |
| S1 | webhooks-and-api-keys | Updating an API key with `permissions: ["*", "files:read"]` is stored as-is and the auth handler then grants unrestricted `*:write`; creation normalises the same input to "unrestricted". | red test, run 2026-11-10 | OPEN |
| S8 | webhooks-and-api-keys | The generated C# SDK writes `"ssl": false` when the caller does not set `Ssl`, so an update through it switches certificate checking off; the server is right and the SDK has to be regenerated. | static (test remarks) | OPEN |
| F1 | file-operations | `POST files/thumbnails` is `[AllowAnonymous]`; an anonymous caller without a link is accepted although the API document says 401. | red test (bug 81268) | OPEN |
| F9 | file-operations | `markAsRead` has no per-item access check: a non-member marks a room file as read with 200; a file the caller cannot read is passed over in silence. | test pins | OPEN, design question |
| PR3 | private-rooms | Audit events written before `b562fc3fb1` keep showing a private room's titles, because the list of titles to hide is stored with new events only. | per commit message, code reading of the mask | OPEN |
| PR1 | private-rooms | Deleting a subfolder of a private room still sends it to Trash. | per commit message | OPEN |
| Q2 | quotas | Moving a file between two accounts' My documents silently raises the receiver's custom user quota by the file size. | red test, run 2026-11-10 | OPEN |
| S6 | webhooks-and-api-keys | The "limited Developer Tools access for Users" setting also blocks Room admins, and now drops the events of their subscriptions; notes and endpoint text say Users only. Intent unclear. | test pins (bug 81236) | OPEN, widened |
| A1 | ai-agents | Vectorization answers `AlreadyProcessed` before any right check, so an already processed file answers 200 to anyone, queues nothing and shows the file exists; a pending file is refused. | static (by code reading, no .NET test) | OPEN, narrowed |
| A11 | ai-agents | Deleting another user's prompt answers success although nothing is deleted. | red by code reading (the engine between the two files was not read; the test comment says "used to") | OPEN |
| A26 | ai-agents | A failed model call on the metadata routes relays the provider's text for every failure, while `aiSkillsGenerate` was narrowed because an authentication failure quotes the masked API key; whether that text reaches a caller without provider rights was not traced. | red by code reading | OPEN |
| P5 | plugins | `getWebPlugins`/`getWebPlugin`, callable by any member incl. Guests, rewrite the portal plugin settings and emit a socket event when a stored value fails to decrypt: a GET mutates portal settings. | static | OPEN |
| P1 | plugins | A manifest with `"enabled": true` is reported as on for every portal without a stored row, and switching it off deletes the row so the manifest value returns: the plugin cannot be switched off; manifest `createBy`/`createOn` are trusted. | red test, run 2026-11-10 | OPEN |
| M1 | data-import | `migrationFileUpload.ashx` answers 200 for every outcome incl. "Access denied." and an upload without `Init`; no size limit; outside OpenAPI. | red test, run 2026-11-10 | OPEN |
| M10 | data-import | `migratorName` of the body is trusted over the parsed job's migrator, so a wrong name re-parses the upload with the wrong migrator and fails late. | static | OPEN |
| MD7 | metadata | Section and rooms listings parse the metadata filter before any access check, so any member probes template and field ids through the 400 texts. | static | OPEN |
| N1 | notifications | The room-deleted letter checks the subscription and mute list of the deleter instead of each recipient. | red test, run 2026-11-10 | OPEN |
| N2, N8 | notifications | Form submitted/filling letters and the submitted push bypass the rooms-activity subscription and the room mute that every other room letter applies. | red test, run 2026-11-10 | OPEN |
| B4 | backup-and-restore | Restore from a Documents file demands the create right on the folder although reading is all it needs. | static | OPEN |
| B11 | backup-and-restore | `backup/backuptmp` returns the worker's absolute temporary folder to any DocSpace admin. | static | OPEN |
| B12 | backup-and-restore | The portal-wide 403 while restoring is a path-suffix allowlist (`portal`, `settings`, …); any route ending in one of those words passes. | static | OPEN |
| F14 | file-operations | A session below `chunkUploadSize` still stores the file on the first part, so a truncated file is listed and each repeated part adds a version, while a second finalize, a part after finalize and an abort after finalize are fixed by `ae457bdff6`. | red test, run 2026-11-10 | OPEN, narrowed |
| F15 | file-operations | Finalize without any part, or of a 100-byte session that received 300 bytes, stores nothing though it reports a file; a short intermediate part leaves a file listed after finalize. | red test, run 2026-11-10 (short part: a file is listed after finalize, the status was not reached) | OPEN |
| MD10 | metadata | A character outside the Basic Multilingual Plane in a template name, field name, option, string value or custom field fails on write with a 500 (`An error occurred while saving the entity changes`); the earlier "echoed intact, stored as `?`" was wrong on this run's MySQL stand. | red test, run 365d87fe60 | OPEN |
| F17 | file-operations | A version comment holding a character outside the Basic Multilingual Plane fails on write with a 500 from MySQL (`Incorrect string value`); the earlier "stored as `?`" was wrong. | red test, run 2026-11-10 | OPEN |
| F16 | file-operations | A terminate that arrives while the copy still waits for a free worker thread is dropped, so the copy runs to its end when the queue is busy. | red test, run 365d87fe60 (the failure is at the record lookup: the record is gone right after the terminate; the rest per the test's remarks) | OPEN |
| F18 | file-operations | A batch move of several folders is refused or fails as a whole when one of them holds a file open in an editing session. | red test, run 2026-11-10 | OPEN |
| S11 | webhooks-and-api-keys | The room and agent quota endpoints accept the id of a plain folder (or an ordinary room for the agent ones) and write a quota on it. Belongs to `quotas` as well. | static (contract text) | OPEN |
| B18 | backup-and-restore | A webhook-journal statement that keeps failing after five attempts is skipped with a log line, so a dump restore can silently lose part of the journal while reporting success. | static | OPEN |
| AP3 | apps | The first write freezes the then-current configured `enabled` default into the row; a portal that only saved settings stops following later configuration changes of the default. | static | OPEN |
| MD8 | metadata | Copied values are pasted after stamping without the destination's conflict mode, so an overwrite-cascading destination does not overwrite a copy as it does a move. | static | OPEN |
| MD2 | metadata | The stamp pass after a move is invisible to `getCascadeProgress` and writes no audit. | static | OPEN |
| T3 | portal-lifecycle | Owner change promotes the new owner before `ownerId` is saved; a failure leaves a promoted member and the old owner. | static | OPEN |
| T5 | portal-lifecycle | Rename saves the alias, writes the audit and logs everyone out before the hosting cache is updated; a failure leaves a hosted portal reachable under neither name. The action is still absent from the contract. | static | OPEN |
| P2 | plugins | Plugin state is saved and the socket event sent before the CSP save; on the header-size overflow the caller gets 403 while the plugin is installed and enabled. | red test, run 2026-11-10 | OPEN |
| FX6 | files-settings | `setFilesOrder` applies items one at a time with no transaction, so a later item's 403 or 404 leaves earlier items reordered while the caller sees an error. | red by code reading | OPEN |
| SI4 | storage-and-integrations | A storage migration, if it were started, marks the task failed on failure but leaves the portal status at Migrating. | red by code reading | OPEN |
| PG-2 | people-and-groups | `updateGroup` takes any non-null `groupName`, so a blank name would be stored while `addGroup` refuses it with 400; `SaveGroupInfoAsync` itself was not read. | red by code reading | OPEN |
| W6 | billing | Service-limit writes commit to accounting before the audit record and socket event; a late failure answers 500 with the limit already changed. | static | OPEN |
| W4 | billing | `moveSubscriptionToWallet` compares a price in the request region's currency with a USD wallet balance. | static | OPEN |
| W5 | billing | `setTenantWalletSettings.currency` is stored unchecked; auto top-up silently never fires when it matches no sub-account. | red test, run 2026-11-10 | OPEN |
| L1 | branding-and-licensing | Saving logos from image data returns instead of continuing on an empty entry, silently skipping every later slot while answering `true`. | static | OPEN |
| L3 | branding-and-licensing | Restoring built-in logos resets only the seven original slots; the eight editor slots keep `default: false` and their extension. | static | OPEN |
| L2 | branding-and-licensing | Embedded spreadsheet/PDF logo slots read the extension of the normal slot; with different formats the URL names a file that does not exist. | static | OPEN |
| M6 | data-import | Unreachable RoomAdmin branch: the paid-user socket update never fires for imported Room admins; an imported `Guest` gets no type group. | static | OPEN |
| M9 | data-import | Source users whose e-mail belongs to a disabled portal account are dropped silently without a log or an `errors` entry. | static | OPEN |
| B7 | backup-and-restore | Backup counters include removed records, so deleting a backup does not return the free allowance. | test pins | OPEN |
| B9 | backup-and-restore | Any billing or quota failure deletes the backup schedule outright; a temporarily empty wallet silently ends automatic backups. | static | OPEN |
| B10 | backup-and-restore | `getBackupHistory` soft-deletes records and skips unreachable storages, so the list can shrink between two reads. | static | OPEN |
| N9 | notifications | The not-paid skip (`TariffState.NotPaid <= state`) is applied in `SendMsgWhatsNewAsync` to the daily and the hourly digest alike (a portal inside its grace period still gets them); only the at-once room letters of `NotifyClient.cs` have no such check, so a lapsed portal keeps sending them. | static | OPEN, narrowed to the at-once letters |
| A9 | ai-agents | Export into a form-filling room or an out-of-quota room answers 202 and the worker drops the document without a trace. | red by code reading | OPEN |
| Q7 | quotas | **Suspicion, not a verified defect.** `setTenantQuotaSettings` accepts any `tenantId`, so on a multi-portal standalone it may write the quota of another portal than the caller's. | seen in code, not tested | SUSPECTED |

### Crashes (500) and wrong status codes

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| H1 | history-and-audit | `GET security/audit/events/filter` with any non-default `moduleType` dereferences a null module mapper → 500. | red test, run 2026-11-10 | OPEN |
| SI11 | storage-and-integrations | `getThirdPartyCode` for Docusign, Wordpress and Github on an installation without their OAuth application does not build the consent URL: a null dereference (Wordpress, Github) or an unparsable address (Docusign) answers 500, where a provider outside the seven answers 200 without a URL. It may depend on configuration, but the failure is unhandled. | red test (`OA/ThirdPartyOAuthProvidersTests.cs:68`) | OPEN |
| T11 | portal-lifecycle | Deleting a portal on a server installation renames the alias at once, and a server installation resolves an unknown address to its default portal: every request a client still sends to the deleted portal's address is answered by the default portal (200), not refused with 404, so a stale client or link silently works against another portal. | test observation (`PortalImmediateDeletionTests.cs`: the old address answered 200 within 15 s) | OPEN |
| Q1 | quotas | A default room or AI-agent quota above the total storage throws a plain `Exception` → 500; the user-quota siblings answer 400 for the same condition. | red test, run 2026-11-10 | OPEN |
| A23 | ai-agents | `aiThreadsOpenOrCreate` with no `threadId` and no `firstMessage` answers 500 from the engine. | per test (comment, bug 82826) | OPEN |
| A32 | ai-agents | `aiProfilesListModels` on a saved profile whose provider accepts the connection and closes it without an answer is answered 200 with an empty model list instead of 502, so the form shows no models instead of the failure. | red test, run 2026-11-10 (`ProfileListModelsFailureTests.cs:67`, `Category=Bug`) | OPEN |
| F20 | file-operations | `changeVersionHistory` on a file open in somebody else's editing session answers 200 and changes nothing (bug 84772); the 500 on a file locked by another account is fixed in `bb406b409b..6fb013e745`. | red test, run 2026-11-10 | OPEN, narrowed |
| F12, F10, H2 | file-operations, history-and-audit | 403 instead of 404 for a missing folder in `createFile` and a missing file in `getFileVersionInfo`; 403 instead of 401 for an anonymous caller of `file/{id}/history`; the sibling endpoints answer 404 / correctly. | red test, run 2026-11-10 | OPEN |
| F6 | file-operations | `GET fileops/convert` and `fileops/import` → 400 instead of an empty list; `GET fileops/move` is shadowed by the check endpoint. | bug test (82225) | OPEN |
| Q3 | quotas | `setTenantQuotaSettings` reports a non-admin caller or a hosted portal with `NotSupportedException` → 415 instead of 403. | red test, run 2026-11-10 | OPEN |
| PS-6 | portal-settings | `closeAdminHelper` gives a signed-in non-administrator of a self-hosted portal 415 instead of 403, one branch for a role and for an edition; the yaml documents it. | test, run 2026-11-11 (`AdminHelperTests.cs`) | OPEN |
| T2 | portal-lifecycle | Confirming the owner change reports every business refusal ("User not found", "Access denied") as 500; the sibling `sendOwnerChangeInstructions` uses 400/403. | static | OPEN |
| T4 | portal-lifecycle | The "last full-access space" rule and the "wizard passed" check throw plain exceptions and answer 500; the yaml documents the 500 as intended. | static | OPEN |
| T7 | portal-lifecycle | System-API registration on a server installation validates `portalName` only inside the tenant registration: a taken or malformed name answers 500 instead of the 400 codes of `validateportalname`. | static | OPEN |
| M3 | data-import | `finishMigration` with `isSendWelcomeEmail: true` after a parse-only job: NullReferenceException, 500. | red test, run 2026-11-10 | OPEN |
| M4 | data-import | Cancel/clear read the operation of a not-yet-started task: NRE in the event handler, or an unmatched parse that keeps running while its upload folder is deleted. | bug test (84792; M13 is its concrete form) | OPEN |
| M11 | data-import | An unknown `migratorName` does not produce "No such migration provider": the job ends with the null-reference message as `error` and `operation` null; the same for `startMigration` with an empty body. | static (the test asserts only non-empty) | OPEN |
| M12 | data-import | A parse that failed before doing anything still answers 200 with its log; the test expects 404. | bug test (84791) | OPEN |
| M2 | data-import | `getMigrationStatus` swallows every exception and returns the empty result; an outage looks like "no job". | static | OPEN |
| W12 | billing | `getCustomerDeposits` swallows every accounting failure into an empty result, so "no top-ups" looks like "service down"; the sibling `getDepositInvoice` answers 402. | red test, run 2026-11-10 | OPEN |
| SI10 | storage-and-integrations | `getStorageEncryptionSettings` catches every exception, so missing rights, a hosted portal and real failures all look like 200 with an empty body; the yaml documents this. | red test, run 2026-11-10 | OPEN |
| N3 | notifications | `getNotificationSettings` with an undefined but bindable `type` (e.g. 7) answers 500 "Incorrect parameters" instead of 400; `setNotificationSettings` with it answers 200 and stores nothing. | static | OPEN |
| N10 | notifications | `linkTelegram` without a bot publishes a token and answers an empty link, then `checkTelegram` reports status 2 for 20 minutes although no link exists; the test expects 0. | red test, run 365d87fe60 | OPEN |
| L4, L5 | branding-and-licensing | A logo file whose name does not start with the slot number, or a `data:` URI in an unsupported format, answers 500 instead of 400. | static | OPEN |
| L9 | branding-and-licensing | An unparsable licence or one lacking customer id/signature answers 500 "License is not correct" instead of a client error. | static | OPEN |
| L10 | branding-and-licensing | Greeting title save answers 500 on a `localhost` installation with more than one portal (alias rewritten to `localhost`, unique index); the .NET tests are commented out. | static | OPEN |
| SI3 | storage-and-integrations | `getStorageProgress` on a server installation dereferences a null cache value when no migration is remembered: 500, and with SI1 that is every call. | red test, run 2026-11-10 | OPEN |
| SI7 | storage-and-integrations | `updateStorage` and `updateCdnStorage` read `props` unconditionally although it is not required: 500 instead of 400 for a configured module with no `props`. | red by code reading | OPEN |
| SI9 | storage-and-integrations | `startStorageEncryption` reports a running backup by a bare exception: 500 with no message for an expected condition. | red by code reading | OPEN |
| FX8 | files-settings | `setDefaultTemplate` with a numeric `selectedFile` that is not a 32-bit integer answers 500, and the yaml declares it. | red by code reading | OPEN |
| FX7 | files-settings | `createRoomTag` with a name holding a character outside the BMP is refused by the database write with 500, not validated as 400; the test comment says bug 81682 was closed as by-design. | per test comment | OPEN |
| PS-3 | portal-settings | `configureDeepLink` with a body without `deepLinkSettings` fails on a null reference: 500 for a request fault, documented in the yaml. | red by code reading | OPEN |
| PS-4 | portal-settings | `configureCsp` with a `null` entry in `domains` fails on a null reference: 500 for a request fault, documented in the yaml. | red by code reading | OPEN |
| PS-5 | portal-settings | `saveDnsSettings` answers 500 through plain exceptions for a malformed or taken name and a hidden section, not 400 or 402 as the sibling settings do; the yaml documents the 500. | test, run 2026-11-11 (`DnsSettingsTests.cs`) | OPEN |
| A24 | ai-agents | In a concurrent MCP add of one name the losers answer 403, not the soft "already registered". | per test (comment) | OPEN |
| P4 | plugins | Every failure after the size checks, storage I/O included, is wrapped as 400 "Wrong plugin archive. …". | static | OPEN |
| W9 | billing | `getPaymentUrl` quantity below 1 is validated after the configuration and admin checks: 403 without billing, 400 with it. | red test, run 2026-11-10 | OPEN |
| AC-8 | access-settings | The 403 of `updateTfaSettings` for naming the owner depends on the `id` field, not the caller's role, so an owner listing themselves in `mandatoryUsers` with no `id` gets 403. | test, run 2026-11-11 (`TfaOwnerGuardTests.cs:89`) | OPEN |
| AC-9 | access-settings | `unlinkTfaApp` never reaches the branch that treats an empty `id` as the caller: an empty `id` resolves to no account and answers 403 first. | by code reading | OPEN |
| W2 | billing | `getCustomerInfo`/`getRestrictedAiModels` answer 200-empty to Guests when billing/AI is not configured and 403 when it is; the rule depends on the installation, not the caller. | red test, run 2026-11-10 | OPEN |
| W8, W10 | billing | "No limits" vs "not a customer" are indistinguishable on the participants page; a failed balance read is cached 10 minutes as "no customer" (`c2f48e709e` only documents W10). | static | OPEN |
| B3 | backup-and-restore | `startBackupRestore` with the all-zero GUID queues a restore with no source that fails only in the worker (200). | static (observed by the test suite, which refuses to send it) | OPEN |
| B13 | backup-and-restore | The schedule refuses an unwritable storage with 400, but `startBackup` accepts the same body with 200 and lets the job fail in the worker, occupying the single queue slot. | static | OPEN |
| B5 | backup-and-restore | `backupFileUpload.ashx` answers 200 with `success:false` for every failure, permission refusals included. | static | OPEN |
| B2 | backup-and-restore | The documented 400 "dump on a non-standalone portal" is dead: the space-access demand already answers 403. | static | OPEN |
| M8 | data-import | Progress steps use integer division: with more than 30 users or 20 groups the progress does not move during that stage. | static | OPEN |
| L6 | branding-and-licensing | A colour theme beyond the limit is dropped and the call answers 200 with the unchanged list. | red test, run 2026-11-10 | OPEN |
| R6 | shared-links | `getFolderPrimaryExternalLink` on a folder inside a Form filling room throws, while `PUT` on the same folder creates a primary link (D12): GET and PUT disagree. | test pins (no status asserted) | OPEN |
| R8 | shared-links | The link-limit refusal is produced as an operation warning with the entry skipped, not as an error; invalid `linkType`/`access` answer 403 instead of 400. | bug test (82370, 82371) | OPEN |
| OA3 | oauth2-applications | `getConsents` answers a Guest with their own signature 200 and an empty page, as the yaml documents; the earlier 500 came from the SDK failing on the null cursor. Bug 81729 asks for 403, so the test stays red: which side is wrong is a product decision. | red test, run 2026-11-10 | OPEN (product decision) |
| R3 | user-type-permissions | The invitation-link endpoint refuses the Guest type with 400 "EmployeeType" while the people endpoints answer 403 for the same refusal. | static | OPEN |
| R9 | shared-links | Revoking a Custom room's primary link without the `primary` field leaves `getRoomsPrimaryExternalLink` at 404 for good; a revocation with `primary:true` lets it be minted again. | red test, run 365d87fe60 (bug 84873) | OPEN |
| FX4 | files-settings | `get*PrimaryExternalLink` checks the revocation before the sharing right, so a caller with no access to a revoked entry gets 404, while an unrevoked entry gives 403. Refuted for `create*PrimaryExternalLink`, which no longer runs the revocation check. | red by code reading | OPEN, narrowed |
| AP2 | apps | An application id longer than 64 characters in the configuration reads fine but every write fails at the database → 500; no start-up validation. | static | OPEN |
| MD3 | metadata | Raw exception text is returned in the cascade `error` field to any reader. | static | OPEN |
| MD1 | metadata | Retyping single ↔ multi choice is refused like any retype, and retyping a choice field requires `options: []`. | static | OPEN |

### Inconsistencies, messages and documentation

| # | Spec | What the server does | Evidence | Status |
|---|------|----------------------|----------|--------|
| F11 | file-operations | `createNewIfExist` is inverted between text/HTML creation (`true` = return the existing file) and every upload call (`true` = keep both). | test pins | OPEN |
| P8 | plugins | The published `WebPluginDto` lists `image`, `cssUrl`, `settings` and the other optional manifest fields as required while the server leaves a null field out of the answer, so the generated C# client throws "Required property 'image' not found in JSON" on `getWebPlugin` / `getWebPlugins` for nearly every plugin. | red test (`WebPluginReadTests.cs:181-196`) | OPEN |
| S13 | webhooks-and-api-keys | `ApiKeyManager.UpdateApiKeyAsync` stores `["*"]` as no list (`null`), where the creation stores an empty one; the listing then leaves the required `permissions` field out and the generated C# client fails to read the whole `GET keys` answer. The key itself is unrestricted as intended. | red test, run 2026-11-10 (`UpdateApiKeyFieldTests.cs:89`, FAIL-BUG) | OPEN |
| M14 | data-import | The OpenAPI document does not describe the `text/plain` body of `getMigrationLogs`, so the generated .NET SDK (4.1.0) parses the log as JSON and throws `ApiException` on every successful download. A contract/SDK defect; the server answers correctly. | test reads it over raw HTTP (`MigrationParseTests.cs:222`) | OPEN |
| P9 | plugins | At the plugin count limit the upload looks the new name up with `GetWebPluginByNameAsync`, which throws instead of returning null, so the caller gets 400 "Wrong plugin archive. Plugin not found"; the `ErrorWebPluginMaxCount` text is never sent. The refusal itself and the re-upload exemption work. | red test (`WebPluginReuploadTests.cs:107-119`) | OPEN |
| FW4 | form-filling-workflow | A room owner may Duplicate a draft in a per-form "In process" folder: `FileSecurity` refuses Duplicate for every file of such a folder, yet `GET file/{id}` answers `security.Duplicate` true for the portal owner (the draft's creator gets false). | bug test (`FormResultFoldersTests.cs:238`, `Category=Bug`); its verification calls it F4 | OPEN |
| FW5 | form-filling-workflow | A PDF copied into a Form filling room is not started: the move branch stores `startFilling` true, the copy branch writes no filling state, so the copy stays hidden from the form fillers while a moved, created or uploaded PDF is started. | bug test (`FormMoveCopyTests.cs:115`, `Category=Bug`); its verification calls it F5 | OPEN |
| H7 | history-and-audit | `PUT security/activeconnections/logoutallchangepassword` calls `securityContext.Logout()` before it sends the password-change instructions, so the audit event is stored with the anonymous account as the one who acted, not the user who asked. | red test (`ConnectionLogoutAuditTests.cs:108`, `Category=Bug`) | OPEN |
| A34 | ai-agents | The description of `getRoomAiFolder` says the rooms with skills are listed with `GET api/2.0/files/rooms?withAiFolder=true`, but without `searchArea=AiHub` the flag filters the Rooms section and the answer is empty; with `searchArea=AiHub` it lists the Instructions rooms only. | red test, run 2026-11-10 (`AiRoomOutsiderAccessTests.cs:129`, `Category=Bug`) | OPEN |
| PS-1 | portal-settings | `getSocketSettings` reads the key `web:hub`, a section with no value of its own in the default configuration, so the answer's `url` is empty while `getPortalSettings` reads `web:hub:url`; deployments that set a flat `web:hub` were not checked. | red by code reading | OPEN |
| PS-2 | portal-settings | `saveMailDomainSettings` stores the request's `inviteUsersAsVisitors` in a setting named `InviteAsUsers`, an opposite meaning, and the CodeGraph index shows no reader of it (LSP not run). | red by code reading | OPEN |
| AC-10 | access-settings | `updateTfaAppCodes` does not check that the method is on at portal level while `getTfaAppCodes` does, so with the method off and an application still linked the two disagree. | by code reading | OPEN |
| PG-1 | people-and-groups | `displayName` of a profile comes back HTML-encoded (`O&#39;Hara`) while `firstName`, `lastName` and the stored columns are not, so feeding it back to a search finds nothing. | per test comment | OPEN |
| SI5 | storage-and-integrations | The Yandex entry of `getAllProviders` has key `WebDav` without `requiredConnectionUrl`, while a WebDAV connection without an address is refused (the refusal read only as a comment). | red by code reading | OPEN |
| SI8 | storage-and-integrations | A second `saveThirdPartyBackup` stores the new title without the invalid-character cleaning a new account gets. | red by code reading | OPEN |
| PR2 | private-rooms | A private room or its subfolder can be added to Favorites. | per commit message | OPEN |
| FW2 | form-filling-workflow | By-id read of a hidden form: this spec, the code, the test and `form-filling-room-roles` say 403, and only the old live-run text of D6 says 404; see the recorded contradiction after the D table. | test pins (documentation item) | OPEN |
| Q5 | quotas | A file that does not fit the remaining portal allowance is refused with "The maximum file size is exceeded (<remaining>)", quoting free space as a per-file limit. | static | OPEN |
| FX2 | files-settings | A second person refused by `startEditFile` is told "This document is being edited by you in another tab". | pinned by a test; wording judged by reading | OPEN |
| Q4 | quotas | `FolderQuotaChangeAsync` refuses a quota above `maxTotalSize` or the Standalone portal quota with `InvalidOperationException` (403) and the "greater than the total storage" message, the status of an access refusal; the user-quota sibling answers 400 (`ArgumentException`). The earlier second, unreachable "folder not found" refusal is gone. | red test, run 2026-11-10 | STILL OPEN |
| Q6 | quotas | The `maxTotalSize = 0` branch of `getPortalQuota` for a User is dead code behind the permission demand, yet documented in the yaml. | static | OPEN |
| S3 | webhooks-and-api-keys | `ApiKeyManager.DeleteApiKeyAsync` returns `result >= 0`, i.e. always true. | static | OPEN |
| S4 | webhooks-and-api-keys | `PUT settings/webhook/enable` demands `name` and `uri` in a body it ignores. | static | OPEN |
| S7 | webhooks-and-api-keys | OpenAPI: the `WebhookTrigger` description still stops at `form.stopped` while the enum lists all 38 members; the signature-header part is fixed. | static | OPEN, partly fixed |
| S9 | webhooks-and-api-keys | Unlocking a file of the portal storage raises `file.updated` twice, and the lock call raises it even when nothing changes. | static (documented in the contract) | OPEN |
| S10 | webhooks-and-api-keys | `room.updated`, `agent.updated` and `user.updated` fire on every successful call even without a change, while `group.updated` was narrowed to real changes. | static (test covers the group side) | OPEN |
| S12 | webhooks-and-api-keys | `WebhooksLogDto.route` is the subscription's current URL, so the journal cannot show where a past delivery went. Design smell, documented. | static | OPEN |
| H4 | history-and-audit | A missing body on `POST security/audit/settings/lifetime` is reported as "LoginHistoryLifeTime" 400. | red test, run 2026-11-10 | OPEN |
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
| M13 | data-import | `clearMigration` removes only completed jobs whose `operation` is "migration", so a failed parse stays queued until a new parse replaces it, and `cancelMigration` skips an unknown-migrator job too. | red test, run 365d87fe60 | OPEN |
| N4 | notifications | A file updated or renamed by somebody else is not reported as new. | bug test (81713; TS `test.fail`) | OPEN |
| N5 | notifications | `setRoomsNotificationStatus` stores any id forever; no cleanup. | static | OPEN |
| N6 | notifications | SMTP save accepts and drops `useNtlm`; validation is only DTO attributes (TODO in the controller). | static | OPEN |
| N7 | notifications | Telegram texts declared for portal_deactivate, portal_delete, user_has_join, self_profile_updated, backup_failed are never sent; the Files `subject_*_tg` keys are unreferenced. | static | OPEN |
| L7 | branding-and-licensing | `feedbackAndSupportUrl` of the payment settings is always null. | static | OPEN |
| L11 | branding-and-licensing | The licensor flag of the company details is always stored as `false`. | static | OPEN |
| PR6 | private-rooms | The neutral room title exists in the default culture only, so a letter in another language names the room in that default language. | per commit message | OPEN |
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
| FX9 | files-settings | `filterValue` with an uppercase letter matches nothing in `getGroupsMembersWithFolderSecurity` and `getGroupsMembersWithFileSecurity`, per the yaml text. | yaml text only | OPEN |
| R5 | room-role-permissions | `updateFileComment` (version comment) is refused for an Editor; design question, kept outside the Comment row. | static | OPEN |
| FS1 | file-sharing | **Behaviour, not a defect**, recorded so nobody files it as one: `addFavorites` / `deleteFavoritesFromBody` skip a file the caller cannot read and answer `true`, the same as for a readable one; the spec states the skip. | test pins | NOT A DEFECT |
| — | user-type-permissions | Matrix row "copy of an archived room" is allowed by the matrix but `room-types` refuses duplication of an archived room; kept MATRIX-ONLY and flagged, neither spec changed. | static | not re-checked |
| AC-11 | access-settings | `getIpRestrictions` and `getSupportedCultures` compute the ETag by hashing a `MemoryStream` that is never rewound, so every list gets the same ETag (MD5 of zero bytes): after the IP list changes, a request with the old ETag still gets 304 and the client keeps the stale list. | red test, run 2026-11-11 (`IpRestrictionsRulesTests.GetIpRestrictions_ListChangedAfterTheETag_ReturnsList`, `Category=Bug`); code read | OPEN |
| PS-7 | portal-settings | A refused `saveMailDomainSettings` (Custom list with a bad domain) and a refused `saveDnsSettings` (name mapped by another portal) change the cached portal object before the check: the database keeps the old value, but the portal then reports the refused value until the cache expires. | red test, run 2026-11-11 (`MailDomainSettingsTests.cs:70`, `DnsSettingsTests.cs:160`, `Category=Bug`) | OPEN |
| FX10 | files-settings | `getGroupsMembersWithFolderSecurity` / `...FileSecurity` for a group with no share on the entry return an empty list, but `total` and `count` give the group's size: the count query skips the share check the list query has. | red test, run 2026-11-11 (`GroupMembersFolderSecurityTests.cs:120`, `Category=Bug`) | OPEN |

## Closed by v4.1.0

Rows whose `verification.md` verdict is FIXED or REFUTED, including those that were found and fixed within the
range. The fixes were read, not run. "Submodule" is a commit of `sdk/docspace-api-spec`, the regenerated contract.

| # | Spec | What the server did | Evidence | Status |
|---|------|---------------------|----------|--------|
| AC-1 | access-settings | `saveSsoSettingsV2` with no IdP entity ID, no SSO URL, a malformed SLO URL or an incomplete field mapping answered 500; it now answers 400 (bug 84685). | commit read, not run | FIXED `f3a52c35dd` |
| AC-2 | access-settings | `saveSsoSettingsV2` with a missing or invalid SSO URL answered 500; it now answers 400 (bug 84686). | commit read, not run | FIXED `f3a52c35dd` |
| AC-3 | access-settings | `saveSsoSettingsV2` with an incomplete field mapping answered 500; it now answers 400 (bug 84687). | commit read, not run | FIXED `f3a52c35dd` |
| AC-4 | access-settings | `saveSsoSettingsV2` with a malformed SLO URL answered 500; it now answers 400 (bug 84688). | commit read, not run | FIXED `f3a52c35dd` |
| AC-5 | access-settings | `sendSmsCode` with valid credentials and SMS off for the account answered 500, the thrown exception type having no mapping (bug 84503). | commit read, not run | FIXED `f5f2735e30` |
| F22 | file-operations | `updateFile` on a file locked by somebody else was refused through a 500 instead of a client error. | commit read, not run | FIXED, no commit named |
| R10 | shared-links | After a file's primary link was revoked, `createFilePrimaryExternalLink` answered 404 from then on, while `setFileExternalLink` could still make one (bug 84872). | commit read, not run | FIXED `62419af921` |
| FX3 | files-settings | Same bug as R10, on `createFilePrimaryExternalLink` and the folder one: create shared the read path, so after a revocation the POST answered 404 (bug 84872). | commit read, not run | FIXED `62419af921` |
| FW3 | form-filling-workflow | `startFillingFile` outside a Form filling room returned the file's metadata to a caller who could not read it. New row; its `verification.md` calls it F3. | commit read, not run | FIXED `33f1d746c1` |
| L13 | branding-and-licensing | The Files worker's periodic re-read put an uploaded, not yet accepted licence in force, bypassing `acceptLicense`. New row. | commit read, not run | FIXED `2c268c3041` |
| W1 | billing | A request header `Payment-Info` with any value other than `true` skipped the 402 gate of a not-paid portal. | static | FIXED `dbb446ada2` |
| S5 | webhooks-and-api-keys | `PUT settings/webhook` validated the target, incl. an outbound HEAD request, before checking ownership. | two tests cover it, plus a regression guard (`WebhookTargetProbeTests.cs`, 2026-11-10) | FIXED `9e317df40d` |
| A12 | ai-agents | A server-executed DocSpace tool answered with another tenant's folder: a client-supplied `Mcp-Session-Id` reached the shared container (BUG 83172). | commit message | FIXED `916074e486` |
| A2 | ai-agents | `aiPreferencesGetToolPermissionMode` was served from an ungated snapshot, so a Guest read it past the AI switch. | tests | FIXED `4b380e5ee7`, `2a4975b142` |
| A7 | ai-agents | Web-search configure / set-active / test-connection ran validation and the provider probe before the Guest and AI-switch checks. | tests | FIXED `0dc046cacd`, `857671bb6d` |
| AP1 | apps | The `app_settings` table was in no backup module, so a backup dropped every application flag and settings document. Backups taken before the fix still hold no rows; the full cycle has no test. | test prepares the row | FIXED `9e696831d5` (bug 84721) |
| F8 | file-operations | `markAsRead` on the My documents root cleared the "new" marks of an unrelated room. | tests (bug 83509) | FIXED `73aa4e8ab8` |
| F7 | file-operations | A room member with Editing rights got 403 from `checkConversionStatus`. | tests (bug 81825) | FIXED `73aa4e8ab8` |
| F5 | file-operations | Aborting an upload session that never existed answered 500 instead of 404. | tests (bug 82278) | FIXED `73aa4e8ab8` |
| R1 | user-type-permissions | A private room was listed to a non-member Owner and DocSpace admin. | code and test read, not run (bug 82956); the "expected to fail" remark of the old test is stale | FIXED `73aa4e8ab8` |
| F19 | file-operations | Creating a `.docx`, a text file or an HTML file directly in a Form filling room was refused through a plain `Exception` → 500; it now answers 403 and stores nothing, on the upload and session routes as well. | code and tests read, not run (bug 84840) | FIXED `1a1d539775` |
| FW1 | form-filling-workflow | `POST files/{folderId}/file`, `/text` and `/html` with a non-PDF in a Form filling room were not refused with a client error; they now answer 403 and store nothing. | code and test read, not run (bug 84840) | FIXED `1a1d539775` |
| D8 | form-filling-room-roles | A docx uploaded or created in a Form filling room answered 500; it now answers 403, the same as `fileops/copy` of that docx. | code and tests read, not run; same root as F19 | FIXED `1a1d539775` |
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
| OA1 | oauth2-applications | `updateClient` accepts a `name` outside 3 to 256 characters (200), while `createClient` refuses it and the yaml declares the same bound on the update (bug 81670). | per test comment | FIXED (tests green 2026-11-10, no commit named) |
| OA2 | oauth2-applications | `getPublicClientInfo` with an unknown id does not answer the 404 the yaml declares (bug 81728). | per test comment | FIXED (tests green 2026-11-10, no commit named) |
