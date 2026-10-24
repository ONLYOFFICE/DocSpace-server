# invitations — verification

One row per requirement scenario of `spec.md`. Status: TEST-CONFIRMED — an integration test asserts it;
CODE-ONLY — read from the server code and/or the OpenAPI document, no test covers it; NOTES-ONLY — only
the release notes say it (none of the scenarios is in this state; everything that stayed notes-only was
left out, see the last section). Paths are relative to the repo root; `yaml` means
`sdk/docspace-api-spec/docspace-backend.yaml`.

Abbreviations: PT = `products/ASC.People/Tests/Tests`, FT = `products/ASC.Files/Tests/Tests`,
WT = `web/ASC.Web.Api.Tests/Tests`, UC = `products/ASC.People/Server/Api/UserController.cs`,
FS = `products/ASC.Files/Core/Utils/FileSharing.cs`, IS = `products/ASC.Files/Core/Core/VirtualRooms/InvitationService.cs`,
IV = `common/ASC.Api.Core/Security/InvitationValidator.cs`, PC = `web/ASC.Web.Api/Api/PortalController.cs`,
VRC = `products/ASC.Files/Server/Api/VirtualRoomsController.cs`.

## Inviting members to the portal by email

| Claim | Source | Status |
|---|---|---|
| Allowed inviter creates a pending member (display name = email, id, no avatar, not anonymous) | PT/01_Profiles/InviteUsersTests.cs:56-72 | TEST-CONFIRMED |
| … with status Pending and activation Pending | PT/01_Profiles/ResendActivationTests.cs:70-71; `web/ASC.Web.Core/Users/UserManagerWrapper.cs:101-130` | TEST-CONFIRMED |
| Guest type refused for everyone (403 "Access denied") | PT/01_Profiles/InviteUsersTests.cs:81-98 | TEST-CONFIRMED |
| Equal or higher privilege refused (403), bug 79500 | PT/01_Profiles/InviteUsersTests.cs:112-124 | TEST-CONFIRMED |
| Overlong address 400 "maximum length of '255'", bug 79527 | PT/01_Profiles/InviteUsersTests.cs:129-138 | TEST-CONFIRMED |
| Anonymous 401 | PT/01_Profiles/InviteUsersTests.cs:141-150 | TEST-CONFIRMED |
| Existing address: related to inviter, type raised, not mailed | UC:519-537; yaml `inviteUsers` description | CODE-ONLY |
| Disabled account → 400 | UC:521-524; yaml `inviteUsers` 400 | CODE-ONLY |
| Not enough paid seats → 402 before anything is sent | UC:494-505; yaml `inviteUsers` 402 | CODE-ONLY |
| Answer lists every pending profile the caller may see | UC:552-563; yaml description | CODE-ONLY |

## Adding a member directly

| Claim | Source | Status |
|---|---|---|
| Member created with the flags of the requested type | PT/01_Profiles/AddMemberTests.cs:55-85 | TEST-CONFIRMED |
| Created-by is the caller, bug 80020 | PT/01_Profiles/AddMemberTests.cs:90-110 | TEST-CONFIRMED |
| Directly added member is Active | PT/03_UserStatus/UserStatusGetTests.cs:43-69; `UserManagerWrapper.cs:163-190` (activation Pending, status default Active) | TEST-CONFIRMED |
| Guest type refused (403) | PT/01_Profiles/AddMemberTests.cs:290-308 | TEST-CONFIRMED |
| Equal or higher type refused (403) | PT/01_Profiles/AddMemberTests.cs:153-216 | TEST-CONFIRMED |
| Field length validation 400 | PT/01_Profiles/AddMemberTests.cs:113-150 | TEST-CONFIRMED |
| Link-local avatar URL refused (403), bug 81491 | PT/01_Profiles/AddMemberTests.cs:246-263 | TEST-CONFIRMED |
| No free paid seat → created as User | UC:395-401; yaml `addMember` description | CODE-ONLY |
| Guests are created through hidden `POST /people/active`, activated | UC:144-230 (`IgnoreApi`); `common/Tests/ASC.Tests.Common/Data/Invitations.cs:90-128`; PT/00_Regressions/GuestTest.cs:39-53 | TEST-CONFIRMED |

## Account statuses Pending invite, Active and Disabled

