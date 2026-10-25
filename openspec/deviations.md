# Known deviations between the specifications and the server

The specifications in `specs/` describe the server as it behaves today (checked against the integration
tests, a static reading of the code and a live run against a local portal, October 2026). This file keeps
what was deliberately left out of them: behaviour that looks like a defect rather than a design decision,
and places where the product matrix ("Types and Roles. Access rights", "Shared link rules") describes
something the server does not do. Each item names the specification it would belong to.

## Suspected defects (not written into the specs)

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| D1 | vdr-room-roles, form-filling-room-roles | `PUT /api/2.0/files/file/{id}/manageformfilling` checks permissions only for the Start and Stop actions. Resume (1) on a stopped form answers 200 and resumes filling; Edit (3) on a started form in a Form filling room answers 200 and stops collecting. Both work for a Viewer, a Form Filler without a role, and for a User or Guest who is not a member of the room. | live run; `FileStorageService.cs` manage-form-filling actions |
| D2 | shared-links | In a Public room a non-member (User or Guest) gets 200 on `PUT /api/2.0/files/folder/{id}/links` with `primary:true, access:2`, and a primary read link is created on a folder they cannot read. The token is not returned to them and they cannot read it back, so no content leaks; it is an unauthorized write. The same request on a Custom room is refused. | live run; `FileSharing.SetAceObjectAsync` skips the check for a primary read link in a Public room |
| D3 | user-type-permissions | A Room admin promotes a guest invited by someone else to User through `POST /api/2.0/people/type` (200, job completes, type changes), while `PUT /api/2.0/people/type/User` refuses the same guest (403). | live run |
| D4 | user-type-permissions | A Room admin cannot add a foreign guest to their room by id (200, nobody added) but can by email: the guest joins the room and the Room admin's guest list, after which file sharing with that guest works. | live run |
| D5 | vdr-room-roles | A Form Filler who holds no role on a PDF form, and a Form Filler in a room where the form is not configured, cannot see the form in the listing but reads it by id (`GET files/file/{id}` 200, `presigneduri` 200, `openedit` 200). | live run; `FileSecurity` PDF branch |
| D6 | form-filling-room-roles | A Form Filler copies a not-started form, hidden from them in the listing and 404 by id, to My documents through `fileops/copy` (200, copied). | live run |
| D7 | user-type-permissions | `POST /api/2.0/files/@my/file` as a Guest answers 500 (`NullReferenceException` in `FileStorageService.CreateNewFileAsync`); `@my/text` and `@my/html` answer 404. | live run |
| D8 | form-filling-room-roles | A docx uploaded or created in a Form filling room answers 500 with the "cannot be uploaded to this room" message; `fileops/copy` of the same docx answers 403. | live run |
| D9 | user-type-permissions | `GET /api/2.0/portal/users/invite/{type}` never refuses: a disallowed pair answers 200 with an empty string, and for the Guest type it returns a real link to the Owner, a DocSpace admin and a Room admin, although `POST /portal/users/invitationlink` and the people endpoints refuse Guest for everyone. | live run |
| D10 | user-type-permissions | `PUT /api/2.0/people/delete` by a DocSpace admin on a disabled DocSpace admin answers 200 with the account echoed and deletes nothing; `DELETE /people/{id}` answers 403. | live run |
| D11 | shared-links | Files and folders in My documents, Custom, Collaboration and VDR rooms accept up to 6 *primary* links in addition to 6 additional ones (`primary:true` with a new link id succeeds up to 6 times). Rooms and Public-room entries keep one primary link. | live run; `FileSecurity`/`FileSharing` link limits |
| D12 | shared-links | A primary link of a folder inside a Form filling room stores an expiration date on create; later updates and revocation are skipped unless the stored date is echoed. | live run |
| D13 | form-filling-room-roles | The "only ONLYOFFICE PDF forms" rule is enforced by extension only (`.pdf`), so an ordinary PDF enters the room and is auto-started on move; the form-signature check exists only in the advisory `checkdestfolder`. | live run; `FileDao`, `CopyPermissionsCheck` |

## Matrix says one thing, server does another (specs follow the server)

