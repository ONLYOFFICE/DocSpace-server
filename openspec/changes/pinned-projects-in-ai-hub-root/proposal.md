# Proposal

## Why

The web client shows the AI projects a member pinned in the side menu, next to the tree of sections it builds from
`getRootFolders` (`GET api/2.0/files/@root?filterType=2&count=1`). Today that call cannot serve them: the AI rooms
section in it carries the first `count` rooms of the section, of every type, and a second request per menu would be
needed.

## What Changes

- **BREAKING** In the answer of `getRootFolders`, the AI rooms section (`current.rootFolderType` of the AI rooms
  root) carries in `folders` the AI projects the caller pinned and can see, newest pin first, and nothing else:
  no Instructions or Knowledge room, pinned or not, and no unpinned project. `total` and `count` are the number of
  those projects, `startIndex` is 0.
- `count`, `startIndex`, `filterType`, `filterValue`, `userIdOrGroupId` and the sort parameters of the request do
  not change that section. The other sections of the answer keep the current behaviour.
- A project is in the list when the caller pinned it and the caller is a DocSpace admin, its creator, or holds a
  share record of their own on it, as in the listing of the section (`getRoomsFolder` with `searchArea=AiHub`),
  except that a share held through a group does not count. A pin on a project the caller can no longer see is not
  listed and is not removed.
- `getRoomsFolder`, `getFolderByFolderId` with the AI rooms root, `pinRoom` and `unpinRoom` do not change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: found by `getRootFolders` (`GET api/2.0/files/@root`) together with the requirement "The AI rooms
  section" and the pin operations `pinRoom` / `unpinRoom` (`PUT api/2.0/files/rooms/{id}/pin`, `/unpin`). One
  requirement is added: the AI rooms section of the root folders lists the pinned projects. "The AI rooms section"
  and the pinning requirements of `room-types` ("Pinning rooms", "Rooms of the AI rooms section") stay as they are.

## Breaks current behaviour

No requirement is modified or removed. No spec describes what `getRootFolders` returns inside a section: the
requirements naming it (`file-sharing`, "Forms root folder" of `room-types` and `form-filling-workflow`, the private
rooms section of `private-rooms`) speak of which sections it lists and of their ids. "Rooms of the AI rooms
section" says the rooms are listed with `getRoomsFolder` `searchArea=AiHub`, which stays true. "Pinning rooms"
speaks of the room list, where pinned rooms stay on top; the list of `getRoomsFolder` keeps that.

What changes for a caller is the contract of `getRootFolders` written in its description ("The filters, `count`,
and `startIndex` apply to each section"): for the AI rooms section it no longer holds, and `folders`, `total`,
`count` and `startIndex` of that section change as listed above. Who is affected:

- the web client (`../client`): `getFoldersTree` reads `current`, `new` and `pathParts` of each section and not
  `folders` or `total`, so nothing breaks; the side menu reads the pinned projects from `folders` once the client
  picks the feature up;
- the published SDKs and the OpenAPI document: no schema changes; the description of `getRootFolders` changes and
  reaches the SDK doc comments when the documents are regenerated;
- webhooks: none raised or changed;
- socket events: none added (a decision of this change). The list changes when a project is pinned or unpinned,
  for which `pinRoom` and `unpinRoom` already send `update-folder` for the room, and when a project is renamed or
  deleted or the caller loses access to it, which the client learns from the events those operations send today;
- backup and restore, the portal migration and existing stored data: no schema change, nothing new is stored;
  pins stored before the change are listed as they are.

No row of `openspec/deviations.md` is touched. The open defect of `ai-agents` "Members of the AI rooms" (a DocSpace
admin outside a project reads it, `AiRoomOutsiderAccessTests`) is kept: the list follows the listing of the
section, where such an admin sees every project.

## Impact

- **Files service**: `getRootFolders` asks for the pinned-projects form of the AI rooms section; the listing path
  of the section (`FolderContentDtoHelper` → `FileStorageService.GetFolderItemsAsync` → `EntryManager`) gets a
  branch that reads the projects from the caller's pins instead of every room of the section; one precompiled
  query reads the pinned projects with the caller's own share record. The rights of each listed project are
  computed as for any listed room.
- **OpenAPI**: the description of `getRootFolders`.
