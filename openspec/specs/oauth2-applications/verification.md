# oauth2-applications — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement of `spec.md`, with where it was verified. Paths are relative to the repo root (`server/`). `yaml`
is `sdk/docspace-api-spec/docspace-backend.yaml` (cited by operationId or schema). Tests: `CM/` is
`web/ASC.Web.Api.Tests/Tests/08_OAuth2/ClientManagement/`, `CQ/` is `.../08_OAuth2/ClientQuerying/`, `SM` is
`.../08_OAuth2/ScopeManagement/ScopeManagementTests.cs`, `JWT` is
`web/ASC.Web.Api.Tests/Tests/05_Security/OAuth2/GenerateJwtTokenTests.cs`, `TS` is
`../tests/api-tests/src/tests/oauth2.0/`. A `file:line` is the assertion line.

**The identity service is outside the C# tree.** Everything except `generateJwtToken` is served by the Java service in
`common/ASC.Identity` (registration and authorization). As agreed for this spec, those rows cite only the tests and
the yaml. One exception is disclosed in the row it concerns: nothing in the tests or the yaml says what a restricted
member is treated as, so that one mapping was read from the service's signature provider and is marked CODE-ONLY.

Status: **TEST-CONFIRMED** (an integration test of this repository asserts it, assertion read), **CODE-ONLY** (read
from the yaml or from C# code, no test). Nothing was built or run for this spec: a test is cited for what it asserts,
not because it was seen to pass. Tests with a `Bug` trait are expected red and are not used as evidence for a
requirement; they are in "Suspected defects".

**Guest tests that do not test a guest.** `*_Guest_ThrowsForbidden` in `CM/` (all eight operations) and
`GetScopes_GuestWithoutSignature_*` sign the guest in to the portal but send no signature (`ApplySignature(null)`).
They prove the no-signature refusal only. Only the `CQ/` guest tests (`GetClient`, `GetClientInfo`, `GetClients`,
`GetClientsInfo`, `GetConsents`, `GetPublicClientInfo`) use the guest's own signature.

## Identity signature for the calling member

| Claim | Source | Status |
|---|---|---|
| Owner gets a three-part JWT | `JWT:56` | TEST-CONFIRMED [run: NO-LINK] |
| DocSpace admin, Room admin, User, Guest get one | `JWT:74` | TEST-CONFIRMED [run: NO-LINK] |
| Anonymous 401 | `JWT:88` | TEST-CONFIRMED [run: NO-LINK] |
| Five-minute expiry; caller's own identity | `common/ASC.Core.Common/Identity/IdentityClient.cs:89-91,124`; yaml `generateJwtToken` | CODE-ONLY |
| Not a credential for the portal API | yaml `generateJwtToken` | CODE-ONLY |

## Signature required by the application API

| Claim | Source | Status |
|---|---|---|
| No signature 403 on write operations | `CM/CreateClientPermissionsTests.cs:57`, `CM/UpdateClientPermissionsTests.cs:59`, `CM/DeleteClientPermissionsTests.cs:56`, `CM/DeleteUserClientsPermissionsTests.cs:56`, `CM/DeleteTenantClientsPermissionsTests.cs:56`, `CM/RegenerateSecretPermissionsTests.cs:57`, `CM/ChangeActivationPermissionsTests.cs:59`, `CM/RevokeUserClientPermissionsTests.cs:57` | TEST-CONFIRMED [run: PASS] |
| No signature 403 on read operations | `CQ/GetClientTests.cs:102`, `CQ/GetClientInfoTests.cs:100`, `CQ/GetClientsTests.cs:200`, `CQ/GetClientsInfoTests.cs:201`, `CQ/GetConsentsTests.cs:89`, `SM:89` | TEST-CONFIRMED [run: FAIL-BUG] |
| Portal session without a signature 403 | `CM/CreateClientPermissionsTests.cs:74`, `CM/UpdateClientPermissionsTests.cs:78`, `CM/DeleteClientPermissionsTests.cs:72`, `CM/DeleteUserClientsPermissionsTests.cs:72`, `CM/DeleteTenantClientsPermissionsTests.cs:72`, `CM/RegenerateSecretPermissionsTests.cs:74`, `CM/ChangeActivationPermissionsTests.cs:78`, `CM/RevokeUserClientPermissionsTests.cs:74`, `SM:107` | TEST-CONFIRMED [run: PASS] |

## Registering a client

| Claim | Source | Status |
|---|---|---|
| Owner, DocSpace admin, Room admin, User: 201 and the fields echoed | `CM/CreateClientTests.cs:77-98` | TEST-CONFIRMED [run: PASS] |
| `logout_redirect_uri` in, `logout_redirect_uris` out | `CM/CreateClientTests.cs:91` | TEST-CONFIRMED [run: PASS] |
| Tenant cap 400 | yaml `createClient` | CODE-ONLY |

## Client field validation on create

| Claim | Source | Status |
|---|---|---|
| `name` 257 and 2 characters: 400, `errors` entry | `CM/CreateClientPermissionsTests.cs:89,105`, `CM/ClientManagementTestBase.cs:91-101` | TEST-CONFIRMED [run: PASS] |
| `scopes` empty | `CM/CreateClientPermissionsTests.cs:121` | TEST-CONFIRMED [run: PASS] |
| `logo` missing | `CM/CreateClientPermissionsTests.cs:234` | TEST-CONFIRMED [run: PASS] |
| Not-a-URL in `redirect_uris`, `allowed_origins`, `website_url`, `terms_url`, `policy_url`, `logout_redirect_uri` | `CM/CreateClientPermissionsTests.cs:136,151,166,181,196,211` | TEST-CONFIRMED [run: PASS] |
| Unknown scope 400; 1 to 12 entries | yaml `createClient`, `CreateClientRequest` | CODE-ONLY |

## Which clients a caller sees

| Claim | Source | Status |
|---|---|---|
| Owner reads a User's client | `CQ/GetClientTests.cs:86`, `CQ/GetClientInfoTests.cs:84` | TEST-CONFIRMED [run: PASS] |
| User on another member's client 404 | `CQ/GetClientTests.cs:152`, `CQ/GetClientInfoTests.cs:150`, `CM/UpdateClientPermissionsTests.cs:97`, `CM/DeleteClientPermissionsTests.cs:89`, `CM/RegenerateSecretPermissionsTests.cs:92`, `CM/ChangeActivationPermissionsTests.cs:96` | TEST-CONFIRMED [run: PASS] |
| Unknown id 404 | `CQ/GetClientTests.cs:134`, `CQ/GetClientInfoTests.cs:132`, `CM/UpdateClientPermissionsTests.cs:171`, `CM/DeleteClientPermissionsTests.cs:104`, `CM/RegenerateSecretPermissionsTests.cs:107`, `CM/ChangeActivationPermissionsTests.cs:113` | TEST-CONFIRMED [run: PASS] |
| Administrator may update, delete, regenerate, activate another member's client | yaml `updateClient`, `deleteClient`, `regenerateSecret`, `changeActivation`; no test | CODE-ONLY |
| DocSpace admin reads a single client of another member | yaml `getClient`; only the Owner is tested | CODE-ONLY |

## Guests hold no clients

| Claim | Source | Status |
|---|---|---|
| Guest's own signature: 403 on the four reads | `CQ/GetClientTests.cs:119`, `CQ/GetClientInfoTests.cs:117`, `CQ/GetClientsTests.cs:215`, `CQ/GetClientsInfoTests.cs:216` | TEST-CONFIRMED [run: PASS] |
| Guest's own signature: 403 on the write operations | yaml `createClient` ("Administrators and users"), `deleteTenantClients`; the `CM/` guest tests send no signature (see above) | CODE-ONLY |

## Reads and lists

| Claim | Source | Status |
|---|---|---|
| Full record of the caller's client | `CQ/GetClientTests.cs:65-70` | TEST-CONFIRMED [run: PASS] |
| Info of the caller's client | `CQ/GetClientInfoTests.cs:65-68` | TEST-CONFIRMED [run: PASS] |
| Info omits secret, redirect URIs, allowed origins | yaml `getClientInfo`, `ClientInfoResponse` | CODE-ONLY |
| Public info without a signature | `CQ/GetPublicClientInfoTests.cs:78` | TEST-CONFIRMED [run: PASS] |
| Public info for a guest and for a non-creator | `CQ/GetPublicClientInfoTests.cs:95,112` | TEST-CONFIRMED [run: PASS] |
| No tenant scoping and no creator check on public info | yaml `getPublicClientInfo` | CODE-ONLY |
| Full list carries the secret | `CQ/GetClientsTests.cs:82-85` | TEST-CONFIRMED [run: PASS] |
| Info list carries no `client_secret` | `CQ/GetClientsInfoTests.cs:83-86` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin sees the tenant | `CQ/GetClientsTests.cs:150`, `CQ/GetClientsInfoTests.cs:151` | TEST-CONFIRMED [run: PASS] |
| User and Room admin see their own | `CQ/GetClientsTests.cs:168,186`, `CQ/GetClientsInfoTests.cs:169,187` | TEST-CONFIRMED [run: PASS] |
| Empty list | `CQ/GetClientsTests.cs:99`, `CQ/GetClientsInfoTests.cs:100` | TEST-CONFIRMED [run: PASS] |
| Newest first | yaml `getClients` | CODE-ONLY |
| `limit` echoed | `CQ/GetClientsTests.cs:132`, `CQ/GetClientsInfoTests.cs:133` | TEST-CONFIRMED [run: PASS] |
| Keyset cursors, range, defaults, 400s | yaml `getClients`, `getClientsInfo` | CODE-ONLY |

## Update, activation, secret, delete

| Claim | Source | Status |
|---|---|---|
| Update 200 per role (no read-back: the test only asserts the call does not throw) | `CM/UpdateClientTests.cs:49-59` | TEST-CONFIRMED [run: PASS] |
| Invalid `allowed_origins` 400 | `CM/UpdateClientPermissionsTests.cs:150` | TEST-CONFIRMED [run: PASS] |
| Which fields change; required fields | yaml `updateClient`, `UpdateClientRequest` | CODE-ONLY |
| Deactivate per role; reactivate (status only, `enabled` not read back) | `CM/ChangeActivationTests.cs:60,77` | TEST-CONFIRMED [run: PASS] |
| Disabled client gets no new tokens; existing tokens stay; missing `status` 400 | yaml `changeActivation`, `ChangeClientActivationRequest` | CODE-ONLY |
| Regenerate per role: new secret differs | `CM/RegenerateSecretTests.cs:61-63` | TEST-CONFIRMED [run: PASS] |
| Old secret stops working; issued tokens keep working | yaml `regenerateSecret` | CODE-ONLY |
| Delete per role | `CM/DeleteClientTests.cs:60` | TEST-CONFIRMED [run: PASS] |
| Authorizations and consents removed afterwards | yaml `deleteClient` | CODE-ONLY |

## Bulk deletes

| Claim | Source | Status |
|---|---|---|
| `deleteUserClients` per role; gone; nothing to delete | `CM/DeleteUserClientsTests.cs:62,86,99` | TEST-CONFIRMED [run: PASS] |
| Other members' clients stay | `CM/DeleteUserClientsPermissionsTests.cs:91` | TEST-CONFIRMED [run: PASS] |
| Never reaches other clients for an administrator | yaml `deleteUserClients` | CODE-ONLY |
| `deleteTenantClients` Owner and DocSpace admin; gone; empty | `CM/DeleteTenantClientsTests.cs:61,85,98` | TEST-CONFIRMED [run: PASS] |
| Room admin and User 403 | `CM/DeleteTenantClientsPermissionsTests.cs:87,102` | TEST-CONFIRMED [run: PASS] |

## Consents and scopes

| Claim | Source | Status |
|---|---|---|
| Empty list and `limit` 50 | `CQ/GetConsentsTests.cs:62,75` | TEST-CONFIRMED [run: PASS] |
| Caller's own consents, any role including Guest; range; 503 | yaml `getConsents`; a non-empty list needs the browser flow and is not tested (`CQ/GetConsentsTests.cs:36-46`) | CODE-ONLY |
| Revoke without prior consent 200; unknown client 200 | `CM/RevokeUserClientTests.cs:58`, `CM/RevokeUserClientPermissionsTests.cs:88` | TEST-CONFIRMED [run: PASS] |
| Touches only the caller's grant; re-authorization needed | yaml `revokeUserClient` | CODE-ONLY |
| Catalogue per role: seven scopes with group and type | `SM:74` | TEST-CONFIRMED [run: NO-LINK] |
| Order, any valid signature including a Guest's | yaml `getScopes`; the guest test sends no signature | CODE-ONLY |

## Authorization endpoints

No integration test: `TS/authorization/authorization.spec.ts` only records that the three operations need a browser
flow, and `authorization.permissions.spec.ts` is empty.

| Claim | Source | Status |
|---|---|---|
| `authorizeOAuth` parameters, 302 targets, `X-Disable-Redirect`, 400 | yaml `authorizeOAuth` | CODE-ONLY |
| `submitConsent` form, stored consent, 302, 400 | yaml `submitConsent` | CODE-ONLY |
| `exchangeToken` fields, 200, 401, 400, single use | yaml `exchangeToken` | CODE-ONLY |

## Developer tools setting gates applications

| Claim | Source | Status |
|---|---|---|
| Signature carries the setting; loaded when the signature is issued | `common/ASC.Core.Common/Identity/IdentityClient.cs:102-107,122`; yaml `generateJwtToken` | CODE-ONLY |
| Only a DocSpace admin (the Owner reads as one) counts as administrator; a Room admin does not | `IdentityClient.cs:95`; `CQ/GetClientsTests.cs:186`, `CM/DeleteTenantClientsPermissionsTests.cs:87` | TEST-CONFIRMED [run: PASS] |
| Restricted Room admin and User are treated as a Guest | `common/ASC.Identity/registration/registration-application/.../SignatureAuthenticationProvider.java:88-93` (the disclosed exception); no test combines the setting with an OAuth call | CODE-ONLY |
| Takes effect with the next signature; a signature issued earlier keeps its value up to five minutes | follows from `IdentityClient.cs:102-107,124` | CODE-ONLY |

## Clients follow their owner and portal

| Claim | Source | Status |
|---|---|---|
| Deleting a non-guest member deletes their clients | `products/ASC.People/Server/Api/UserController.cs:798-801,1612-1615` | CODE-ONLY |
| Reassign operation deletes them | `common/ASC.Data.Reassigns/ReassignProgressItem.cs:190-193` | CODE-ONLY |
| Portal removal: tenant clients deleted, failure surfaces unless automatic | `web/ASC.Web.Core/RemovePortal/PortalRemovalService.cs:68-71`, `IdentityClient.cs:155-173` | CODE-ONLY |

## Differences between the yaml and the server

| Where | yaml says | Server / tests |
|---|---|---|
| `setTenantDevToolsAccessSettings` | Restricts the `User` role; `RoomAdmin` is never affected | The signature's administrator flag is true only for a DocSpace admin (`IdentityClient.cs:95`), so a Room admin is restricted too; `webhooks-and-api-keys` records the same for API keys |
| `createClient` | The body "must carry" a description | `description` is not in `required` of `CreateClientRequest`, and the suite's default request has none (`08_OAuth2/OAuth2TestBase.cs`, `NewClientRequest`) and is accepted |
| `ScopeResponse.group` | files, rooms, contacts, profiles or openid | `accounts:read` and `accounts:write` have group `accounts` (`SM:74`) |
| `PageableResponse` cursors | Null when the page is empty | Per the comment in `CQ/ClientQueryingTestBase.cs:39-49` they are null whenever the caller sent no cursor, also on a non-empty page; the generated models type them as non-nullable, so the typed SDK list calls throw and the tests read raw JSON |
| Route prefix | `/api/2.0/oauth2/clients` | The tests talk to the identity service directly; raw list calls use `api/2.0/clients` (`CQ/GetClientsTests.cs`, `CM/DeleteTenantClientsTests.cs:78`), a raw create uses `api/2.0/oauth2/clients` (`CM/CreateClientPermissionsTests.cs:230`). The router maps the published prefix (`../buildtools/config/nginx/onlyoffice.conf:646-647`); which prefixes the service itself answers was not traced |
| Signature transport | `x-signature` header (`generateJwtToken`), cookie (`authorizeOAuth`) | The tests send both on every call (`08_OAuth2/OAuth2TestBase.cs`, `ApplySignature`), so neither alone is test-confirmed |

## Left out

None of the 19 operations of the five tags: all are in a requirement. Not described: the 429, 405, 406, 415 and 500
answers (generic pipeline answers), the 400 for a blank id and for concurrent modification (yaml only, no observable
contract beyond the status), and the `OAuth 2.0 / Discovery` operations (not among the owned tags). Reading and changing
`settings/devtoolsaccess` is in `webhooks-and-api-keys`.

## Suspected defects

| # | Where | What the server does | Evidence | Evidence kind |
|---|---|---|---|---|
| OA1 | `updateClient` | Accepts a `name` outside 3 to 256 characters (200), while `createClient` refuses it and the yaml declares the same bound on the update | `CM/UpdateClientPermissionsTests.cs:100-134` (`Bug` 81670); `TS/clientManagement/clientManagement.permissions.spec.ts:468,498`; yaml `UpdateClientRequest` | per test comment |
| OA2 | `getPublicClientInfo` | An unknown id does not answer 404, though the yaml declares 404 | `CQ/GetPublicClientInfoTests.cs:115-130` (`Bug` 81728); `TS/clientQuerying/clientQuerying.permissions.spec.ts:287` | per test comment |
| OA3 | `getConsents` | A Guest with their own signature gets 200; the test expects 403, the yaml documents guests as allowed. The spec follows the yaml, so the test and the bug may be the wrong side | `CQ/GetConsentsTests.cs:92-107` (`Bug` 81729); `TS/clientQuerying/clientQuerying.spec.ts:612`; yaml `getConsents` | per test comment |

Not a defect, a coverage gap: no test reads a client back after `updateClient` or `changeActivation` (`CM/UpdateClientTests.cs:49-59`, `CM/ChangeActivationTests.cs:60,77`), so neither change is proven to be stored.