| Where | Matrix | Server |
|-------|--------|--------|
| user-type-permissions | Users and Guests may invite to a room (cells marked "ships in the next version"); roles handed out are limited to their own | Inviting needs the Room manager role, which a User or Guest cannot hold; 403 |
| user-type-permissions | Nobody sees the links of someone else's public room | A non-member DocSpace admin reads the primary link |
| user-type-permissions | Owner and DocSpace admin see all rooms | Not private (end-to-end encrypted) rooms |
| user-type-permissions | A Room admin requests promotion of a User to Room admin; Owner or DocSpace admin confirms in Accounts | No such operation in the API |
| user-type-permissions | A Room admin shares files with own guests and guests of own rooms | Only guests the Room admin invited |
| user-type-permissions | Forms in rooms other than FFR/VDR are shared "for editing only" | Editing, comment and read links are offered; filling is refused |
| user-type-permissions | DocSpace admin manages every setting except portal deletion | Suspension and granting the DocSpace admin type are owner-only too |
| room-role-permissions | Every role invites users and assigns roles; Owner and Room manager "moderate" invitations | Only Owner and Room manager invite; no moderation exists |
| room-role-permissions | Nobody changes own role | A Room manager can lower own role; any member can leave |
| room-role-permissions | A Content creator locks only own files | Locks any unlocked file; cannot unlock someone else's lock |
| room-role-permissions, vdr-room-roles, form-filling-room-roles | Rename: Owner and Room manager only | A Content creator renames own entries |
| room-role-permissions, vdr-room-roles, form-filling-room-roles | Copy of other members' files: Owner and Room manager only; copy out of the room: Owner, Room manager, Content creator | Copy does not depend on authorship; every role copies out (a Viewer unless download is denied) |
| room-role-permissions | Copy out of an archived room: Owner, Room manager, Content creator | Every role |
| room-role-permissions | Form Filler is a role of a general room | Only VDR and Form filling rooms accept it; a Public room accepts only Content creator and Room manager |
| vdr-room-roles | A Form Filler sees a configured form only when their turn has come | Visible as soon as assigned; the turn governs filling |
| vdr-room-roles | A Content creator stops filling of own forms and forms they organized | Only forms they created |
| vdr-room-roles | A Form Filler views only forms they take part in | Non-form files are refused; see D5 for PDFs |
| form-filling-room-roles | Only ONLYOFFICE PDF forms enter the room | Any PDF (see D13) |
| form-filling-room-roles | A Form Filler does not copy or move forms; does not delete "Complete" forms | Copies forms out; deletes own results in both folders (delete: code only) |
| form-filling-room-roles | Results sync by a Content creator "only own / organized" (resolved comment) | Any Content creator, any form |
| shared-links | Maximum 6 links per file or folder | 6 additional plus primary links (see D11) |
| shared-links | Form filling room link: filling only | The room link also accepts the read right; entries accept filling only |
| shared-links | Form filling room and Public room links are perpetual | Only the room's own primary link; entries and additional links keep an expiration date |
| shared-links | "Rejects" an audience or lifetime value that is not offered | Answers 200 and silently normalises or skips (see the spec) |
| shared-links | VDR: deny-download cannot be turned off on a link while the room setting is on | The flag is stored; the room setting still blocks the download |

## Suspected defects found while writing the later specs

Static findings (code and tests, not a live run) collected from the `verification.md` of each spec; the
numbers are the ones used there. Ordered by how much they matter.

### Access and integrity

| # | Spec | What the server does | Evidence |
|---|------|----------------------|----------|
| H3 | history-and-audit | `PUT security/activeconnections/logout/{loginEventId}` checks ownership only when the login event has a `UserId`; an event stored without one can be closed by any signed-in user. | `ConnectionsController.LogOutActiveConnection` |
| S2 | webhooks-and-api-keys | `PUT keys/{id}` on an expired key answers `200 false` before the ownership check: a non-owner learns that a foreign key exists and is expired instead of 403. | `ApiKeysController.UpdateApiKey` |
| S5 | webhooks-and-api-keys | `PUT settings/webhook` validates the target (incl. an outbound HEAD request) before checking ownership: any member can make the server probe a public URL by naming someone else's webhook id (403 afterwards). | `WebhooksController.UpdateWebhook` |
| S1 | webhooks-and-api-keys | Updating an API key with `permissions: ["*", "files:read"]` is stored as-is and the auth handler then grants unrestricted `*:write`; creation normalises the same input to "unrestricted". | `ApiKeyManager.UpdateApiKeyAsync`, `ApiKeyBearerAuthHandler` |
| F1 | file-operations | `POST files/thumbnails` is `[AllowAnonymous]`; an anonymous caller without a link is accepted although the API document says 401. | bug 81268, test red |
| F9 | file-operations | `markAsRead` has no per-item access check: a non-member marks a room file as read with 200. | `MarkAsReadPermissionsTests` asserts it |
| F8 | file-operations | `markAsRead` on the My documents root clears the "new" marks of an unrelated room (one shared tag per user). | bug 83509 |
| Q2 | quotas | Moving a file between two accounts' My documents silently raises the receiver's custom user quota by the file size. | `FileDao.cs` move path |
| S6 | webhooks-and-api-keys | The "limited Developer Tools access for Users" setting also blocks Room admins; notes and endpoint text say Users only. Intent unclear; the bug-81236 test asserts the block. | `WebhooksController`, `ApiKeysController` |

