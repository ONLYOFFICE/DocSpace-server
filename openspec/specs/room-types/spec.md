# room-types Specification

## Purpose

Defines the room types a portal offers — Custom, Collaboration (`EditingRoom`), Public, Virtual Data Room,
Form filling and AI room — and what each type adds beyond the access matrix: the parameters a room is created
and updated with (title, tags, logo, cover, colour, quota, indexing, file lifetime, deny download, watermark,
the private flag, third-party storage backing), the Virtual Data Room features (manual order with reorder and
index export, file lifetime, deny download, watermark), room templates, room groups, duplication, pinning,
archiving, deletion, ownership transfer, the per-room storage quota and the placement of Form filling rooms in
the Forms section. Member roles, who may hold them and what they allow are specified in
`room-role-permissions`, `vdr-room-roles` and `form-filling-room-roles`; links and their rules in
`shared-links`. This specification does not restate those rules and refers to them where a room type depends
on them.

Sources: the integration tests of `products/ASC.Files/Tests/Tests/03_Rooms`, `06_Operations/Duplicate` and
`07_Settings/Quota`, the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations are named
by their `operationId`) and the product release notes (v2.0 to v4.0), which say what was shipped and when.
Where the release notes and the server disagree this specification follows the server; the differences and the
evidence for every scenario are listed in `verification.md` next to this file. HTTP status codes are stated
where the tests assert them.

## Requirements

### Requirement: Room types offered by the API

The system SHALL expose the room type as the `roomType` field of `createRoom` with the values of the `RoomType`
enum: `1` Form filling room (`FillingFormsRoom`), `2` Collaboration room (`EditingRoom`), `5` Custom room,
`6` Public room, `8` Virtual Data Room, `9` AI room. There is no separate "Private" room type: end-to-end
encryption is the boolean `private` flag of a room of any of these types. `roomType` is required, MUST be one
of the listed values, and cannot be changed after the room exists (`updateRoom` has no `roomType` property).

| Type              | Value | Primary external link on creation | Section it lives in |
|-------------------|:-----:|:---------------------------------:|---------------------|
| Form filling room | 1     | yes                               | Forms (`searchArea=Forms`, root `FolderType.Forms`) |
| Collaboration     | 2     | no                                | Rooms (`searchArea=Active`, root `FolderType.VirtualRooms`) |
| Custom            | 5     | no                                | Rooms |
| Public            | 6     | yes                               | Rooms |
| Virtual Data Room | 8     | no                                | Rooms |
| AI room           | 9     | no                                | AI Agents (`searchArea=AiAgents`) |

#### Scenario: Every type is created with its own value

- **WHEN** `createRoom` is called with a title and `roomType` 1, 2, 5, 6 or 8
- **THEN** the room is created (200) and `getRoomInfo` returns the same `roomType`, the given title and a positive id

#### Scenario: Unknown or missing room type is rejected

- **WHEN** `createRoom` is called with `roomType` 99999, with `roomType` null or without `roomType`
- **THEN** the system rejects the request (400)

#### Scenario: AI room carries chat settings, other types refuse them

- **WHEN** an AI room (`roomType` 9) is created and later updated with `chatSettings`
- **THEN** the room carries chat settings from creation and the update changes them
- **AND** sending `chatSettings` to `updateRoom` of a Custom room is rejected (400) and the room keeps no chat settings

#### Scenario: Room info exposes only the flags that were set

- **WHEN** a Custom, Public, Collaboration or Form filling room is created without options
- **THEN** `getRoomInfo` reports `indexing` and `denyDownload` as not true, no lifetime value and no watermark text
- **AND** a Virtual Data Room created with `indexing:true, denyDownload:true` reports both flags true

### Requirement: Who may create a room

The system SHALL let the DocSpace Owner, DocSpace admins and Room admins create rooms with `createRoom` and
`createRoomThirdParty`. Users and Guests SHALL be refused (403, "You don't have enough permission to create").
An unauthenticated or terminated caller SHALL get 401. A `share` list in the creation body is not implemented:
a non-empty value is rejected (400) and members are invited afterwards with `setRoomSecurity`.

#### Scenario: Owner and DocSpace admin create a room

- **WHEN** the Owner or a DocSpace admin calls `createRoom`
- **THEN** the room is created with a positive id

#### Scenario: User and Guest cannot create a room

- **WHEN** a User or a Guest calls `createRoom`, with or without `private:true`
- **THEN** the system rejects the request (403)

#### Scenario: Anonymous and terminated callers

- **WHEN** an unauthenticated caller or a terminated account calls `createRoom`
- **THEN** the system rejects the request (401)

#### Scenario: Share list on creation is rejected

- **WHEN** `createRoom` is called with a non-empty `share` list
- **THEN** the system rejects the request (400)

### Requirement: Room title