| Claim | Source | Status |
|---|---|---|
| Statuses Active / Terminated / Pending, activation separate | `common/ASC.Core.Common/Core/EmployeeStatus.cs`, `EmployeeActivationStatus.cs`; release notes v2.6 (Accounts filter) | CODE-ONLY |
| Invited by email is Pending / Pending | PT/01_Profiles/ResendActivationTests.cs:70-71 | TEST-CONFIRMED |
| Disabling all types, Terminated filter lists them | PT/03_UserStatus/UserStatusUpdateTests.cs:43-73; UserStatusGetTests.cs:72-103 | TEST-CONFIRMED |
| Re-enabling restores Active and flags | PT/03_UserStatus/UserStatusUpdateTests.cs:76-108 | TEST-CONFIRMED |
| Disabled account in a room invitation is skipped silently (empty members), bug 79020 | FT/03_Rooms/Permissions/RoomSharePermissionsTests.cs:116-139; FS:131-134 | TEST-CONFIRMED |

## Resending the portal invitation or activation email

| Claim | Source | Status |
|---|---|---|
| Allowed inviter resends; member returned Pending / Pending | PT/01_Profiles/ResendActivationTests.cs:52-72 | TEST-CONFIRMED |
| Guest resends for another account → 403 | PT/01_Profiles/ResendActivationTests.cs:75-91 | TEST-CONFIRMED |
| User resends for another account → 403, bug 79545 | PT/01_Profiles/ResendActivationTests.cs:96-112 | TEST-CONFIRMED |
| Anonymous 401 | PT/01_Profiles/ResendActivationTests.cs:115-125 | TEST-CONFIRMED |
| Skipped accounts and resendAll: active / disabled skipped; activation instructions for non-confirmed; new link each time; resendAll admin-only; own id only for User/Guest | UC:1646-1760; yaml `resendUserInvites` | CODE-ONLY |
| Throttling 5 per 15 minutes → 429 | UC `[EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]`; yaml `resendUserInvites` 429 header text | CODE-ONLY |

## Portal invitation links per user type

| Claim | Source | Status |
|---|---|---|
| Create for a type the caller may invite (id, type, URL) | WT/04_Portal/Users/InvitationLinkCreateTests.cs:52-67 | TEST-CONFIRMED |
| One link per type → 400 | PC:283-291 (`ArgumentException`); yaml `createInvitationLink` 400 | CODE-ONLY |
| Link shared between admins (read by DocSpace admin / Room admin, created by owner) | WT/04_Portal/Users/InvitationLinkGetTests.cs:52-66 | TEST-CONFIRMED |
| Any allowed role deletes the owner's User link | WT/04_Portal/Users/InvitationLinkDeleteTests.cs:72-85 | TEST-CONFIRMED |
| Type above the caller's reach → 403 (create) | WT/04_Portal/Users/InvitationLinkCreateTests.cs:145-159 | TEST-CONFIRMED |
| … (read) | WT/04_Portal/Users/InvitationLinkGetTests.cs:100-113 | TEST-CONFIRMED |
| … (delete) | WT/04_Portal/Users/InvitationLinkDeleteTests.cs:120-155 | TEST-CONFIRMED |
| … (update, User / Guest) | WT/04_Portal/Users/InvitationLinkUpdateTests.cs:149-164 | TEST-CONFIRMED |
| Guest type → 400 naming `EmployeeType` (create, read) | InvitationLinkCreateTests.cs:121-135; InvitationLinkGetTests.cs:83-92 | TEST-CONFIRMED |
| `maxUseCount` 1001 → 400 (create, update); past expiration → 400 (create) | InvitationLinkCreateTests.cs:162-190; InvitationLinkUpdateTests.cs:102-115; PC update path check | TEST-CONFIRMED (past expiration on update: CODE-ONLY, PC:410-418) |
| Limits stored (maxUseCount 1000 / currentUseCount 0; future expiration not expired) | InvitationLinkCreateTests.cs:70-98 | TEST-CONFIRMED |
| Updating maxUseCount / expiration, own link by DocSpace admin / Room admin | InvitationLinkUpdateTests.cs:46-99 | TEST-CONFIRMED |
| Unknown link → 404 (update, delete) | InvitationLinkUpdateTests.cs:118-128; InvitationLinkDeleteTests.cs:88-98 | TEST-CONFIRMED |
| Deleted link is gone (second delete 404) | InvitationLinkDeleteTests.cs:48-67 | TEST-CONFIRMED |
| Paid role with exhausted quota → 402, bug 81564 | InvitationLinkGetTests.cs:122-150 (skipped when the quota is unlimited, which is what the integration AppHost registers); PC:265-268, 350-353 (`countPaidUserChecker.CheckAppend`) | CODE-ONLY (test exists but self-skips on the test portal) |
| Anonymous 401 (create, read, update, delete) | InvitationLinkCreateTests.cs:101-114; GetTests.cs:69-80; UpdateTests.cs:131-144; DeleteTests.cs:101-114 | TEST-CONFIRMED |
| Link URL carries `emplType`; use counted on registration; use limit → QuotaFailed; expired → Expired | `web/ASC.Web.Api/ApiModels/ResponseDto/InvitationLinkDto.cs:125`; UC:383-386; IV:191-218 | CODE-ONLY |

