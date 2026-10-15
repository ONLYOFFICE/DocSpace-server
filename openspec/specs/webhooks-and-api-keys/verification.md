# webhooks-and-api-keys — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo root
(`server/`); test paths omit the `web/ASC.Web.Api.Tests/Tests/` prefix unless another project is named. `yaml`
is `sdk/docspace-api-spec/docspace-backend.yaml`. Status: **TEST-CONFIRMED** (an integration test asserts it),
**CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY** (only the release
notes say it — nothing of that kind was written into the spec; such items are listed at the end as left out).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Developer Tools access rule** | | |
| Owner/DocSpace admin always; Room admin and User only while `limitedAccessForUsers` is false; Guest never; 403 "Access denied" | `WebhooksController.cs` `CheckAdminPermissionsAsync` (IsDocSpaceAdmin → true, IsGuest → SecurityException, LimitedAccessForUsers → SecurityException); `ApiKeysController.cs` `CreateApiKey` (same order, UnauthorizedAccessException → `ResponseWrapper.cs` maps to 403 "Access denied") | CODE-ONLY (rule) |
| Guest refused: create/list webhooks, journal | `02_Settings/Webhooks/CreateWebhookTests.cs` `CreateWebhook_Guest_ThrowsAccessDenied`, `GetTenantWebhooksTests.cs` `GetTenantWebhooks_Guest_ThrowsAccessDenied`, `GetWebhooksLogsTests.cs` `GetWebhooksLogs_Guest_ThrowsAccessDenied` | TEST-CONFIRMED |
| Guest refused: create key (guest-role message), list keys, permission catalogue | `06_ApiKeys/Permissions/CreateApiKeyPermissionsTests.cs` `CreateApiKey_Guest_ReturnsForbidden` (bug 81236), `GetApiKeysListPermissionsTests.cs` `GetApiKeys_Guest_ReturnsForbidden` (81615), `GetAllPermissionsPermissionsTests.cs` `GetAllPermissions_Guest_ReturnsForbidden` (81611) | TEST-CONFIRMED |
| Anonymous 401 on webhooks create/list/journal and on every keys endpoint | `CreateWebhookTests.cs` `CreateWebhook_Anonymous_ThrowsUnauthorized`, `GetTenantWebhooksTests.cs`, `GetWebhooksLogsTests.cs`; `06_ApiKeys/Permissions/*PermissionsTests.cs` `*_Anonymous_ReturnsUnauthorized` (create, list, catalogue, @self, update, delete) | TEST-CONFIRMED |
| Restriction on: Room admin and User 403 creating a key; Owner and DocSpace admin 200 | `05_Security/AccessToDevTools/AccessToDevToolsAffectsApiKeyCreationTests.cs` `CreateApiKey_RoomAdmin_ForbiddenWhenLimitedAccessEnabled`, `CreateApiKey_User_ForbiddenWhenLimitedAccessEnabled` (81236), `CreateApiKey_Owner_AllowedWhenLimitedAccessEnabled`, `CreateApiKey_DocSpaceAdmin_AllowedWhenLimitedAccessEnabled` | TEST-CONFIRMED |
| Restriction off again: Room admin and User 200 | same file, `CreateApiKey_RoomAdmin_AllowedAfterLimitedAccessDisabled`, `CreateApiKey_User_AllowedAfterLimitedAccessDisabled` | TEST-CONFIRMED |
| Restriction also blocks Room admins and Users on the webhook endpoints | `WebhooksController.cs` `CheckAdminPermissionsAsync` (called by list, create, update, enable, remove, journal, retry) | CODE-ONLY |
| Trigger catalogue readable by a Guest | `02_Settings/Webhooks/GetWebhookTriggersTests.cs` `GetWebhookTriggers_Guest_ReturnsNonEmptyList` | TEST-CONFIRMED |
| **Reading and changing the Developer Tools restriction** | | |
| Every type reads it (Owner, DocSpace admin, Room admin, User, Guest) 200; `lastModified` present | `02_Settings/AccessToDevTools/AccessToDevToolsGetTests.cs` `*_Owner_ReturnsSettings`, `*_DocSpaceAdmin_ReturnsSettings`; `AccessToDevToolsGetPermissionsTests.cs` `GetTenantAccessDevToolsSettings_Member_ReturnsSettings` | TEST-CONFIRMED |
| Anonymous read 401 | `AccessToDevToolsGetPermissionsTests.cs` `GetTenantAccessDevToolsSettings_Anonymous_ThrowsUnauthorized` | TEST-CONFIRMED |
| Owner sets true/false, echo; read back by Owner and DocSpace admin; DocSpace admin may set | `05_Security/AccessToDevTools/AccessToDevToolsSetTests.cs` (3 tests); `AccessToDevToolsGetTests.cs` `*_ReflectsEnabledRestriction`, `*_ReflectsDisabledRestriction`, `*_DocSpaceAdmin_ReflectsRestrictionSetByOwner` | TEST-CONFIRMED |
| Room admin/User/Guest 403, anonymous 401 on set | `AccessToDevToolsSetPermissionsTests.cs` | TEST-CONFIRMED |
| Default false; audit trail `DevToolsAccessSettingsChanged`; both methods on `/api/2.0/settings/devtoolsaccess` | `common/ASC.Core.Common/Tenants/TenantAccessDevToolsSettings.cs` `GetDefault`; `SettingsController.cs` `SetTenantDevToolsAccessSettings`; yaml l. 51503 (`setTenantDevToolsAccessSettings`, `getTenantAccessDevToolsSettings`) | CODE-ONLY |
| **Creating a webhook configuration** | | |
| Owner creates enabled webhook: id, name, uri, enabled, createdBy, createdOn | `CreateWebhookTests.cs` `CreateWebhook_Owner_ReturnsCreatedWebhook` | TEST-CONFIRMED |
| ssl and triggers stored | `CreateWebhookTests.cs` `CreateWebhook_Owner_WithTriggersAndSsl_ReturnsCreatedWebhook` | TEST-CONFIRMED |
| Room admin and User create | `CreateWebhookTests.cs` `CreateWebhook_RoomAdminOrUser_ReturnsCreatedWebhook` | TEST-CONFIRMED |
| Field limits (name/secret 50, targetId 255), name/uri required, secret required on create and checked against password rules, secret never returned, duplicate name+uri → 400, audit trail | `web/ASC.Web.Api/ApiModels/RequestsDto/WebhooksConfigRequestsDto.cs`; `WebhooksController.cs` `CheckWebhook` (`creation || secret` → `PasswordSettingsManager.CheckPassword`); `common/ASC.Webhooks.Core/DbWorker.cs` `AddWebhookConfig` ("Webhook with the same name and payload URL already exists"); `WebhooksConfigDto.cs` (no secret) | CODE-ONLY |
| **Validation of the target URL** | | |
| http/https only, https when ssl, blacklist (defaults), DNS 3 s, pinned connection, HEAD without redirects, 400 on rule failure, 500 when no answer | `common/ASC.Core.Common/Security/UrlValidator.cs` (`_defaultBlacklist`, `ValidateAsync`, `PinnedConnectCallback`); `WebhooksController.cs` `CheckWebhook`; `../buildtools/config/appsettings.json` `webhooks:blacklist`; yaml `createWebhook` 400/500 descriptions | CODE-ONLY |
| `http://localhost` → 400 | `products/ASC.People/Tests/Tests/00_Regressions/WebhookTest.cs` `WebhookCreation_CheckBlacklist` (bug 80980) | TEST-CONFIRMED |
| Redirecting target refused; `example.com` accepted | `02_Settings/Webhooks/WebhooksTestData.cs` (documents that `onlyoffice.com` 301 fails and `example.com` passes; every create test relies on it); `WebhooksController.cs` `CheckWebhook` (`IsSuccessStatusCode`, `ErrorWebhookUrlNotAvaliable`) | TEST-CONFIRMED (acceptance) / CODE-ONLY (redirect refusal) |
| **Trigger catalogue and per-role availability** | | |
| Catalogue values, names, bits, order, roles | `common/ASC.Webhooks.Core/WebhookTrigger.cs` (`Description`, `AvailableFor`, `Order` attributes); `products/ASC.People/Tests/Tests/00_Regressions/WebhookTest.cs` (`AdminOnlyTriggers`, `AllRolesTriggers`, `AllTriggers_HaveAvailableForAttribute` — unit assertions on `IsAvailableFor`); yaml `WebhookTrigger` l. 103664 | TEST-CONFIRMED (availability per role) / CODE-ONLY (order, names) |
| Non-empty, first entry named; readable by Owner and Guest | `GetWebhookTriggersTests.cs` (2 tests) | TEST-CONFIRMED |
| User with `UserCreated` → 400 | `products/ASC.People/Tests/.../WebhookTest.cs` `GetTriggers_CheckUserRestrictions` (bug 73741); `WebhooksController.cs` `CheckWebhook` ("Trigger is not available") | TEST-CONFIRMED |
| Owner with `FileCreated` → 200 | `CreateWebhookTests.cs` `CreateWebhook_Owner_WithTriggersAndSsl_ReturnsCreatedWebhook` | TEST-CONFIRMED |
| **Visibility of webhook configurations** | | |
| Owner list contains created webhook with name/uri | `GetTenantWebhooksTests.cs` `GetTenantWebhooks_Owner_ContainsCreatedWebhook` | TEST-CONFIRMED |
| Room admin / User get 200 | `GetTenantWebhooksTests.cs` `GetTenantWebhooks_RoomAdminOrUser_ReturnsList` | TEST-CONFIRMED |
| Admins see all, others only their own; `status` = last delivery status, 0 when none | `WebhooksController.cs` `GetTenantWebhooks` (`userId = admin ? null : caller`); `DbWorker.cs` `GetTenantWebhooksWithStatus`; `WebhooksConfigDtoHelper` (`Status ?? 0`); yaml `getTenantWebhooks` | CODE-ONLY |
| Creator still sees the webhook after a foreign removal attempt | `RemoveWebhookTests.cs` `RemoveWebhook_AnotherUsersWebhook_ThrowsAccessDenied` | TEST-CONFIRMED |
| **Updating a webhook configuration** | | |
| Owner renames/re-targets: same id, new name/uri, modifiedOn | `UpdateWebhookTests.cs` `UpdateWebhook_Owner_NameAndUriAreUpdated` | TEST-CONFIRMED |
| Full replace, secret kept when empty, validation before lookup, 404 unknown, 403 foreign (non-admin), audit trail | `WebhooksController.cs` `UpdateWebhook`; yaml `updateWebhook` | CODE-ONLY |
| **Switching a webhook on or off** | | |
| Owner disables: id/name/uri unchanged, enabled false, modifiedOn | `EnableWebhookTests.cs` `EnableWebhook_Owner_TogglesEnabledState` | TEST-CONFIRMED |
| Foreign webhook → 403 "Access denied" | `EnableWebhookTests.cs` `EnableWebhook_AnotherUsersWebhook_ThrowsAccessDenied` | TEST-CONFIRMED |
| Re-validation on enable only, 404 unknown, events dropped while off | `WebhooksController.cs` `EnableWebhook`; `WebhookPublisher.cs` `GetWebhookConfigsAsync` reads only active configs (`DbWorker.GetActiveWebhookConfigsFromCache`) | CODE-ONLY |
| **Removing a webhook configuration** | | |
| Owner removes: 200 with id, gone from list | `RemoveWebhookTests.cs` `RemoveWebhook_Owner_RemovesItFromTheList` | TEST-CONFIRMED |
| Foreign → 403, still listed for creator | `RemoveWebhookTests.cs` `RemoveWebhook_AnotherUsersWebhook_ThrowsAccessDenied` | TEST-CONFIRMED |
| 404 unknown / second removal; journal rows vanish (inner join on config); audit trail | `WebhooksController.cs` `RemoveWebhook`; `DbWorker.cs` `GetQueryForJournal` (join with configs) | CODE-ONLY |
| **Which actions emit which events, and to whom** | | |
| Audience filter: enabled, trigger mask or 0, targetId, creator may read entity / view user / read groups | `common/ASC.Webhooks.Core/WebhookPublisher.cs` `GetWebhookConfigsAsync`; `products/ASC.Files/Core/Utils/WebhookManager.cs` (`WebhookFileEntryAccessChecker.CheckAccessAsync` → `FileSecurity.CanReadAsync`); `common/ASC.Api.Core/Webhook/UserWebhookManager.cs` (`CanUserViewAnotherUserAsync`, `Action_ReadGroups`) | CODE-ONLY |
| Emission sites: user.created (people API, invitation confirm, third-party sign-up), user.invited, group.created, room.created vs agent.created, room.copied, file.downloaded | LSP `findReferences` on the enum members: `products/ASC.People/Server/Api/UserController.cs:232,431,552`, `ThirdpartyController.cs:286`, `GroupsController.cs:221`, `products/ASC.Files/Core/Core/FileStorageService.cs:1044-1045` (`folder.IsAgent ? AgentCreated : RoomCreated`), `FileMoveCopyOperation.cs:343`, `FileDownloadOperation.cs:284`, `FileHandler.ashx.cs:328` | CODE-ONLY |
| Form events | `openspec/specs/form-filling-workflow/spec.md` "Webhook events of the workflow"; `EntryManager.cs:2561` (`FormFilledOut`) | CODE-ONLY (cross-reference) |
| **Delivery of a webhook** | | |
| POST, `Accept: */*`, JSON body, `x-docspace-signature-256: sha256=<HMAC-SHA256 hex>` | `web/ASC.Web.Studio/WebHooks/WebhookSender.cs` `Send`, `GetSecretHash` (`Convert.ToHexString`) | CODE-ONLY |
| Payload shape `event` / `payload` / `webhook`, camel case, UTC, defaults omitted | `common/ASC.Webhooks.Core/WebhookPayload.cs`; `WebhookPublisher.cs` `_serializerOptions` | CODE-ONLY |
| Re-validation at delivery: blacklisted → deleted, other failure → disabled, audit trail | `WebhookSender.cs` `DisableWebhook` | CODE-ONLY |
| Retries: `webhooks:repeatcount` default 5, exponential from 1 s, retry fields in payload | `WebhookSender.cs` `AddWebhookSenderHttpClient` (`MaxRetryAttempts`, `Delay`, `Exponential`), `Send` (`retryCount > 0` branch); `web/ASC.Web.Studio/WebHooks/Settings.cs` | CODE-ONLY |
| 410 → subscription deleted "(HTTP status 410)" | `WebhookSender.cs` `catch (HttpRequestException)` | CODE-ONLY |
| Final status, 500 on non-HTTP failure, headers/bodies stored, `delivery` only on success, `lastSuccessOn` | `WebhookSender.cs` `Send`; `DbWorker.cs` `UpdateWebhookJournal` | CODE-ONLY |
| Disabled after more than `trustedDaysCount` (3) days without success | `WebhookSender.cs` (`TimeSpan.FromDays(settings.TrustedDaysCount ?? 3)`) | CODE-ONLY |
| **Delivery journal** | | |
| Owner reads with count 10 → 200 | `GetWebhooksLogsTests.cs` `GetWebhooksLogs_Owner_ReturnsList` | TEST-CONFIRMED |
| Guest 403, anonymous 401 | `GetWebhooksLogsTests.cs` | TEST-CONFIRMED |
| Fields, newest first, count 1–100 default 100 (400 outside), total, filters AND, groupStatus bits, non-admin forced to own `userId` | `web/ASC.Web.Api/ApiModels/RequestsDto/WebhookLogsRequestDto.cs` (`Range(1, ApiContext.MaxCount)`, `common/ASC.Api.Core/Core/ApiContext.cs:39-40`); `WebhooksController.cs` `GetWebhooksLogs`; `DbWorker.cs` `GetQueryForJournal`, `WebhookGroupStatus`; `WebhooksLogDto.cs` | CODE-ONLY |
| **Retrying a delivery** | | |
| Empty batch → 200 empty | `RetryWebhooksTests.cs` `RetryWebhooks_EmptyIdList_ReturnsEmptyResult` | TEST-CONFIRMED |
| Single retry: new record, 400 for id 0, 404 unknown, 403 foreign; batch skips silently; rate limit | `WebhooksController.cs` `RetryWebhook`, `RetryWebhooks` (`EnableRateLimiting(SensitiveApi)`); `WebhookPublisher.cs` `RetryPublishAsync` | CODE-ONLY |
| **Creating an API key** | | |
| Every type, with/without scopes and expiry: fields, createBy, expiresAt iff expiresInDays | `06_ApiKeys/CreateApiKeyTests.cs` `CreateApiKey_ByRole_ReturnsCreatedKey` (24 cases) | TEST-CONFIRMED |
| Name kept verbatim incl. markup | `06_ApiKeys/CreateApiKeySpecialCharacterNameTests.cs` | TEST-CONFIRMED |
| Blank/whitespace name 400 "The Name field is required."; missing property 400; 31 chars 400 | `06_ApiKeys/Permissions/CreateApiKeyPermissionsTests.cs` `CreateApiKey_Owner_BlankName_ReturnsBadRequest`, `*_WithoutNameProperty_*`, `*_NameLongerThan30Characters_*` | TEST-CONFIRMED |
| Empty permissions array 400 "Permissions are not valid." | same file, `CreateApiKey_Owner_EmptyPermissionsArray_ReturnsBadRequest` (bug 81237) | TEST-CONFIRMED |
| Key format `sk-` + 64 hex, hashed storage, shown once, `keyPostfix`, `*` → unrestricted (stored as empty list), expiresInDays 1–365, unknown scope 400, rate limit, audit trail | `web/ASC.Web.Core/Users/ApiKeyManager.cs` `GenerateApiKey`, `CreateApiKeyAsync`; `common/ASC.Core.Common/EF/Model/ApiKey.cs` (`Key => $"sk-***{KeyPostfix}"`); `products/ASC.People/Server/ApiModels/RequestDto/CreateApiKeyRequestDto.cs`; `ApiKeysController.cs` `CreateApiKey`, `IsValidPermission` | CODE-ONLY |
| **Permission catalogue of API keys** | | |
| Exactly the 11 values in order, count 11, for every type | `06_ApiKeys/GetAllPermissionsTests.cs` `GetAllPermissions_ByRole_ReturnsFullCatalog` | TEST-CONFIRMED |
| **Listing API keys** | | |
| Owner and DocSpace admin see everybody's keys | `06_ApiKeys/GetApiKeysListTests.cs` `GetApiKeys_Owner_SeesKeysOfAllUsers`, `GetApiKeys_DocSpaceAdmin_SeesKeysOfAllUsers` | TEST-CONFIRMED |
| Room admin and User see only their own | same file, `GetApiKeys_RoomAdmin_SeesOnlyOwnKeys`, `GetApiKeys_User_SeesOnlyOwnKeys` | TEST-CONFIRMED |
| Newest first, expired/inactive kept, masked key | `common/ASC.Core.Common/EF/Context/ApiKeysDbContext.cs` (`OrderByDescending(CreateOn)`, no status filter); `ApiKeyMapper` | CODE-ONLY |
| **Reading the key that authenticates the request** | | |
| Key reads itself: id, name, `sk-***`, postfix, empty permissions, isActive, count 1 | `06_ApiKeys/GetOwnApiKeyTests.cs` `GetApiKey_ByRole_ReturnsOwnKeyInfo` | TEST-CONFIRMED |
| Scoped key reads itself with its scopes | `06_ApiKeys/Scenarios/ApiKeyAuthScenariosTests.cs` `ApiKeyWithScopedPermissions_CanGetOwnKeyInfo` (bug 74914) | TEST-CONFIRMED |
| User token → 400 | `GetOwnApiKeyTests.cs` `GetApiKey_UserToken_ReturnsBadRequest` (bug 84486) | TEST-CONFIRMED |
| **Updating an API key** | | |
| Every type updates own key: true, count 1, PUT link | `06_ApiKeys/UpdateApiKeyTests.cs` `UpdateApiKey_ByRole_UpdatesOwnKey` | TEST-CONFIRMED |
| DocSpace admin updates Owner's key | same file, `UpdateApiKey_DocSpaceAdmin_UpdatesOwnersKey` | TEST-CONFIRMED |
| Deactivate / reactivate reflected in listing | same file, `UpdateApiKey_Owner_DeactivatesOwnKey`, `UpdateApiKey_Owner_ActivatesDeactivatedKey` | TEST-CONFIRMED |
| Room admin / User / Guest on Owner's key → 403 | `06_ApiKeys/Permissions/UpdateApiKeyPermissionsTests.cs` `UpdateApiKey_NonOwnerRole_CannotUpdateOwnersKey` (bug 81616; `ApiKeysController.UpdateApiKey` now throws SecurityException) | TEST-CONFIRMED |
| Empty / whitespace name → 400, name unchanged | `UpdateApiKeyTests.cs` `UpdateApiKey_EmptyOrWhitespaceName_ReturnsBadRequestAndKeepsName` (bug 84485) | TEST-CONFIRMED |
| Unknown / deleted key → 404 | `UpdateApiKeyTests.cs` `UpdateApiKey_UnknownKeyId_ReturnsNotFound`, `UpdateApiKey_DeletedKey_ReturnsNotFound` (bug 84482) | TEST-CONFIRMED |
| Left-out fields keep values, `["*"]` → unrestricted, expired key → false, audit trail | `ApiKeyManager.cs` `UpdateApiKeyAsync`; `ApiKeysController.cs` `UpdateApiKey` | CODE-ONLY |
| **Deleting an API key** | | |
| Every type deletes own key: true, count 1, DELETE link | `06_ApiKeys/DeleteApiKeyTests.cs` `DeleteApiKey_ByRole_DeletesOwnKey` | TEST-CONFIRMED |
| DocSpace admin deletes Owner's key | same file, `DeleteApiKey_DocSpaceAdmin_DeletesOwnersKey` | TEST-CONFIRMED |
| Room admin / User / Guest on Owner's key → 403 | `06_ApiKeys/Permissions/DeleteApiKeyPermissionsTests.cs` `DeleteApiKey_NonOwnerRole_CannotDeleteOwnersKey` (bug 81609) | TEST-CONFIRMED |
| Unknown / deleted key → 404 | `DeleteApiKeyTests.cs` `DeleteApiKey_UnknownKeyId_ReturnsNotFound`, `DeleteApiKey_AlreadyDeletedKey_ReturnsNotFound` (bug 84483) | TEST-CONFIRMED |
| **Authenticating with an API key** | | |
| Scheme chosen for `Bearer sk-...` that is not a JWT; hash lookup; active and unexpired only; 401 otherwise; runs as creator; lastUsed; scope claims; unrestricted = `*:write` | `common/ASC.Api.Core/Core/BaseStartup.cs:507-520`; `common/ASC.Api.Core/Auth/ApiKeyBearerAuthHandler.cs`; `ApiKeyManager.cs` `ValidateApiKeyAsync`; `ApiKeysDbContext.cs` `ValidateApiKeyAsync` (`IsActive && (ExpiresAt == null || ExpiresAt > now)`) | CODE-ONLY |
| Valid key is accepted as the creator | `GetOwnApiKeyTests.cs` (every `AsApiKeyAsync` call succeeds as the creator) | TEST-CONFIRMED |
| **Scope enforcement for API-key requests** | | |
| Route map, GET read-or-write, unmapped routes `*:read`/`*:write`, `keys/@self` free, anonymous endpoints too, 403 | `common/ASC.Api.Core/Auth/AuthorizationExtension.cs` (`ScopesMap`, `GetAuthorizePolicy`, `WithRequirementAuthorization`); `ScopesAuthorizationHandler.cs`; `common/ASC.Api.Core/Middleware/ApiKeyScopesFilter.cs` | CODE-ONLY |
| `files:read` key cannot `POST /keys` → 403 | `ApiKeyAuthScenariosTests.cs` `ApiKeyWithFilesReadScope_CannotCreateAnotherApiKey` (bug 81238) | TEST-CONFIRMED |
| User's key with accounts scopes still 403 on people list | `ApiKeyAuthScenariosTests.cs` `UserApiKey_WithAccountsPermissions_CannotAccessGetAllProfiles` | TEST-CONFIRMED |
| Scoped key reads `@self` | `ApiKeyAuthScenariosTests.cs` `ApiKeyWithScopedPermissions_CanGetOwnKeyInfo` | TEST-CONFIRMED |
| **Expiry of an API key** | | |
| Job every 10 min deactivates expired keys and notifies active, activated creators | `products/ASC.Files/Worker/Services/AutoDeactivateExpiredApiKeysService.cs` | CODE-ONLY |
| Letter subject "Your API key is expired", body "has expired and has been deactivated", name HTML-escaped | `common/Tests/ASC.Notify.Tests/Account/ApiKeyExpiredLetterTests.cs` (bug 82910) | TEST-CONFIRMED (letter) |
| Expired key refused before the job | `ApiKeysDbContext.cs` `ValidateApiKeyAsync` | CODE-ONLY |

