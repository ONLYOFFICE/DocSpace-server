# Proposal

## Why

The client's private-rooms changes left a list of server tasks (the document "Server tasks of private rooms",
2026-11-16, read against `release/v4.1.0`). Members without keys are stuck in rooms, a refusal for keys cannot be told
from a refusal for rights, the keys of a file do not follow the room, a few operations work on encrypted content they
cannot handle, the `e2e-rooms` app switches off only the section root, and a refused private room is saved anyway. None of
it is merged yet. This change takes the items that belong to the server's own code, read against this repository at
`52c8407261`.

## What Changes

- **BREAKING** The key check of `setRoomSecurity` and the sharing operations runs only for a user who gets access to a
  private room for the first time. A member without keys can leave, be removed and get another role; the previous owner
  without keys no longer blocks `changeFileOwner`. A group invited to a private room is refused by a rule of its own.
- **BREAKING** A refusal for a missing key carries an `error.type` of its own, which no refusal for rights carries.
- **BREAKING** Every key check of one request runs before its first write: a request that names one user without keys
  stores nothing.
- A member's file keys are revoked before their access record is removed, so a removed member never keeps keys.
- **BREAKING** `getEncryptionAccess` lists the room owner always, and the creator of the file only while they are a member.
- **BREAKING** When a deleted user's rooms are reassigned, a private room goes only to someone holding keys: the user who
  started the removal, else the portal owner; otherwise it stays with the deleted user and the operation reports it.
- The Forms and form-templates areas of `getRoomsFolder` list no private room.
- **BREAKING** On an encrypted file the owner is no longer offered renaming, history, locking, copying the link, custom
  filter and the form gallery; a private room reports no AI; `updateFileComment` on an encrypted file is refused.
- **BREAKING** While the `e2e-rooms` app is off, no room list holds a private room, `createRoom` with `private:true` is
  refused, and a private room, its folders and files answer as missing; the user's own key routes stay open.
- A private room and its folders are not added to Favorites, and those marked earlier are not listed there.
- **BREAKING** The editor configuration of an encrypted file turns AI off.
- **BREAKING** `createRoom` with `private:true` on a Public or Form filling room is refused with 400 before anything is saved,
  announced or audited; `createRoomFromTemplate` with `private:true` and a private AI agent are refused the same way.
- **BREAKING** Disabling a user (`updateMember`, `updateUserStatus`, `deleteProfile`) deletes their file keys, with an audit
  record; disabling the owner of a private room is refused with 403 until the room has another owner. An account disabled
  any other way (an LDAP synchronization) loses its file keys too, shortly after, through the Files worker.
- **BREAKING** A user made a guest (`startUserTypeUpdate`) leaves every private room: their own private rooms go to the
  recipient, their file keys and key pair are deleted, and the audit trail records the deleted keys. If the recipient
  cannot open a room they would get, the call answers 403 before anything changes. The refusals about owned private
  rooms carry an `error.type` of their own.
- **BREAKING** A data migration takes the guests made before this change out of the private rooms and deletes their
  file keys and key pair, without audit records (private rooms have not been released, so the data is small).