## Legacy invitation link endpoint

| Claim | Source | Status |
|---|---|---|
| Allowed caller gets a URL | WT/04_Portal/Users/InvitationLinkGetDeprecatedTests.cs:54-67 | TEST-CONFIRMED |
| Denied caller gets 200 with an empty body | InvitationLinkGetDeprecatedTests.cs:105-119; PC:209-214 | TEST-CONFIRMED |
| Guest type returns a URL for the owner | InvitationLinkGetDeprecatedTests.cs:70-81 | TEST-CONFIRMED |
| Anonymous 401 | InvitationLinkGetDeprecatedTests.cs:84-97 | TEST-CONFIRMED |

## Room invitation by account or group id

| Claim | Source | Status |
|---|---|---|
| Room owner adds a member; DocSpace admin in own room | FT/03_Rooms/Permissions/RoomSharePermissionsTests.cs:11-47 | TEST-CONFIRMED |
| Invited Room manager adds a member | RoomSharePermissionsTests.cs:164-184 | TEST-CONFIRMED |
| Revoked manager → 403 | RoomSharePermissionsTests.cs:187-209 | TEST-CONFIRMED |
| DocSpace admin outside the room → 403 | RoomSharePermissionsTests.cs:50-69 | TEST-CONFIRMED |
| User / Guest cannot invite (403) — referenced from `user-type-permissions` | RoomSharePermissionsTests.cs:72-112 | TEST-CONFIRMED |
| Room manager role for User / Guest → 403, not added | RoomSharePermissionsTests.cs:218-237 | TEST-CONFIRMED |
| Room admin inviting a foreign guest → empty result — referenced from `room-role-permissions` | RoomSharePermissionsTests.cs:143-161 | TEST-CONFIRMED |
| Guest to an AI agent as Room manager / Content creator → 403 | FS:255-260; release notes v3.6 | CODE-ONLY |
| Invisible subjects and notification: subject the caller cannot see is dropped | VRC `SetRoomSecurity` wrappers filter (`CanUserViewAnotherUserAsync`); yaml `setRoomSecurity` | CODE-ONLY |
| Group as subject | FS:144-157 (subject type resolved to Group), 452-458 (group members); `products/ASC.People/Tests/Tests/BaseTest.cs:175-189` helper "user or a group"; release notes v2.5 | CODE-ONLY |
| `notify` emails existing accounts that gained access, with `message` | FS:430-437 (`SendEmailRoomInviteExistingUserAsync`); yaml `RoomInvitationRequest.notify/message` | CODE-ONLY |

## Everyone system group as a share subject

| Claim | Source | Status |
|---|---|---|
| Everyone group accepted as share subject (file, read-write); link viewer still read-only, bug 78985 | FT/04_Security/FileShareTests.cs:1192-1223; `common/ASC.Core.Common/Users/Constants.cs:45-49`; `common/ASC.Common/Security/Authorizing/AuthConstants.cs:41` | TEST-CONFIRMED (file) — rooms: release notes v3.5 only, no test |

## Room invitation by email

| Claim | Source | Status |
|---|---|---|
| Unknown address → pending Guest profile created by caller, share recorded, room invitation email with `roomId` | FS:273-300, 404-418; IS:61-66; `UserManagerWrapper.cs:101-130` | CODE-ONLY |
| Existing pending account → related, access set, invitation resent | FS:277-290 | CODE-ONLY |
| Address of an existing active account → related, access set, no invitation email, existing-user notice when `notify` | FS:286-290, 430-437 | CODE-ONLY |
| Guest invitations disabled → 403 for the whole request | VRC `SetRoomSecurity` (`newGuestsInvited` / `AllowInvitingGuests`); yaml `setRoomSecurity` 403 | CODE-ONLY |
| Malformed or overlong address → 400 | `common/ASC.Api.Core/Model/EmailInvitationDto.cs:59-61`; yaml `setRoomSecurity` 400 | CODE-ONLY |
| Address rejected during profile creation → `warning`, other entries applied | FS:292-300 | CODE-ONLY |