Counts: 78 rows — TEST-CONFIRMED 49, CODE-ONLY 29 (two rows carry both and are counted once, under
TEST-CONFIRMED), NOTES-ONLY 0 (nothing was written into the spec on the strength of the release notes alone).
Spec: 22 requirements, 70 scenarios.

## Where the server differs from the release notes or the endpoint texts (spec follows the server)

| Release note / document | Server |
|---|---|
| v3.1: "Guests never have access, for User it can be configured, everybody else always has access" — and the `devtoolsaccess` remarks: "`RoomAdmin` and `DocSpaceAdmin` are never affected by this setting" | Both `WebhooksController.CheckAdminPermissionsAsync` and `ApiKeysController.CreateApiKey` test only `IsDocSpaceAdmin`, so a **Room admin is blocked as well** while `limitedAccessForUsers` is on. `AccessToDevToolsAffectsApiKeyCreationTests.CreateApiKey_RoomAdmin_ForbiddenWhenLimitedAccessEnabled` asserts the 403 (it is a bug-81236 test, i.e. the behaviour was asked for). The spec states the server rule. |
| v3.1: "the admin sees all created webhooks" | True, and in addition a non-admin sees only their own (`GetTenantWebhooks` filters by creator). The `GetTenantWebhooksTests` class comment says "any member sees the whole tenant list" but no assertion checks that; the code filters. |
| `getTenantWebhooks` / `createWebhook` remarks on the TS expectation "This operation available only for portal owner/admins" | The message reaching the client is the generic "Access denied" (`ResponseWrapper` rewrites `UnauthorizedAccessException`); the tests assert "Access denied". |
| yaml `WebhookTrigger.description` lists the triggers only up to `form.stopped` (1073741824) | The enum (and `x-enum-varnames`) also has `agent.created` (2^31), `agent.updated` (2^32), `agent.deleted` (2^33), `file.downloaded` (2^34), `folder.downloaded` (2^35); the description text is stale. |
| `WebhooksLogDto` example shows a header `x-docspace-signature` | The sender writes `x-docspace-signature-256: sha256=<hex>` (`WebhookSender.SignatureHeader`). |
| `UpdateApiKeyTests` / `DeleteApiKeyPermissionsTests` class comments: "the endpoint currently lets a non-owner delete/rename the owner's key" (bugs 81609, 81616) | `ApiKeysController.UpdateApiKey` / `DeleteApiKey` throw `SecurityException` ("Access denied") for a non-admin on a foreign key; the tests assert 403 and the spec states 403. The comments are stale descriptions of the old behaviour. |
| v3.7: "e-mail when an API key expires" | Implemented in the Files worker (`AutoDeactivateExpiredApiKeysService`), i.e. the notice is sent only while a Files worker instance runs the active-passive job; the letter itself is tested. |