### Crashes (500) and wrong status codes

| # | Spec | What the server does | Evidence |
|---|------|----------------------|----------|
| H1 | history-and-audit | `GET security/audit/events/filter` with any non-default `moduleType` dereferences a null module mapper → 500. No test covers `moduleType`. | `AuditEventsRepository.GetByFilterWithActionsAsync` |
| Q1 | quotas | A default room or AI-agent quota above the total storage throws a plain `Exception` → 500; the user-quota siblings answer 400 for the same condition. | `SettingsController` |
| F3 | file-operations | `copyBatchItems` with a folder id that does not exist → 500 (a missing file id → 404). | `FolderCopyTests` asserts the 500 |
| F5 | file-operations | Aborting an upload session that never existed → 500 instead of 404. | bug 82278 |
| F2 | file-operations | `POST files/thumbnails` without `fileIds` → 400 with a stack trace instead of 200 and an empty list. | test red, unfiled |
| Q3 | quotas | `setTenantQuotaSettings` reports a non-admin caller or a hosted portal with `NotSupportedException` → 415 instead of 403. | `SettingsController` |
| F4 | file-operations | `moveBatchItems` with a file id that does not exist → 403 instead of 404. | bug 82243 |
| F12, F10, H2 | file-operations, history-and-audit | 403 instead of 404 for a missing folder in `createFile` and a missing file in `getFileVersionInfo`; 403 instead of 401 for an anonymous caller of `file/{id}/history`; the sibling endpoints answer 404 / correctly. | tests assert the current codes |
| F6 | file-operations | `GET fileops/convert` and `fileops/import` → 400 instead of an empty list; `GET fileops/move` is shadowed by the check endpoint. | bug 82225 |
| F7 | file-operations | A room member with Editing rights gets 403 from `checkConversionStatus`. | bug 81825 |

### Inconsistencies, messages and documentation

| # | Spec | What the server does | Evidence |
|---|------|----------------------|----------|
| F11 | file-operations | `createNewIfExist` is inverted between text/HTML creation (`true` = return the existing file) and every upload call (`true` = keep both). | tests assert both |
| Q5 | quotas | A file that does not fit the remaining portal allowance is refused with "The maximum file size is exceeded (<remaining>)", quoting free space as a per-file limit. | `FileDao`, `SetupInfo` |
| Q4 | quotas | `FolderQuotaChangeAsync` has an unreachable second over-limit check with a "folder not found" message; both cases are 403. | `FileStorageService` |
| Q6 | quotas | The `maxTotalSize = 0` branch of `getPortalQuota` for a User is dead code behind the permission demand, yet documented in the yaml. | `PortalController` |
| S3 | webhooks-and-api-keys | `ApiKeyManager.DeleteApiKeyAsync` returns `result >= 0`, i.e. always true. | `ApiKeyManager` |
| S4 | webhooks-and-api-keys | `PUT settings/webhook/enable` demands `name` and `uri` in a body it ignores. | `EnableWebhook` DTO reuse |
| H4 | history-and-audit | A missing body on `POST security/audit/settings/lifetime` is reported as "LoginHistoryLifeTime" 400. | `SecurityController.SetAuditSettings` |
| H5 | history-and-audit | Audit and login filters are silently dropped on a plan without the audit option: 20 most recent events, 200. Design smell, documented. | `SecurityController` |
| S7 | webhooks-and-api-keys | OpenAPI: `WebhookTrigger` description stops at `form.stopped`; `WebhooksLogDto` example names the header `x-docspace-signature` instead of `x-docspace-signature-256`. | yaml, `WebhooksLogDto.cs` |
| F13 | file-operations | `checkMoveOrCopyBatchItems`, `checkMoveOrCopyDestFolder` and `deleteFileVersions` cannot be called through the generated SDK clients (query binding / array response). | tests use raw HTTP |