No integration test sends an `email` entry to `PUT /files/rooms/{id}/share`; the harness creates every
member through `POST /people` or `POST /people/active`.

## Resending room invitations

| Claim | Source | Status |
|---|---|---|
| Owner resends (one, several, all) | FT/03_Rooms/Invitations/RoomInvitationResendTests.cs:60-96, 152-161 | TEST-CONFIRMED |
| Invited Room manager resends | FT/03_Rooms/Permissions/RoomResendPermissionsTests.cs:29-45 | TEST-CONFIRMED |
| Across room types (Public, Form filling, Collaboration, VDR) | RoomInvitationResendTests.cs:216-235 | TEST-CONFIRMED |
| Membership untouched; active member keeps access, appears once | RoomInvitationResendTests.cs:164-208 | TEST-CONFIRMED |
| No-op bodies (no ids, empty array) | FT/03_Rooms/Invitations/RoomInvitationTests.cs:99-118 | TEST-CONFIRMED |
| Active member no-op; outsider skipped; member + outsider batch; repeated calls; `resendAll` ignores `usersIds` | RoomInvitationResendTests.cs:74-84, 99-149 | TEST-CONFIRMED |
| Non-member DocSpace admin / Room admin → 403 | RoomResendPermissionsTests.cs:50-69 | TEST-CONFIRMED |
| Member below Room manager → 403 | RoomResendPermissionsTests.cs:119-152 | TEST-CONFIRMED |
| Anonymous 401; disabled manager 401 | RoomResendPermissionsTests.cs:72-113 | TEST-CONFIRMED |
| Malformed account id → 400 | RoomInvitationTests.cs:120-138 | TEST-CONFIRMED |
| Room id 0 / -1 / unknown / deleted → 404, bugs 81879, 81880 | RoomInvitationTests.cs:150-204 | TEST-CONFIRMED |
| Archived room → 403 | RoomInvitationTests.cs:206-223 | TEST-CONFIRMED |
| Throttling 5 per 15 minutes → 429 | VRC `[EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]` on `{id}/resend`; yaml `resendEmailInvitations` 429 header text | CODE-ONLY |

## Invitation link of a room

| Claim | Source | Status |
|---|---|---|
| Second invitation link → 403, first remains | FT/03_Rooms/Links/RoomLinkCreateTests.cs:82-104; FS:62 (`MaxInvitationLinks = 1`) | TEST-CONFIRMED |
| Recreated after deletion, exactly one | RoomLinkCreateTests.cs:107-133 | TEST-CONFIRMED |
| Accepted accesses Read / Review / Comment / Editing | RoomLinkCreateTests.cs:136-156 | TEST-CONFIRMED |
| Missing `linkType` → invitation link | RoomLinkCreateTests.cs:201-214 | TEST-CONFIRMED |
| Missing access → no link | RoomLinkCreateTests.cs:218-231 | TEST-CONFIRMED |
| Custom / Public / Collaboration / VDR lifecycle | RoomLinkCreateTests.cs:297-324 | TEST-CONFIRMED |
| Form filling room: invitation link with FillForms / ContentCreator created (200), second one 403 "exceed 1", Read 403 "role not available" | live run 2026-10-04 (scratchpad live-roles/report.md, scenario 13); the test RoomLinkCreateTests.cs:387-404 sends access Read only, so its 403 is the role check, not a ban on invitation links | TEST-CONFIRMED (Read half) / LIVE-RUN (FillForms half) |
| Not the primary link | FT/03_Rooms/Links/RoomLinkPrimaryTests.cs:118-134 | TEST-CONFIRMED |

## Following an invitation link