## Left out of the spec, and why

- **Developer Tools page, JavaScript SDK samples, OAuth2 applications** (v1.1, v3.0, v3.5, v3.7, v4.0): client
  pages and the OAuth2 service (`ASC.Identity`) are outside this capability; OAuth2 is not covered by the Web.Api
  test suites read here.
- **`WebhookDisable` attribute** (`common/ASC.Api.Core/Core/WebhookDisableAttribute.cs`): marks controllers whose
  actions must not themselves be reported as webhook events; the publishing is explicit through the managers, so
  nothing observable to specify.
- **Exact payload of each event** (`FileDto`, `EmployeeDto`, `GroupDto`, `SubmittedFormData`): the entity is
  serialised as-is; its shape is the DTO of the respective product and changes with it. The spec names the
  envelope only.
- **The `WebhookTrigger` bit values above 2^30** are given as powers of two because the OpenAPI document and the
  SDK carry them as `long` values; no test asserts them.
- **`ssl=false` with an `https` target**: the HEAD probe and the delivery then skip certificate validation
  (`PinnedHttpClientSslIgnore`, `WebhookHttpClientSslIgnore`); mentioned in the URL-validation requirement, no
  test.
- **Thread count of the sender** (`webhooks:threadcount`, default 10): operational, not behaviour.
- **TS scenario "API key with accounts:read scope cannot access rooms"**: not ported (the Web.Api test project
  has no Files client); the route map in the spec is CODE-ONLY for the rooms rows.