## Suspected defects found in the fourth batch (notifications … billing, 2026-10-24)

Static findings from the `verification.md` of `notifications` (N), `plugins` (P), `data-import` (M),
`branding-and-licensing` (L), `portal-lifecycle` (T), `ai-agents` (A), `billing` (W) and the revised
`backup-and-restore` (B). The numbers are the ones used there; the evidence columns of those files hold the
file:line references. Re-checked against the hotfix/v4.0.1 merges (`183da52625`, `c6a76a4594`): B1 is fixed,
everything else is still open.

### Access and integrity

| # | Spec | What the server does |
|---|------|----------------------|
| W1 | billing | A request header `Payment-Info` with any value other than `true` (e.g. `false`) skips the 402 gate of a not-paid portal, so any client keeps using the whole API by adding one header. |
| L8 | branding-and-licensing | `acceptLicense` and `refreshLicense` demand no right: any signed-in member of any portal of the installation, a Guest included, can move a staged licence into place, rewrite the installation's quota and tariff and write the audit event under their name. |
| T8 | portal-lifecycle | `deleteportalimmediately` lets any owner whose address matches the configured test-address pattern delete the portal with an ordinary session and no confirmation; a loose pattern turns every owner of a domain into a one-call deleter. |
| T6 | portal-lifecycle | The rename answers with a confirmation sign-in link of type `Auth` for the caller in the body of a 200, i.e. a credential-bearing URL that proxies log. |
| A1 | ai-agents | Vectorization checks only the file's Vectorization right: a manager of agent A queues a Knowledge file of agent B they cannot read; a Viewer queues files of their agent. |
| A7 | ai-agents | Web-search configure / set-active / test-connection run body validation and the provider probe before the Guest and AI-switch checks: a Guest validates a key through the portal. |
| A2 | ai-agents | `aiPreferencesGetToolPermissionMode` is ungated: a Guest reads it and the read ignores the AI switch; the sibling `aiSettingsGetToolMode` refuses both. |
| A12 | ai-agents | A server-executed DocSpace tool answered with another tenant's folder on the test stand (BUG 83172). |
| A11 | ai-agents | Deleting another user's prompt answers success although nothing is deleted. |
| P5 | plugins | `getWebPlugins`/`getWebPlugin`, callable by any member incl. Guests, rewrite the portal plugin settings and emit a socket event when a stored value fails to decrypt: a GET mutates portal settings. |
| P1 | plugins | A manifest with `"enabled": true` is reported as on for every portal without a stored row, and switching it off deletes the row so the manifest value returns: the plugin cannot be switched off; manifest `createBy`/`createOn` are trusted. |
| M1 | data-import | `migrationFileUpload.ashx` answers 200 for every outcome incl. "Access denied." and an upload without `Init`; no size limit; outside OpenAPI. |
| M10 | data-import | `migratorName` of the body is trusted over the parsed job's migrator, so a wrong name re-parses the upload with the wrong migrator and fails late. |
| N1 | notifications | The room-deleted letter checks the subscription and mute list of the deleter instead of each recipient. |
| N2, N8 | notifications | Form submitted/filling letters and the submitted push bypass the rooms-activity subscription and the room mute that every other room letter applies. |
| B4 | backup-and-restore | Restore from a Documents file demands the create right on the folder although reading is all it needs. |
| B11 | backup-and-restore | `backup/backuptmp` returns the worker's absolute temporary folder to any DocSpace admin. |
| B12 | backup-and-restore | The portal-wide 403 while restoring is a path-suffix allowlist (`portal`, `settings`, …); any route ending in one of those words passes. |
| T3 | portal-lifecycle | Owner change promotes the new owner before `ownerId` is saved; a failure leaves a promoted member and the old owner. |
| T5 | portal-lifecycle | Rename saves the alias, writes the audit and logs everyone out before the hosting cache is updated; a failure leaves a hosted portal reachable under neither name. |
| A3 | ai-agents | Agent create is non-atomic: the room is created before the Chat binding and stays behind without a model when the binding fails. |
| P2 | plugins | Plugin state is saved and the socket event sent before the CSP save; on the header-size overflow the caller gets 403 while the plugin is installed and enabled. |
| W6 | billing | Service-limit writes commit to accounting before the audit record and socket event; a late failure answers 500 with the limit already changed. |
| W4 | billing | `moveSubscriptionToWallet` compares a price in the request region's currency with a USD wallet balance. |
| W5 | billing | `setTenantWalletSettings.currency` is stored unchecked; auto top-up silently never fires when it matches no sub-account. |
| L1 | branding-and-licensing | Saving logos from image data returns instead of continuing on an empty entry, silently skipping every later slot while answering `true`. |
| L3 | branding-and-licensing | Restoring built-in logos resets only the seven original slots; the eight editor slots keep `default: false` and their extension. |
| L2 | branding-and-licensing | Embedded spreadsheet/PDF logo slots read the extension of the normal slot; with different formats the URL names a file that does not exist. |
| M6 | data-import | Unreachable RoomAdmin branch: the paid-user socket update never fires for imported Room admins; an imported `Guest` gets no type group. |
| M9 | data-import | Source users whose e-mail belongs to a disabled portal account are dropped silently without a log or an `errors` entry. |
| B7 | backup-and-restore | Backup counters include removed records, so deleting a backup does not return the free allowance (a test pins this). |
| B9 | backup-and-restore | Any billing or quota failure deletes the backup schedule outright; a temporarily empty wallet silently ends automatic backups. |
| B10 | backup-and-restore | `getBackupHistory` soft-deletes records and skips unreachable storages, so the list can shrink between two reads. |
| N9 | notifications | The not-paid skip exists only for the daily digest; the hourly digest and at-once room letters keep going on a lapsed portal. |
| A4 | ai-agents | The capability check exempts the Default slot, so an image-only profile can be Default and Chat resolves to a model that cannot chat. |
| A9 | ai-agents | Export into a form-filling room or an out-of-quota room answers 202 and the worker drops the document without a trace. |