| Claim | Source | Status |
|---|---|---|
| Existing account not in room → added during the check, `UserExisted`, room data | IS:96-205 (`ResolveAccessAsync` → `ProcessShare`), 208-216; release notes v2.6 | CODE-ONLY |
| Existing member → `UserExisted`, no new share | IS:109-128, 208-216 | CODE-ONLY |
| Unknown person → `Ok`, email, room data; registration creates, counts use, joins room | IS:218-222; UC:280-286, 383-386, 430-433; IS:304-354 | CODE-ONLY |
| Check not Ok → `POST /people` 403 | UC:283-286; yaml `addMember` 403 | CODE-ONLY |
| Previously removed member → `UserExcluded` | IS:130-164 | CODE-ONLY |
| Use limit reached → `QuotaFailed` | IS:290-295, 419-426 | CODE-ONLY |
| Expired / Invalid (expiration, foreign key, creator disabled or removed) | IS:272-288; IV:54-69, 191-218 | CODE-ONLY |
| Paid role for User / Guest lowered to highest free role, `QuotaFailed` when none | IS:170-179, 336-343 | CODE-ONLY |
| Guest into an AI agent with a creator role → Viewer | IS:399-407 | CODE-ONLY |
| Individual link: password set → `UserExisted`; disabled or gone → `Invalid` | IV:143-150 | CODE-ONLY |
| Individual link visited too long ago → `Expired` | IV:151-160 | CODE-ONLY |
| Key from the confirmation cookie | `web/ASC.Web.Api/Api/AuthenticationController.cs:493-500` | CODE-ONLY |
| IP restriction → 403 | IS:70-73; yaml `checkConfirm` 403 | CODE-ONLY |

The yaml describes `checkConfirm` as "read-only: it neither accepts the invitation nor signs anyone in".
For a signed-in account that follows a room invitation link this is not what the server does: the check
itself writes the share (IS:181, `ProcessShare`) and the audit event. The spec follows the server.

## Invitation settings

| Claim | Source | Status |
|---|---|---|
| Defaults both on | `common/ASC.Core.Common/Tenants/TenantUserInvitationSettings.cs:43-50` | CODE-ONLY |
| Anonymous read, 304 on `If-Modified-Since` | `web/ASC.Web.Api/Api/Settings/SettingsController.cs:1569-1578`; yaml | CODE-ONLY |
| Update needs the portal-settings right → 403 | SettingsController.cs:1583-1595 | CODE-ONLY |
| Members off blocks invite by email, direct add, invitation links (CRUD), legacy link | UC:476-480, 309-320; PC:201-205, 249-253, 332-336, 388-392, 458-462 | CODE-ONLY |
| Guests off blocks email entries in a room invitation and guest creation | VRC `SetRoomSecurity`; UC:148-162, 302-307 | CODE-ONLY |
| Stored link cannot be accepted while its type is blocked | UC:300-320 (settings checked after the link resolved the type) | CODE-ONLY |

No integration test toggles the invitation settings (`web/ASC.Web.Api.Tests/Tests/02_Settings/Security`
holds password, modules and web-item security only).

## Invitation quota

| Claim | Source | Status |
|---|---|---|
| Sending reduces `invitationLimit` by one | PT/00_Regressions/InvitationLimitTest.cs:42-72; `web/ASC.Web.Core/Notify/StudioNotifyService.cs:177-180, 194+` (`ReduceLimit`) | TEST-CONFIRMED (returns early when the limit is unlimited, which is the default) |
| Quota exceeded → 400 "Invitation limit is exceeded" | InvitationLimitTest.cs:79-93; `common/ASC.Api.Core/Model/EmailInvitationDto.cs:64-88` | TEST-CONFIRMED (same caveat) |
| Following the link gives one back, capped at the limit | UC:330-332; `web/ASC.Web.Core/Users/UserInvitationLimitHelper.cs:97-124` | CODE-ONLY |
| Config keys `web:invitation-limit` (unlimited by default) and `web:invitation-limit-duration` (24 hours), exposed in settings | `web/ASC.Web.Core/SetupInfo.cs:140-141`; InvitationLimitTest.cs:47-51; release notes, technical changes v2.6 | CODE-ONLY |

## Daily email invitation rate limit

| Claim | Source | Status |
|---|---|---|
| Per-portal fixed one-day window (`core:hosting:rateLimiterOptions:maxEmailInvitationsPerDay`), each address one permit, no address not counted, 429 | `common/ASC.Api.Core/Core/BaseStartup.cs:293-360`; `RateLimiterPolicy.cs:39`; UC `[EnableRateLimiting(EmailInvitationApi)]` on `POST invite`; VRC on `PUT {id}/share`; release notes, technical changes v2.6 | CODE-ONLY |