The system SHALL require a title of 1 to 170 characters after trimming. Characters a folder name cannot hold
(`"`, `<`, `>`, `/`, `\` and characters outside the Basic Multilingual Plane such as emoji) SHALL be replaced
with underscores; letters of any script, `&`, `'` and `;` SHALL be kept. Titles are not unique: two rooms may
carry the same title and are told apart by id. On `updateRoom` a missing, null, empty or whitespace-only title
SHALL leave the current title unchanged, while a title over 170 characters SHALL be rejected (400).

#### Scenario: Blank or missing title on creation

- **WHEN** `createRoom` is called without `title`, with `title` null, `""` or `"   "`
- **THEN** the system rejects the request (400)

#### Scenario: Over-long title

- **WHEN** `createRoom` or `updateRoom` is called with a title of 1000 characters, or `updateRoom` with 171
- **THEN** the system rejects the request (400) and an existing room keeps its title
- **AND** a title of exactly 170 characters is accepted

#### Scenario: Forbidden characters are replaced

- **WHEN** a room is created with the title `Room "with" \slashes & <html> tags`
- **THEN** the stored title is `Room _with_ _slashes & _html_ tags`
- **AND** `Room 🎉 测试 ファイル` is stored as `Room __ 测试 ファイル` and `'; DROP TABLE rooms; --` is stored unchanged

#### Scenario: Duplicate titles are allowed

- **WHEN** two rooms are created with the same title
- **THEN** both are created with different ids and the same title

#### Scenario: Blank title on update is a no-op

- **WHEN** `updateRoom` is called with `title` `""`, `"   "` or null, or with an empty body
- **THEN** the response and `getRoomInfo` return the original title

### Requirement: Room tags

The system SHALL attach the tags named in `tags` to the room. Names missing from the portal tag catalogue
(`getRoomTagsInfo`) are added to it; duplicates within one request are stored once. On `updateRoom` the `tags`
list SHALL replace the whole tag set, an empty list clears it and a null list leaves it unchanged. A tag name
over 255 characters SHALL be rejected (400); characters that a title cannot hold are stored verbatim in a tag.
When a room is deleted, a tag used by no other room SHALL be removed from the catalogue and a tag still used by
another room SHALL stay. Tags are also attached and detached with `addRoomTags` and `deleteRoomTags` and
listed in `getRoomsFolder` through the `tags` filter (a JSON array of names, any match) and `withoutTags`.

#### Scenario: Tags given on creation

- **WHEN** a room is created with `tags: ["Autotest Alpha", "Autotest Beta"]`
- **THEN** the room carries both tags and both names appear in `getRoomTagsInfo`
- **AND** `tags: ["autotest-dup", "autotest-dup", "autotest-dup"]` yields one tag; 50 tags are accepted; `tags: null` yields none

#### Scenario: Update replaces the tag set

- **WHEN** a room with tag A is updated with `tags: ["B", "C"]`, then `["D"]`, then `[]`
- **THEN** the room carries B and C, then only D, then no tags

#### Scenario: Tag filter must be JSON

- **WHEN** `getRoomsFolder` is called with `tags=AutotestFilterTag` or `tags=A,B` instead of a JSON array
- **THEN** the system rejects the request (400)

#### Scenario: Single-use tag disappears with its room

- **WHEN** the only room carrying a tag is deleted
- **THEN** the tag is no longer in `getRoomTagsInfo`; a tag shared with another room stays

### Requirement: Room colour, cover and logo

The system SHALL draw a room without a logo with a colour and an optional cover. `color` MUST be six
hexadecimal digits without `#` (case-insensitive); an empty value restores a random default colour of the
room, null leaves it unchanged, and any other value is rejected (400). `cover` MUST be an identifier from
`getRoomCovers`; an empty value removes the cover and an unknown value is rejected (400). The gallery of
covers is the same for every role and language and contains `schedule`. A logo is an image uploaded with
`uploadRoomLogo` beforehand and applied with `createRoomLogo` (or the `logo` field of `createRoom`), and is
reported in `logo.original/large/medium/small`; applying a logo to an archived room (403) or a deleted room
(404) is refused, and so is a `tmpFile` the caller did not upload (403).

#### Scenario: Colour set on creation and update

- **WHEN** a room is created with `color: "FF5733"` and later updated to `"00AA00"`, then `""`, then null
- **THEN** `logo.color` is `FF5733`, then `00AA00`, then a six-digit colour different from `00AA00`, then unchanged

#### Scenario: Invalid colour

- **WHEN** a room is created or updated with `color` `not-a-color`, `#FF5733`, `ZZZZZZ` or `123`
- **THEN** the system rejects the request (400) and the room keeps its colour

#### Scenario: Cover set, replaced and cleared

- **WHEN** a room is updated with a cover id, then another, then `""`
- **THEN** `logo.cover.id` is the first id, then the second, then null
- **AND** a cover id that is not in the gallery is rejected (400)

#### Scenario: Logo applied from an uploaded image

- **WHEN** an image is uploaded with `uploadRoomLogo` and applied with `createRoomLogo` to a room
- **THEN** the room reports all four logo URLs and `getRoomInfo` returns the same paths; applying another image changes the original URL

### Requirement: Data-protection settings of a room

The system SHALL accept `indexing`, `denyDownload`, `lifetime` and `watermark` on `createRoom` and `updateRoom`.
They were shipped for the Virtual Data Room (v3.0) and the client offers them there, but the API SHALL accept
`indexing` and `denyDownload` on a room of any type. Defaults are `indexing:false`, `denyDownload:false`, no
lifetime, no watermark.

| Setting        | Shape                                                        | Removal |
|----------------|--------------------------------------------------------------|---------|
| `indexing`     | boolean; the contents keep a manual order (see reorder)     | `false` |
| `denyDownload` | boolean; members without editing rights cannot download or print | `false` |
| `lifetime`     | `{enabled, value 1-999, period 0 Day / 1 Month / 2 Year, deletePermanently}` | `enabled:false`, reported as null |
| `watermark`    | `{enabled, text up to 255, additions bitmask 1 UserName / 2 UserEmail / 4 UserIpAdress / 8 CurrentDate / 16 RoomName, rotate, imageScale, imageUrl}` | `enabled:false`, reported as null |

#### Scenario: Settings given on creation of a Virtual Data Room

- **WHEN** a VDR is created with `indexing:true`, `denyDownload:true`, `lifetime {Day, 30, deletePermanently:false}` and `watermark {enabled, text "Confidential", rotate -45}`
- **THEN** `getRoomInfo` reports `indexing` true, `denyDownload` true, the lifetime period Day, value 30, deletePermanently false, and the watermark text

#### Scenario: All settings updated at once and toggled off

- **WHEN** a VDR is updated with new title, `indexing:true`, `denyDownload:true`, a lifetime, a watermark with additions UserName and `color`
- **THEN** the response and `getRoomInfo` reflect every value
- **AND** a later update with `indexing:false` and `denyDownload:false` reports both false, `watermark {enabled:false}` makes the watermark null and `lifetime {enabled:false}` makes the lifetime null

#### Scenario: Invalid lifetime

- **WHEN** a room is created with a lifetime `period` of 999 or updated with a lifetime `value` of -5
- **THEN** the system rejects the request (400)

#### Scenario: Settings accepted on a Custom room

- **WHEN** a Custom room is updated with `indexing:true`, or with `denyDownload:true` together with other fields
- **THEN** `getRoomInfo` reports the flag true

#### Scenario: Wrongly typed field

- **WHEN** `updateRoom` receives `{"denyDownload":"yes"}`, `{"indexing":"no"}`, `{"title":123}`, `{"tags":"notarray"}`, `{"color":123}` or a property the update does not define
- **THEN** the system rejects the request (400) and the room is unchanged

### Requirement: Manual order of an indexed room and reorder

When `indexing` is on, every file and folder of the room SHALL carry an `order` that listings follow and that
`setFileOrder`/`setFolderOrder` change. `reorderRoom` SHALL renumber the direct children of the room — folders
and files in one sequence — to a dense 1..N keeping their relative order; it SHALL NOT touch nested folders'
contents, nor delete, rename, duplicate or move anything, and running it twice gives the same result. It
answers 404 for an unknown or deleted room and 403 for an archived one, and runs without error on a room whose
`indexing` is off. Only the Room Owner, a member with the Room manager role and a DocSpace admin invited as
such may reorder; a DocSpace admin or Room admin who is not a member and any invited User or Guest get 403,
anonymous 401.

#### Scenario: Gaps are compacted

- **WHEN** folders A, B, C of a VDR carry orders 10, 50, 30 and `reorderRoom` is called
- **THEN** the orders become A=1, C=2, B=3; orders 100, 5000, 999999 become 1, 2, 3 and three folders all at 5 become 1, 2, 3 in some order

#### Scenario: Files and folders share one sequence

- **WHEN** a folder at order 20 and a file at order 90 are reordered
- **THEN** the union of their orders is exactly {1, 2}
- **AND** a file inside a nested folder is still there afterwards

#### Scenario: Reorder of an archived or missing room

- **WHEN** `reorderRoom` is called on an archived room, or with id 0, -1, 999999999 or a deleted room's id
- **THEN** the system answers 403 for the archived room and 404 otherwise

#### Scenario: Reorder permissions

- **WHEN** a Room admin invited as Room manager reorders a VDR
- **THEN** the room is reordered
- **AND** the same Room admin invited with any other VDR role, an uninvited DocSpace admin, an invited User or Guest get 403

### Requirement: Index export of a Virtual Data Room

The system SHALL start a background export of the room index with `startRoomIndexExport`, report it with
`getRoomIndexExport` (null while nothing runs; `isCompleted`, `error`, `resultFileId` once done) and cancel it
with `terminateRoomIndexExport`. The export SHALL be refused for an archived room (403), for a room that is
not an indexed room or whose caller is neither its Room manager nor a portal administrator (403), and SHALL
answer 404 for id 0, -1 or a missing room. An unarchived VDR can be exported again.

#### Scenario: Owner or DocSpace admin starts the export

- **WHEN** the Owner, or a DocSpace admin who was never invited, calls `startRoomIndexExport` on a VDR
- **THEN** a job with a non-empty id and no error is returned

#### Scenario: Members without manager rights cannot export

- **WHEN** a member invited with a VDR role below Room manager, a User who is not a member, or an anonymous caller starts the export
- **THEN** the system rejects the request (403 for members and non-members, 401 for anonymous)

#### Scenario: Archived or unknown room

- **WHEN** `startRoomIndexExport` is called on an archived VDR or with id 0 or -1
- **THEN** the system answers 403 for the archived room and 404 for the ids

### Requirement: Form filling rooms live in the Forms section

Form filling rooms (v4.0) SHALL belong to a root of their own: `getRoomsFolder` with `searchArea=Forms` and
`getFormsFolder` (root `FolderType.Forms`, value 36) list them, while `searchArea=Active` and the Virtual Rooms
root (`FolderType.VirtualRooms`, value 14) SHALL NOT list them even when the `type` filter asks for them. Their
content SHALL be counted in `formsUsedSpace` of the files statistics, not in `roomsUsedSpace`, and SHALL move
to `archiveUsedSpace` while archived. Templates made from a Form filling room SHALL be listed under
`searchArea=FormTemplates` and not under `Templates`, and a room created from such a template SHALL appear in
the Forms section. Room groups of the Forms section are created with `searchArea=Forms`.

#### Scenario: Form filling room is listed only in Forms

- **WHEN** a Form filling room, a Custom room and a Public room are created
- **THEN** `searchArea=Forms` lists the form room only and `searchArea=Active` lists the other two only
- **AND** `getRoomsFolder` with `type=[1]` and `searchArea=Active` does not return the form room

#### Scenario: Forms root folder

- **WHEN** the root folders are listed
- **THEN** one of them has `rootFolderType` 36 (Forms), its id equals the id `getFormsFolder` returns, and browsing it by id lists form rooms only

#### Scenario: Used space is accounted to Forms

- **WHEN** a file is uploaded to a Form filling room, the room is archived, unarchived and finally deleted
- **THEN** `formsUsedSpace` grows by the file size and `roomsUsedSpace` does not change; while archived the size sits in `archiveUsedSpace`; after deletion `formsUsedSpace` is back to the previous value

#### Scenario: Form room templates have their own section

- **WHEN** a template is created from a Form filling room and another from a Custom room
- **THEN** the form template is listed under `searchArea=FormTemplates` and not under `Templates`, the custom template the other way round, and browsing the Templates root by id lists the custom template only
- **AND** a room created from the form template is listed under `Forms` and not under `Active`

#### Scenario: Form room opened through its link is listed in Forms

- **WHEN** a Room admin who is not a member resolves the primary link of a Form filling room with `getExternalShareData`
- **THEN** `getRoomInfo` reports the room as `external` for them and the room appears in their `searchArea=Forms` listing

### Requirement: Public room backed by third-party storage

The system SHALL turn a folder of a connected third-party account into a room with `createRoomThirdParty`
(v2.5). The path parameter is the folder id of the connected account, not a room id; one connected account
backs one room only; with `createAsNewFolder` the room is a new subfolder named after `title` ("Create a new
folder in the storage"), otherwise the connected folder itself becomes the room and `indexing`, `denyDownload`,
`tags` and `logo` are dropped. The room has string identifiers and reports `providerKey`/`providerId`. The
release notes restrict this to Public rooms; the API SHALL accept every room type of `RoomType` 1-8. Only the
account that connected the storage may make a room of it (403 for a User or Guest using someone else's
connection); an id that is not a connected third-party folder is refused with a 4xx. Deleting such a room
disconnects the account behind it.

#### Scenario: Connected folder becomes the room

- **WHEN** the Owner connects a Nextcloud account and calls `createRoomThirdParty` with its id and `roomType` Custom
- **THEN** the room id equals the connection id, `rootFolderType` is VirtualRooms, `providerKey` names the provider and the room is listed in `getRoomsFolder` with that `providerKey`

#### Scenario: Every room type is accepted

- **WHEN** `createRoomThirdParty` is called with `roomType` 1, 2, 5, 6 or 8
- **THEN** `getRoomInfo` reports that type

#### Scenario: One connection, one room

- **WHEN** `createRoomThirdParty` is called a second time over a folder that already backs a room
- **THEN** the system rejects the request (403)

#### Scenario: Foreign connection

- **WHEN** a User or a Guest calls `createRoomThirdParty` with the id of a connection made by the Owner
- **THEN** the system rejects the request (403)

#### Scenario: Deleting a third-party room

- **WHEN** a room created with `createAsNewFolder` is deleted with `deleteRoom`
- **THEN** the operation finishes without error, the room is gone from the listing and the connected account is removed from the third-party account list

### Requirement: A room reached through its link joins the caller's lists

Resolving the primary external link of a Public or Form filling room with `getExternalShareData` SHALL, for a
signed-in caller, put the room into the caller's own lists on the first successful resolution (v3.0 "show
public rooms shared by link in the rooms list"), and `getRoomInfo` SHALL report `external:true` for that
caller. For a visitor without an account it opens an anonymous session instead. See `shared-links` for the
link rules themselves.

#### Scenario: Member by link sees the room

- **WHEN** a portal member who was never invited resolves the room link with `getExternalShareData`
- **THEN** `getRoomInfo` reports `external:true` and the room is listed in the section of its type

### Requirement: Room templates

The system SHALL create a template from a room with `createRoomTemplate` (v3.1) as a background job whose
state is read with `getRoomTemplateCreatingStatus` (`isCompleted`, `progress`, `templateId`, `error`), and
create a room from a template with `createRoomFromTemplate`, polled with `getRoomCreatingStatus` (`roomId`).
Both statuses are per caller: another account never sees the caller's job. A template is listed with
`searchArea=Templates` and read with `getRoomInfo`; its title is sanitised like a room title and titles need
not be unique. Creation accepts `title` (required, non-blank, 400 otherwise), `color`, `cover`, `tags`,
`quota`, `public`, `share` (emails given read access) and `groups`. Template creation SHALL require that the
caller may edit the source room — its owner, a Room manager, a Room admin or DocSpace admin on their own room —
and SHALL answer 403 for an archived source, for a DocSpace admin, User or Guest with no access and for a User
invited with any non-manager role, 404 for room id 0, a missing or a deleted room, 401 for anonymous or
terminated callers.

#### Scenario: Template created from a room

- **WHEN** the Owner creates a template from a Custom room
- **THEN** the immediate answer has no error and a progress in 0..100; polling ends with `isCompleted` true and a positive, stable `templateId`; the template appears in the Templates listing
- **AND** two consecutive templates get distinct ids and two templates may share one title

#### Scenario: Template options are stored

- **WHEN** a template is created with `color`, `cover`, `tags`, `quota` (with the room quota feature on) or `public:true`
- **THEN** `getRoomInfo` of the template reflects the colour, cover, tags and `quotaLimit`, and `getPublicSettings` returns true

#### Scenario: Source room cannot be used

- **WHEN** `createRoomTemplate` names room id 0, a non-existent room or a deleted room
- **THEN** the system answers 404 and no template is created
- **AND** an archived source room answers 403

#### Scenario: Callers without edit rights on the source

- **WHEN** a DocSpace admin, User or Guest with no access to the room, or a User invited as Editor, Reviewer, Commentator or Viewer, creates a template from it
- **THEN** the system rejects the request (403) and no template with that title exists

#### Scenario: Status does not leak between users

- **WHEN** the Owner has just created a template and a DocSpace admin reads `getRoomTemplateCreatingStatus`
- **THEN** the admin's status does not carry the Owner's template id

### Requirement: Template visibility and public setting

A template SHALL be visible to its creator and to the accounts and groups it was shared with. A template with
`public:true` (or made public with `setPublicSettings`) SHALL additionally be visible to every DocSpace admin and
Room admin, and SHALL NOT be visible to Users and Guests. `getPublicSettings` answers 404 for a deleted
template, for a non-template room and for an unknown id, and 400 for id 0, -1 or a non-integer; the same
applies to `setPublicSettings`. Setting the same value twice keeps it, setting it for one template does not
change another, and the public flag survives deletion or archiving of the source room.

#### Scenario: Non-public template is hidden from an unrelated user

- **WHEN** a User who was not given access reads a non-public template with `getRoomInfo`
- **THEN** the system rejects the request (403)

#### Scenario: Public template visible to admin roles only

- **WHEN** a template is created with `public:true`
- **THEN** a DocSpace admin and a Room admin can read it, while a User and a Guest get 403

#### Scenario: Shared template

- **WHEN** a template is created with `share: [email of a DocSpace admin]`
- **THEN** that account reads the template with `getRoomInfo`

#### Scenario: Public flag toggled

- **WHEN** `setPublicSettings` is called with true, then false, then true, then false
- **THEN** `getPublicSettings` returns each value in turn; the template is still usable for `createRoomFromTemplate`

### Requirement: Creating a room from a template

`createRoomFromTemplate` SHALL create a new room whose title comes from the request (required, non-blank,
at most 170 characters, sanitised; 400 otherwise). The room SHALL inherit the `roomType`, `indexing` and
`denyDownload` of the source room and the folders and files captured in the template (nested hierarchy
included, with new ids); it SHALL NOT inherit the source's lifetime or watermark. Rooms made from one template
are independent of each other, of the template and of the source room, which may already be deleted. The
caller must be able to read the template and must be allowed to create rooms: a DocSpace admin with a public
template succeeds; a User or Guest (403), a DocSpace admin on someone else's non-public template (403), a User
with access to the source room but not the template (403), anonymous and terminated callers (401) are refused.
A template id of 0, a non-existent or a deleted template answers 404.

#### Scenario: Room created from a Custom room template

- **WHEN** the Owner calls `createRoomFromTemplate` with a template and the title "Room From Template"
- **THEN** polling ends with `isCompleted` true and a positive `roomId`; `getRoomInfo` reports that title, `roomType` Custom and the room is in the rooms listing; it can be updated, archived and deleted

#### Scenario: VDR settings partially inherited

- **WHEN** a template is made from a VDR with `indexing:true`, `denyDownload:true`, a lifetime of 30 days and a watermark "Confidential", and a room is created from it
- **THEN** the room is a VDR with `indexing` true and `denyDownload` true, no enabled lifetime and no "Confidential" watermark
- **AND** `indexing:false` and `denyDownload:false` of a source are inherited as false

#### Scenario: Content is copied with new ids

- **WHEN** the source room held a folder "Parent" with a child "Child" and a file "Source File"
- **THEN** the new room holds "Parent" with "Child" inside and a file whose title starts with "Source File", with ids different from the source; deleting the copied folder leaves the source untouched; an empty source yields an empty room

#### Scenario: Validation of the request

- **WHEN** `createRoomFromTemplate` is called without `templateId`, with `templateId` null, without `title`, with an empty, whitespace or 1000-character title
- **THEN** the system rejects the request (400) and no room is created
- **AND** `templateId` 0, 999999999 or a deleted template answers 404

### Requirement: Room groups

The system SHALL let every account group the rooms it can read into personal room groups (v3.7) with
`addRoomGroup`, `getRoomGroups`, `getRoomGroupInfo`, `updateRoomGroup`, `changeRoomGroupIcon` and
`deleteRoomGroup`. A group has a `name` (required, trimmed, non-blank, at most 128 characters; 400 otherwise,
and a name with characters outside the Basic Multilingual Plane is refused), an `icon` from the fixed set
(`star`, `heart`, `flag`, `folder`; a missing, blank, unknown or `none` icon is 400), a non-empty `rooms` list
(empty is 403; missing, null, non-array, string or fractional ids are 400; id 0 or negative is 400; no listed
room existing is 404) and a `searchArea` of `Active` (default) or `Forms`, which fixes the section the group
belongs to: a Forms group accepts Form filling rooms only and an Active group accepts the other types only (400
otherwise); `Archive`, `Any`, `Templates` and `FormTemplates` are refused (400). Groups are private to their
creator: any other role reading or updating the group gets 404 and deleting it gets 403. A room may be in
several groups, two groups may share a name, and a group keeps its rooms after `getRoomGroups` is called with
or without `includeMembers`.

#### Scenario: Group created over rooms of several types

- **WHEN** the Owner creates a group over a Custom, a Collaboration, a Public and a VDR room
- **THEN** the group has the given name, a positive id, `totalRooms` 4 equal to the length of `rooms`, the icon `star` and `searchArea` Active; `getRoomGroupInfo` and `getRoomGroups` return it
- **AND** a private (encrypted) room can be grouped as well

#### Scenario: Section of a group

- **WHEN** a group is created over a Form filling room with `searchArea=Forms`, and another over a Custom room without `searchArea`
- **THEN** the first has `searchArea` Forms and is listed by `getRoomGroups(searchArea=Forms)` only; the second has Active and is listed by `Active` and by the default call only
- **AND** a Forms group over a Custom room, an Active group over a form room, and a group with `searchArea` Archive, Any, Templates or FormTemplates are refused (400); a numeric `searchArea` value is accepted

#### Scenario: Partially valid room list

- **WHEN** a group is created over one valid room and one id that does not exist, or over a form room and a custom room with `searchArea=Forms`
- **THEN** the system answers 403 and the group is nevertheless created with the rooms it could take (one room)

#### Scenario: Rooms added and removed

- **WHEN** `updateRoomGroup` adds two rooms, re-adds a present room, removes a room, removes a non-member room, adds and removes in one request, or renames and adds at once
- **THEN** `totalRooms` follows the set exactly (duplicate or already-present rooms are not counted twice, non-members are ignored), removing every room leaves an empty group of the same section, and `searchArea` in the update body is ignored
- **AND** adding or removing an id that does not exist answers 404 and a room of the other section answers 400, leaving the group unchanged

#### Scenario: Group follows the rooms' lifecycle

- **WHEN** the only room of a group is archived, then unarchived, or a room of a group is deleted, or a room is renamed
- **THEN** the archived room leaves the group (`totalRooms` 0) and comes back on unarchive; the deleted room leaves the group; the new title shows in the group's room list
- **AND** deleting the group deletes none of its rooms

#### Scenario: Groups are per account

- **WHEN** a DocSpace admin, Room admin, User or Guest reads, renames, re-icons or deletes a group of the Owner
- **THEN** reading, renaming and re-iconing answer 404 and deleting answers 403; the group is unchanged
- **AND** each of those roles can create, read, rename, re-icon, list and delete a group of its own over a room it can read, and gets 403 creating a group over a room it cannot read

#### Scenario: Listing rooms by group

- **WHEN** `getRoomsFolder` is called with `groupId` of an Active group and `searchArea=Active`, then with `searchArea=Forms`
- **THEN** the first call returns the group's rooms and the second returns nothing

#### Scenario: Deleting a group

- **WHEN** a group is deleted, then deleted again, or ids 0, -1, 999999, `1.5` or `not-a-number` are deleted
- **THEN** the first delete succeeds and the group disappears from the list and answers 404 to `getRoomGroupInfo`; the repeat and the bad ids answer 404; `includeMembers` true and false are both accepted and a non-boolean value is 400

### Requirement: Duplicating rooms, files and folders

`duplicateBatchItems` SHALL copy each listed file or folder next to itself as a background operation (v2.6).
The copy keeps the original name with a numeric suffix so nothing is overwritten; a folder is copied with its
content. When the folder is a room, the copy SHALL carry the room's colour, cover, indexing, lifetime, quota,
watermark and deny-download settings, its logo, its custom tags and its metadata, and a Public or Form filling
copy gets a primary external link of its own; members of the original SHALL NOT be copied, and the copy counts
against the room limit of the tariff. The caller needs the rights that creating an item in that folder needs:
inside a room Room manager or Content creator; Editing or Read rights, no access at all, an archived room and
an item in Trash answer 403; an id that does not exist answers 404; an empty selection queues nothing.

#### Scenario: File duplicated inside a room

- **WHEN** the Owner, a Room admin invited as Room manager, or a User invited as Content creator duplicates a file in a Custom room
- **THEN** the room holds at least two files whose title contains the original name

#### Scenario: Rights below Content creator

- **WHEN** a User invited as Editor, a Guest invited as Viewer, or a User with no access duplicates a file
- **THEN** the system rejects the request (403); anonymous gets 401

#### Scenario: Archived room, Trash and unknown ids

- **WHEN** a file of an archived room or a file in Trash is duplicated, or file id / folder id 999999999 is given
- **THEN** the system answers 403 for the first two and 404 for the unknown ids

#### Scenario: Folder with content

- **WHEN** a folder holding a file is duplicated, or a file and a folder are duplicated in one request
- **THEN** a second folder with the nested file appears next to the original, and both the file and the folder copies appear

### Requirement: Pinning rooms

`pinRoom`/`unpinRoom` SHALL pin a room to the top of the calling account's room list and return the room with
the `pinned` flag. Pinning is personal (another member sees their own state), idempotent (a second pin or
unpin answers 200 and the room appears once), limited to 10 pinned rooms per account (v2.6; the eleventh pin
answers 403 "You can't pin a room" and unpinning one frees a slot), and AI rooms SHALL have a separate limit of
10 of their own. Pinned rooms SHALL stay above unpinned ones whatever the sort (`AZ`, `DateAndTime`), filter or
page, their mutual order is stable, and an unpinned room returns to its natural sort position. A pin SHALL
NOT survive archive and unarchive; an archived room answers 403 to pin and unpin; a deleted or unknown id
(0, -1, 999999999) answers 404 ("The required folder was not found"). Anybody who can read the room may pin it —
including a DocSpace admin who was never invited — while a User or Guest who is not a member gets 403,
anonymous and terminated callers 401. Every room type can be pinned; a Form filling room is pinned within the
Forms section.

#### Scenario: Pin limit of ten

- **WHEN** an account pins ten Custom rooms and pins an eleventh
- **THEN** the eleventh answers 403, is not pinned and the ten stay pinned
- **AND** after unpinning one of them the eleventh is pinned and exactly ten are pinned

#### Scenario: AI rooms have their own limit

- **WHEN** ten Custom rooms are pinned and an AI room is pinned
- **THEN** the AI room is pinned on top
- **AND** with ten AI rooms pinned an eleventh AI room answers 403

#### Scenario: Pinned rooms stay on top

- **WHEN** a room is pinned and the list is sorted by title ascending, by creation date descending, filtered by a title fragment or read in pages of three
- **THEN** the pinned room precedes the first unpinned one and is on the first page; pages contain no duplicates or gaps

#### Scenario: Pin is personal and does not survive archiving

- **WHEN** the Owner pins a room that a User is invited to, and the User pins it and the Owner unpins it
- **THEN** each sees only their own state
- **AND** a pinned room that is archived and unarchived is reported `pinned:false`

#### Scenario: Pin on archived, deleted or foreign room

- **WHEN** an archived room is pinned or unpinned, a deleted room is pinned, or a non-member User pins a room
- **THEN** the system answers 403, 404 and 403 respectively

#### Scenario: Unpin never changes membership

- **WHEN** a room with an invited Editor is pinned and unpinned
- **THEN** the room still exists and its member list is unchanged; unpinning a never-pinned room answers 200

### Requirement: Archiving and restoring a room

`archiveRoom` and `unarchiveRoom` SHALL move a room between the active section (`rootFolderType`
VirtualRooms or Forms) and the Archive (`rootFolderType` Archive) as background operations whose body
`{deleteAfter}` is required and must be a boolean (null or a string is 400). Only the room's owner and portal
administrators may archive or restore: the Owner archives a DocSpace admin's room, a DocSpace admin archives
their own and restores someone else's, while a Room admin, User or Guest — invited or not, with Read or Editing
rights, or removed from the room — gets 403 and anonymous 401. Archiving is idempotent; restoring a room that is
active is a no-op (200); an unknown or deleted id answers 404. An archived room SHALL keep its content, title,
type and tags, SHALL be read-only (creating a folder or file, renaming, adding tags, inviting, applying a logo,
pinning, reordering, index export, duplication and quota changes answer 403), and SHALL still report its primary
link if it is a Public room (see `shared-links`). After restoring, renaming, sharing and index export work
again. Archiving and restoring are also available to members by role as stated in `room-role-permissions`.

#### Scenario: Full cycle

- **WHEN** the Owner archives a Custom room with a file and a folder, then restores it
- **THEN** the archive operation finishes without error, the room is in `searchArea=Archive` and not in `Active`, `getRoomInfo` reports `rootFolderType` Archive, the file and folder are still there; after `unarchiveRoom` the room is back in `Active` with `rootFolderType` VirtualRooms and keeps its title, type and both tags

#### Scenario: Archived room is read-only

- **WHEN** a folder or a file is created in an archived room, the room is renamed, a tag is added, a user is invited, a logo is applied, a quota is set or reset, or a file in it is duplicated
- **THEN** the system rejects each request (403)

#### Scenario: Who may archive and restore

- **WHEN** a Room admin, User or Guest who is not invited archives or restores a room, or an invited User with Read or Editing rights restores it
- **THEN** the system rejects the request (403); an anonymous caller gets 401
- **AND** the Owner archives a room created by a DocSpace admin, and a DocSpace admin restores a room created by the Owner

#### Scenario: Idempotent and no-op cases

- **WHEN** an already archived room is archived again, a restored room is restored again, or a never-archived room is restored
- **THEN** each answers 200 and the room ends up in the expected section

#### Scenario: Unknown room and bad body

- **WHEN** archive or unarchive is called with id 999999999, a deleted room's id or a non-numeric id, or with `deleteAfter` null or a string
- **THEN** the ids answer 404 and the bad bodies 400, and an archived room stays archived

### Requirement: Deleting a room

`deleteRoom` SHALL queue a background operation (its id is returned; `getOperationStatuses` reports it
`finished` with progress 100) that removes the room with its files, logo, cover, quota, links and
single-use tags. The body `{deleteAfter}` is required but does not change the outcome: omitting the field
defaults to false, null, a string or a number is 400. Only the room's creator and portal administrators on their
own room may delete it: a User or Guest, invited or not (403 "Access denied"), a Room admin not invited or
invited with any role including Room manager (403), anonymous and terminated callers (401) are refused. An
archived room can be deleted. After deletion `getRoomInfo`, `getRoomsPrimaryExternalLink`, a second delete,
pin, cover and logo changes answer 404, `updateRoom` answers 403, and a member who could read the room before
gets 404.

#### Scenario: Owner deletes a room of every type

- **WHEN** the Owner deletes a Custom, Collaboration, Public, Form filling or VDR room
- **THEN** the call answers 200 with an operation id, the operation finishes without error, the room is gone from the listing and `getRoomInfo` answers 404

#### Scenario: Content and links go with the room

- **WHEN** a room holding a file, or a Public room with its primary link, is deleted
- **THEN** `getFileInfo` of the file and `getRoomsPrimaryExternalLink` of the room answer 404

#### Scenario: Who may delete

- **WHEN** a User or Guest (not invited, or invited with any non-manager role), or a Room admin invited with any role, deletes the Owner's room
- **THEN** the system rejects the request (403)
- **AND** a DocSpace admin deletes a room they created

#### Scenario: Invalid ids and body

- **WHEN** `deleteRoom` is called with id 0, -1, 999999999, a non-numeric id, or with `deleteAfter` null, `"x"` or 1
- **THEN** the ids answer 404 and the bodies 400; the room is not deleted

#### Scenario: Deleted room cannot be revived

- **WHEN** a deleted room is updated, archived, deleted again or used as a template source
- **THEN** `updateRoom` answers 403 and the others 404

### Requirement: Changing the room owner and leaving the room

`changeFileOwner` (`POST api/2.0/files/owner`, v2.0) SHALL hand the listed rooms over to one account, which
MUST be an active account allowed to manage rooms (Room admin, DocSpace admin or Owner); a deactivated account,
a Guest or a User is refused (403, "You cannot select this user"), and for a private room the new owner must
have encryption keys. The caller MUST be the creator of every listed room or a portal administrator. The
previous owner keeps the room as its Room manager; an item already owned by the target is returned untouched.
Leaving a room is removing oneself from the member list and is specified in `room-role-permissions` (the Owner
cannot leave).

#### Scenario: New owner must be an active account

- **WHEN** ownership is transferred to a Room admin whose account was disabled
- **THEN** the system rejects the request (403, "You cannot select this user")

#### Scenario: User cannot become the owner

- **WHEN** ownership is transferred to a User or a Guest
- **THEN** the system rejects the request (403) — see `room-role-permissions`

### Requirement: Per-room storage quota

The system SHALL support a storage limit per room (v2.5) only while the room quota feature is on
(`saveRoomQuotaSettings` with a default quota). A room may be created or updated with `quota` (bytes),
changed for several rooms with `updateRoomsQuota` (the rooms are returned with `quotaLimit` and
`isCustomQuota:true`; `0` is accepted and stored) and returned to the portal default with `resetRoomQuota`
(`isCustomQuota:false`, `quotaLimit` equal to the default). With the feature off both calls answer 403. The
calls need edit rights on each room: an archived room (403), a DocSpace admin or Room admin who is not a
member, a User (403) and anonymous callers (401) are refused; an empty room list returns an empty array.
`getRoomInfo` reports `quotaLimit`, `usedSpace` and `isCustomQuota`; a negative `quota` on creation is not
stored as given (the stored limit is 0 or more).

#### Scenario: Custom quota set and reset

- **WHEN** the feature is on with a 1 GB default and a VDR gets `updateRoomsQuota` 2 GB, then `resetRoomQuota`
- **THEN** the first answer reports `quotaLimit` 2 GB and the second 1 GB; two rooms are handled in one call

#### Scenario: Quota given on creation

- **WHEN** the feature is on and a room is created with `quota` 10 MiB
- **THEN** `getRoomInfo` reports `quotaLimit` 10 MiB

#### Scenario: Feature off, archived room, wrong caller

- **WHEN** `updateRoomsQuota` or `resetRoomQuota` is called while the feature is off, on an archived room, by a DocSpace admin, Room admin or User who was not invited, or anonymously
- **THEN** the system answers 403 (401 for anonymous)

### Requirement: Behaviour when the room quota is exceeded

Creating or uploading a file into a room SHALL be refused once the room's used space is above its limit. The
server allows one soft overrun: a file that takes the room above its limit but below twice the limit is
accepted once, and the next file is refused; a file that would take the room above twice its limit is refused
outright (an upload session answers 402 with a message naming the quota).

#### Scenario: File within the quota

- **WHEN** a VDR with a 1 GB default quota, or with a 100 KB custom quota, receives a new `.docx`
- **THEN** the file is created and listed in the room

#### Scenario: Quota smaller than the file

- **WHEN** a room has a quota of 1 byte and a file is created in it
- **THEN** the system rejects the request

#### Scenario: Soft overrun allowed once

- **WHEN** the room quota is 4 KB and a `.docx` under 8 KB is created, then a second one
- **THEN** the first file is created and the second is refused
- **AND** with a 3 KB quota (the file exceeds twice the limit) the first file is already refused, and a chunked upload answers 402 with "quota"

### Requirement: Private rooms

The system SHALL accept `private:true` on `createRoom` for a room whose files are end-to-end encrypted on the
client (v4.0). The flag cannot be changed after creation, is reported as `private` in the room, and the
listing can be split by it. Creation requires that the caller has encryption keys set up (403 otherwise) and is
allowed to create rooms: the Owner, a DocSpace admin and a Room admin succeed, a User or Guest gets 403 and an
anonymous caller 401. A private room takes a logo and can be put into a room group. The release notes for v4.0
state that private rooms are hidden in the client until they are production-ready; the API exposes them as
described here.

#### Scenario: Private room created by an account with keys

- **WHEN** the Owner, a DocSpace admin or a Room admin with encryption keys creates a Custom room with `private:true`
- **THEN** the room is created with a positive id and reports `private:true`; a logo can be applied to it

#### Scenario: User and Guest cannot create a private room

- **WHEN** a User or Guest with encryption keys calls `createRoom` with `private:true`
- **THEN** the system rejects the request (403); anonymous gets 401

#### Scenario: Default is not private

- **WHEN** a room is created without `private`
- **THEN** it reports `private:false`

### Requirement: Listing rooms and reading a room

`getRoomsFolder` SHALL return the rooms of one section (`searchArea`, default Active) the caller can read —
the Owner and DocSpace admins see every room of the section, a Room admin, User or Guest only the rooms they
are invited to — with `current` folder metadata and `total`, and SHALL support the filters `type` (one or more
room types, OR), `filterValue` (case-insensitive substring of the title), `tags` / `withoutTags`, `subjectId`
(member), `subjectOwnerId` (creator) with `excludeSubject`, `groupId`, paging (`count`, `startIndex`) and
sorting. `getRoomInfo` SHALL return the room with its `rootFolderType`, type, title, tags, logo, quota and the
flags above; it answers 404 for a deleted room and 403 for a room the caller cannot read. Anonymous and
terminated callers get 401 on both.

#### Scenario: Type filter

- **WHEN** one room of each of the five types exists and the listing is filtered by `type=[6]`, by `[5, 6]` and by `[8]`
- **THEN** it returns one Public room, exactly the Custom and Public rooms, and no Custom, Public, Collaboration or Form filling room respectively

#### Scenario: Title filter

- **WHEN** the listing is filtered by an exact title, by the title in lower case, by a fragment combined with `type`, or by a fragment that matches nothing
- **THEN** it returns the single room, the room despite the case, the intersection, and an empty list with `total` 0

#### Scenario: Subject filters

- **WHEN** the listing is filtered by `subjectOwnerId` of a DocSpace admin, with `excludeSubject`, by `subjectId` of the Owner, or by an unknown subject id
- **THEN** it returns only the admin's rooms, only the other rooms, the rooms the Owner is a member of, and nothing respectively

#### Scenario: Visibility by role

- **WHEN** the Owner created two rooms and invited a Room admin, a User or a Guest to one of them
- **THEN** that account's listing contains the invited room and not the other; a DocSpace admin sees their own room; anonymous and terminated callers get 401