### Crashes (500) and wrong status codes

| # | Spec | What the server does |
|---|------|----------------------|
| T2 | portal-lifecycle | Confirming the owner change reports every business refusal ("User not found", "Access denied") as 500; the sibling `sendOwnerChangeInstructions` uses 400/403. |
| T4 | portal-lifecycle | The "last full-access space" rule and the "wizard passed" check throw plain exceptions and answer 500; the yaml documents the 500 as intended. |
| T7 | portal-lifecycle | System-API registration on a server installation validates `portalName` only inside the tenant registration: a taken or malformed name answers 500 instead of the 400 codes of `validateportalname`. |
| M3 | data-import | `finishMigration` with `isSendWelcomeEmail: true` after a parse-only job: NullReferenceException, 500. |
| M4 | data-import | Cancel/clear read the operation of a not-yet-started task: NRE in the event handler, or an unmatched parse that keeps running while its upload folder is deleted. |
| M2 | data-import | `getMigrationStatus` swallows every exception and returns the empty result; an outage looks like "no job". |
| N3 | notifications | `getNotificationSettings` with an undefined but bindable `type` (e.g. 7) answers 500 "Incorrect parameters" instead of 400; `setNotificationSettings` with it answers 200 and stores nothing. |
| L4, L5 | branding-and-licensing | A logo file whose name does not start with the slot number, or a `data:` URI in an unsupported format, answers 500 instead of 400. |
| L9 | branding-and-licensing | An unparsable licence or one lacking customer id/signature answers 500 "License is not correct" instead of a client error. |
| L10 | branding-and-licensing | Greeting title save answers 500 on a `localhost` installation with more than one portal (alias rewritten to `localhost`, unique index); the .NET tests are commented out. |
| A5 | ai-agents | `aiThreadsRegenerateTitle` answers 500 on a thread with a real conversation. |
| A10 | ai-agents | OpenAI passthrough with a 31 MB body answers 500 with stack trace and internal gateway host; some malformed image requests answer 500 instead of the gateway's 400. |
| A6 | ai-agents | A file id and the Trash root are accepted as thread `entityId`; 0 and -1 are not refused. |
| A8 | ai-agents | MCP replace-all stores two names differing only in case; the case-insensitive lookup reaches one of them. |
| P4 | plugins | Every failure after the size checks, storage I/O included, is wrapped as 400 "Wrong plugin archive. …". |
| W9 | billing | `getPaymentUrl` quantity below 1 is validated after the configuration and admin checks: 403 without billing, 400 with it. |
| W2 | billing | `getCustomerInfo`/`getRestrictedAiModels` answer 200-empty to Guests when billing/AI is not configured and 403 when it is; the rule depends on the installation, not the caller. |
| W8, W10 | billing | "No limits" vs "not a customer" are indistinguishable on the participants page; a failed balance read is cached 10 minutes as "no customer". |
| B3 | backup-and-restore | `startBackupRestore` with the all-zero GUID queues a restore with no source that fails only in the worker (200). |
| B13 | backup-and-restore | The schedule refuses an unwritable storage with 400, but `startBackup` accepts the same body with 200 and lets the job fail in the worker, occupying the single queue slot. |
| B5 | backup-and-restore | `backupFileUpload.ashx` answers 200 with `success:false` for every failure, permission refusals included. |
| B2 | backup-and-restore | The documented 400 "dump on a non-standalone portal" is dead: the space-access demand already answers 403. |
| M8 | data-import | Progress steps use integer division: with more than 30 users or 20 groups the progress does not move during that stage. |
| L6 | branding-and-licensing | A colour theme beyond the limit is dropped and the call answers 200 with the unchanged list. |