- **BREAKING** A DocSpace admin who is a member of a private room, in any role, may change its owner with
  `changeFileOwner`; the new owner must still hold every file key. Today only the owner may, so a room whose owner a
  directory synchronization disabled, or whose owner was removed, could not get a new one.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `private-rooms`: found by `setRoomSecurity`, `setSecurityInfo`, `removeSecurityInfo`, `changeFileOwner` ("Inviting requires
  a key", "Leaving a private room revokes file keys"), `getEncryptionAccess`, `startRemove` and `deleteMember` ("Reassigning
  a deleted user's rooms ignores file keys"), `getRoomsFolder`, `getRootFolders`, `getPrivateRoomsFolder` ("The section
  follows the app"), `createRoom`, `createRoomFromTemplate` ("Room types that can be private", "A private room cannot be
  duplicated or made a template", "Settings that need the content in the clear"), `updateFileComment` ("Operations that need open content are refused"), `getFileInfo`,
  `getRoomInfo`, `addFavorites`, `getFavoritesFolder`, `openEditFile`, `updateMember`, `updateUserStatus`, `deleteProfile`.

`people-and-groups` ("Disabling and re-enabling accounts", "Starting a data removal"), `file-sharing` ("The Favorites
section") and `form-filling-workflow` ("Forms section and form templates") keep their texts: their scenarios name no private
room, and the new rules for private rooms are added to `private-rooms`.

## Breaks current behaviour

- `private-rooms`, "Inviting requires a key". Today every access change of a private-room member is checked for keys; after
  the change only a first access is, a group is refused by its own rule, a mixed list stores nothing, and the key refusal
  has an `error.type` of its own.
  - Web client (`../client`): its invite panel already filters out users without keys; the client change narrows the text
    of `Common:EncryptedAccessChangeRefused` once the type exists.
  - SDKs and the OpenAPI document: the 403 text of `setRoomSecurity` changes; no schema changes.
  - Webhooks, socket events: a refused request now sends none, where today the earlier invitees got their events.
  - Backup and restore, migration, stored data: none.
- `private-rooms`, "Leaving a private room revokes file keys". The revocation moves before the removal of the access record;
  a failed revocation now leaves the member in the room with their keys, instead of out of the room with their keys.
  The role checks of the request run before the revocation too; a write that fails after it leaves the member in
  the room without keys, until a member issues them again. Callers see no difference on success.
- `private-rooms`, "Reassigning a deleted user's rooms ignores file keys", renamed "A deleted user's private room goes to
  someone holding a key". A private room no longer goes to a new owner without keys; it goes to the portal owner, or
  stays with the deleted user and is named in the operation's result.
  - Web client and SDKs: `getRemoveProgress` and `getReassignProgress` report the rooms left behind in a new field,
    `keptPrivateRoomIds`. The same rule applies to a data transfer (`startReassign`), which moved every room as well.
  - Stored data: rooms reassigned before the change stay where they are.
- `private-rooms`, "Room types that can be private". A Public or Form filling room with `private:true` answers 400 instead of
  403, and no room, audit record or `RoomCreated` webhook is left. A private AI agent, accepted today, answers 400.
  - Web client: `createPrivateRoomHandlers` mirror the code; the client offers neither type as private.
  - Webhooks: no `RoomCreated` for a refused room.
  - Stored data: rooms saved by the old refusal stay; the change does not clean them up.
- `private-rooms`, "A private room cannot be duplicated or made a template". `createRoomFromTemplate` with `private:true`
  answers 400 before the operation starts, where today it creates the room.
- `private-rooms`, "Settings that need the content in the clear". The sentence on `createRoomFromTemplate` with a template
  that has indexing changes with it: the request answers 400 instead of failing in `getRoomCreatingStatus`.
  - Web client: it reads the creation status today; it gets the 400 earlier. No other caller is affected.
- `private-rooms`, "Operations that need open content are refused". `updateFileComment` on an encrypted file answers 403 for
  the owner too; the `security` of an encrypted file reports Rename, ReadHistory, EditHistory, Lock, CopyLink, CustomFilter
  and SubmitToFormGallery as false, and the room's `security.UseAi` (`CanUseAi`) as false.
  - Web client: it already hides these actions for encrypted files; the client change drops its own checks after this.
- `private-rooms`, "The section follows the app". With the app off, room lists, room creation and direct reads of private
  entries change from served to refused; turning the app on serves everything as before. Nothing is deleted.
- `people-and-groups`, "Disabling and re-enabling accounts" (no text change). Disabling a member now deletes their file keys,
  and disabling the owner of a private room answers 403, with a message naming the rooms. A re-enabled user keeps rooms and key pair, but gets file keys only
  when a member issues them again.
- `private-rooms`, "Disabling a user deletes their file keys" and "The owner of a private room cannot be disabled" (added
  by this change) and the new "A member DocSpace admin may hand a private room over". `changeFileOwner` on a private room
  changes from 403 to allowed for a member DocSpace admin.
  - Web client: not checked. The room's `security.ChangeOwner` flag, which the same check fills, becomes true for a
    member DocSpace admin.
  - SDKs and the OpenAPI document: the remarks of `changeFileOwner` and the text of `keptPrivateRoomIds` change.
  - Event bus: a new event, `UserDisabledIntegrationEvent`, from every service that disables an account to the Files worker.
- `file-sharing`, "The Favorites section" (no text change). `addFavorites` skips a private room and its folders, and
  `getFavoritesFolder` no longer lists those marked earlier. A room listed by its tag in Favorites or Recent now carries
  its room settings, which that listing used to leave empty.

## Deviations

- PR2 ("A private room or its subfolder can be added to Favorites") is fixed by this change.
- PR6 (titles in letters in the default culture) is kept: translators add the resources.
- PR7 (a member left in a private room without file keys when the access record fails after the revocation) is added
  by this change, at the client's request of 2026-11-23: the order of decision 5 chooses that state over a former
  member who keeps keys.

## Out of scope

- The title of the private rooms root in other languages: the code already uses the caller's culture, only the
  translations are missing; translators add them, as for the letter titles.
- A portal setting for private rooms as a whole (`private-rooms-release-gate` 1.13); the setting for links of private items
  is in `private-rooms-external-links`.
- Whether rooms already saved by the old refusals, and favourites marked before, are cleaned up.
- The guest invitation to AI projects: the server already refuses it (`AiProjectGuestInviteTests`).
- The client code; it follows in `../client`.

## Impact

- `products/ASC.Files/Core/Utils/FileSharing.cs` (key check, validation before writes, order of revocation),
  `common/ASC.FederatedLogin/LoginProviders/EncryptionLoginProvider.cs` and the new
  `common/ASC.FederatedLogin/LoginProviders/EncryptionKeyNotSetException.cs` (the refusal type),
  `products/ASC.Files/Core/Core/Entries/EncryptionKeyPairDto.cs`.
- `products/ASC.Files/Core/Core/FileStorageService.cs` (`ReassignRoomsAsync`, `CreateRoomAsync`, `CreateRoomFromTemplateAsync`,
  `UpdateCommentAsync`, `AddToFavoritesAsync`), `common/ASC.Data.Reassigns/RemoveProgressItem.cs`.
- `products/ASC.Files/Core/Core/Security/FileSecurity.cs`, `products/ASC.Files/Core/Core/VirtualRooms/SearchArea.cs`,
  `products/ASC.Files/Core/Utils/EntryManager.cs`, the `e2e-rooms` check now in `products/ASC.Files/Server/Api/FoldersController.cs`.
- `products/ASC.Files/Core/Services/DocumentService/Configuration.cs` (AI in the editor).
- `products/ASC.People/Server/Api/UserController.cs` (disabling); `common/ASC.Core.Common/Context/Impl/UserManager.cs` and
  `common/ASC.Core.Common/Users/UserDisabledIntegrationEvent.cs` (the announcement),
  `products/ASC.Files/Worker/IntegrationEvents/EventHandling/UserDisabledIntegrationEventHandler.cs` (its handler).
- `common/ASC.Data.Reassigns/UpdateUserTypeProgressItem.cs` and `products/ASC.Files/Core/Helpers/PrivateRoomAccountHelper.cs`
  (a user made a guest, its key deletion records); a data migration in `migrations/mysql/SaaS` for the guests made before
  this change (decision 19).
- The OpenAPI documents and the SDKs; the web client after this lands.