- **`lastModified` of the setter's echo**: the tests note it carries the default timestamp on this host; not
  specified.
- **Audit-trail entries** (`WebhookCreated/Updated/Deleted`, `ApiKeyCreated/Updated/Deleted`,
  `DevToolsAccessSettingsChanged`) are named in the requirements but their content belongs to the audit-trail
  capability.

## Suspected defects (not written into the spec)

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| S1 | API keys, update | `ApiKeyManager.UpdateApiKeyAsync` treats `permissions: ["*"]` as "unrestricted" only when it is the single element; `["*", "files:read"]` passes `IsValidPermission` (both are catalogue values) and is stored as-is, after which the auth handler (`!Permissions.Exists(x == "*")` → falls through to `*:write`) makes the key unrestricted, while `GET keys/@self` and the listing show the two scopes. On creation `["*", "files:read"]` is normalised to an empty list. Inconsistent, no test. | `ApiKeyManager.cs` `CreateApiKeyAsync` vs `UpdateApiKeyAsync`; `ApiKeyBearerAuthHandler.cs` |
| S2 | API keys, update | An update of an already expired key answers `200 false` after the 404 check but **before** the ownership check, so a Room admin or User learns that a foreign key id exists and is expired (`false`) instead of 403. | `ApiKeysController.cs` `UpdateApiKey` (expiry test precedes `CreateBy` test) |
| S3 | API keys, delete | `ApiKeyManager.DeleteApiKeyAsync` returns `result >= 0`, i.e. `true` even when `ExecuteDelete` removed nothing; harmless today because the controller answers 404 first, but the method cannot report a miss. | `ApiKeyManager.cs` `DeleteApiKeyAsync` |
| S4 | Webhooks, enable | `PUT settings/webhook/enable` demands `name`, `uri` (required) and length-limited `secretKey` in the body although it reads only `id` and `enabled`; a client that sends only the two fields gets 400. Documented in the endpoint text, but it is an accident of reusing the update DTO. | `WebhooksController.cs` `EnableWebhook`; `UpdateWebhooksConfigRequestsDto` |
| S5 | Webhooks, update | `PUT settings/webhook` validates (including the outbound HEAD request) **before** checking ownership, so any non-admin member can make the server probe an arbitrary public URL by naming another member's webhook id (answer is 403 afterwards). Same order on creation is by design (the caller owns the result), but here the probe is spent on a request that is then refused. | `WebhooksController.cs` `UpdateWebhook` (`CheckWebhook` first, `CreatedBy` check later) |
| S6 | Developer Tools rule | The restriction blocks Room admins although the setting is documented (notes v3.1, `devtoolsaccess` remarks) as affecting Users only; whether this is intended is unclear — the test for bug 81236 asserts the block, so the spec follows the server. | see the table above |
| S7 | OpenAPI document | `WebhookTrigger.description` omits the five triggers added after `form.stopped`; `WebhooksLogDto` example names the wrong signature header. | yaml l. 103664-103700, `WebhooksLogDto.cs` example |