### Inconsistencies, messages and documentation

| # | Spec | What the server does |
|---|------|----------------------|
| W3, A14 | billing, ai-agents | Nine `portal/payment/servicelimit` operations, `aiServicesLimit` on the profile, GenerateMeta, the `meta/*` routes, the `aiTools` build field and the optional approve/deny `message` exist in the code but not in `docspace-backend.yaml`; SDKs cannot reach them and no integration test exists. |
| T1 | portal-lifecycle | `Blocked` is missing from the published `TenantStatus`, `PortalUnblock` from `ConfirmType`, `PUT portal/unblock` and `portal/portalrename` from the paths; a generated client fails to read a blocked portal. |
| P7, L12 | plugins, branding-and-licensing | `refreshWebPlugin` and `POST settings/license/trial` are published by the controllers but hidden from OpenAPI and the SDKs. |
| B1 | backup-and-restore | FIXED in `61ff4b1ee4` (bug 80605): `deleteBackup` of an unknown id now answers 404; the yaml still says 500. |
| B6, B14, B15 | backup-and-restore | `cancelBackup` answers `true` for a job not yet registered (which then runs to the end), for a finished job, and dequeues a cancelled job so its `Canceled` status is mostly unobservable. |
| B16 | backup-and-restore | `getBackupProgress` can show `isCompleted: true` with `status` Running and before `error` is visible; the fields are published separately. |
| B8 | backup-and-restore | The free-allowance self-check is a no-op left from the free-to-paid transition (TODO to remove). |
| P3, P6 | plugins | A portal plugin shadowed by a same-name installation-wide plugin can be neither listed nor deleted; the count limit counts the de-duplicated list, re-upload bypasses it, the list is silently truncated. |
| M5 | data-import | The upload folder path is cached for one day; a later parse ends with a generic "file not found" and the folder is never cleaned; deletion is fire-and-forget. |
| M7 | data-import | The group loop returns instead of continuing; the per-group `shouldImport` of the contract is dead. |
| N4 | notifications | Bug 81713: a file updated or renamed by somebody else is not reported as new (.NET tests red, TS `test.fail`). |
| N5 | notifications | `setRoomsNotificationStatus` stores any id forever; no cleanup. |
| N6 | notifications | SMTP save accepts and drops `useNtlm`; validation is only DTO attributes (TODO in the controller). |
| N7 | notifications | Telegram texts declared for portal_deactivate, portal_delete, user_has_join, self_profile_updated, backup_failed are never sent; the Files `subject_*_tg` keys are unreferenced. |
| L7 | branding-and-licensing | `feedbackAndSupportUrl` of the payment settings is always null. |
| L11 | branding-and-licensing | The licensor flag of the company details is always stored as `false`. |
| T9 | portal-lifecycle | A suspended portal answers 404 to `getPortalInformation`, so a client cannot read `status` to learn why, although the yaml tells it to. |
| T10 | portal-lifecycle | `validateportalname` variants leak `<alias>_deleted` aliases of removed portals. |
| A13 | ai-agents | `meta/generate` and `meta/fill` borrow the AI-off gate through a throwaway tool-prefs read instead of a gate of their own. |
| W7 | billing | `setParticipantServiceLimits` does up to 1000 sequential user lookups per call. |

