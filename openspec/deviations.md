# Known deviations between the access-rights specifications and the server

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