## Paid-user limit when inviting

| Claim | Source | Status |
|---|---|---|
| Room manager for User / Guest → 403, not added | RoomSharePermissionsTests.cs:218-237; FS:262-268 (`AvailableUserAccesses`) | TEST-CONFIRMED |
| Paid type beyond the quota → 402 (email invite; link create / read) | UC:494-505; PC:265-268, 350-353; InvitationLinkGetTests.cs:122-150 (self-skips) | CODE-ONLY |
| Direct creation with no free seat → User | UC:395-401 | CODE-ONLY |

Release notes v2.0 describe a warning and a downgrade to Editor when a User is invited to a room with a
paid role; the server today refuses the request (403, role not available) and only the link-follow path
downgrades (IS:170-179). The spec follows the server.

## Sharing a guest with another manager

| Claim | Source | Status |
|---|---|---|
| Owner / DocSpace admin / own Room admin get a `/s/` link | WT/04_Portal/Guests/GuestSharingLinkTests.cs:47-130 | TEST-CONFIRMED |
| Room admin or User for the owner's guest → 403 | WT/04_Portal/Guests/GuestSharingLinkPermissionsTests.cs:48-63 | TEST-CONFIRMED |
| Target not a guest → 400; unknown → 404 | UC:964-1000 | CODE-ONLY |
| Approve: relation created, profile returned, idempotent; User / Guest → 403 | UC:1016-1060; yaml `approveGuestShareLink` | CODE-ONLY (needs a browser-set confirm cookie, see PT/11_Guests/GuestsApiTests.cs:39-41) |

## Guest relations decide whose guest it is

| Claim | Source | Status |
|---|---|---|
| Room admin removes own guests; they leave its room | PT/11_Guests/GuestsApiTests.cs:46-81 | TEST-CONFIRMED |
| Owner, DocSpace admin, User, guest, non-inviting Room admin → 403 (bug 80628) | PT/11_Guests/GuestsPermissionsTests.cs:44-137 | TEST-CONFIRMED |
| Invited by email then made a Guest: only the inviter may remove it | PT/11_Guests/GuestsPermissionsTests.cs:156-186 | TEST-CONFIRMED |

## Encrypted email in confirmation links

| Claim | Source | Status |
|---|---|---|
| Links carry `encemail`, not `email` | `web/ASC.Web.Core/Utility/CommonLinkUtility.cs:245-262`; release notes v3.5 | CODE-ONLY |
| `encemail` accepted and decrypted on check and consume | `web/ASC.Web.Api/Api/AuthenticationController.cs:501-505`; `common/ASC.Api.Core/Security/EmailValidationKeyModelHelper.cs:84, 102, 350-353`; UC:1040 | CODE-ONLY |

## Left out of the spec, and why

- Release notes v2.6, "Pending invite / Active / Disabled" filter in Accounts, and v3.0 hourglass icon for
  unconfirmed users in the invite panel — the UI filter and icon; the server side (status values, status
  filter) is in the statuses requirement.
- Release notes v3.5, "Everyone group in the invite panel" — the panel is UI; the server accepts the group
  as a share subject (tested for a file), no room test exists, so only the general requirement is kept.
- Release notes v3.7, "Invite via link restrictions" and the Contacts panel without a type combobox — the
  settings (use limit, expiration) are in the portal invitation links requirement; the panel is UI.
- Release notes v3.2, hiding the "invite" options in the Contacts context menu — UI; the server refusal
  (403) is in the invitation settings requirement.
- Release notes v2.6, "redirect into the room" after following a link — the redirect is the client's; the
  server side (share written, room id and title returned) is in the following requirement.
- Release notes v2.6.1 and v2.0, toasts and warnings on the paid-user limit — UI; the server answers
  (402, 403, downgrade on link) are in the paid-user requirement. The v2.0 downgrade-to-Editor on a room
  invitation no longer exists on the server (see above).
- Release notes v3.1, "GuestInviteForm" confirm page — the page is the client's; the two endpoints behind
  it are specified.
- Release notes v3.0, the renamed types and roles, "guests only through rooms", "Room admin invites only
  User and Guest" — already in `user-type-permissions` and `room-role-permissions`.
- `POST /api/2.0/settings/sendjoininvite` (join request from the login page, trusted domains) — not an
  invitation by a portal member; outside the capability.
- The AI-agent specific rules (v3.6) are kept only where they touch invitations (guest role in an agent).