## Suspected defects found in the fifth batch (apps, metadata, matrix-spec verification, 2026-10-25)

From the `verification.md` of `apps` (AP), `metadata` (MD) and the five matrix specs that received a
verification.md in this batch (R; their live-run items keep the D-numbers above). Evidence with file:line
references stays in those files.

### Access and integrity

| # | Spec | What the server does |
|---|------|----------------------|
| R2 | user-type-permissions | A DocSpace admin can deactivate another DocSpace admin and the Owner (bug 79900, .NET tests red). |
| R1 | user-type-permissions | A private room is listed to a non-member Owner / DocSpace admin (bug 82956, tests red). |
| R7 | shared-links | `getFilePrimaryExternalLink` answers 200 to a caller without access to the file (bug 81572, tests red). |
| R4 | room-role-permissions | `POST folder/{id}/link` by a Content creator, Viewer, non-member admin, Guest or on an archived room answers 200 with an empty list instead of 403 (bug 81575, tests red). |
| MD7 | metadata | Section and rooms listings parse the metadata filter before any access check, so any member probes template and field ids through the 400 texts. |
| AP1 | apps | The `app_settings` table is in no backup module: a restore silently loses every application flag and settings document. |
| AP3 | apps | The first write freezes the then-current configured `enabled` default into the row; a portal that only saved settings stops following later configuration changes of the default. |
| MD8 | metadata | Copied values are pasted after stamping without the destination's conflict mode, so an overwrite-cascading destination does not overwrite a copy as it does a move. |
| MD2 | metadata | The stamp pass after a move is invisible to `getCascadeProgress` and writes no audit. |

### Crashes (500) and wrong status codes

| # | Spec | What the server does |
|---|------|----------------------|
| R6 | shared-links | `getFolderPrimaryExternalLink` on a folder inside a Form filling room throws, while `PUT` on the same folder creates a primary link (D12): GET and PUT disagree. |
| R8 | shared-links | The link-limit refusal is produced as an operation warning with the entry skipped, not as an error; invalid `linkType`/`access` answer 403 instead of 400 (bugs 82370, 82371). |
| R3 | user-type-permissions | The invitation-link endpoint refuses the Guest type with 400 "EmployeeType" while the people endpoints answer 403 for the same refusal. |
| AP2 | apps | An application id longer than 64 characters in the configuration reads fine but every write fails at the database → 500; no start-up validation. |
| MD3 | metadata | Raw exception text is returned in the cascade `error` field to any reader. |
| MD1 | metadata | Retyping single ↔ multi choice is refused like any retype, and retyping a choice field requires `options: []`. |

### Inconsistencies, messages and documentation

| # | Spec | What the server does |
|---|------|----------------------|
| AP5 | apps | A bindable body without the `settings` member drops the stored document with 200, indistinguishable from an explicit reset (a test pins it). |
| AP4 | apps | The JSON re-parse and its 400 in the settings write are unreachable; the real 400 comes from model binding. |
| AP6 | apps | A socket event is sent on switching an application, none on saving its settings. |
| MD4, MD5, MD6 | metadata | Empty `templateIds` on a file still writes audit and socket (the folder variant returns early); cascade-off writes no audit while on/unassign do; a no-op custom-field removal still audits, sockets and re-indexes. |
| MD9 | metadata | The yaml predates the number-limit text of `setFileValues` and lacks the 403 row of `getCascadeProgress`; the SDK drops `id`, `assignedMetadataTemplates` and `metadataMatches` from listing entries. |
| R5 | room-role-permissions | `updateFileComment` (version comment) is refused for an Editor; design question, kept outside the Comment row. |
| — | user-type-permissions | Matrix row "copy of an archived room" is allowed by the matrix but `room-types` refuses duplication of an archived room; kept MATRIX-ONLY and flagged, neither spec changed. |
