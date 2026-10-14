# shared-links Specification

## Purpose

Defines the rules for shared (external) links: on which entities — a room, a folder, a file — a link can be
created depending on where the entity lives, how many links an entity may have, which settings a link
offers in each room type, how a rule that a request violates is applied, and which room roles may manage
links.

Sources: product matrix "Shared link rules", checked against the integration tests and a live run against
the server (October 2026). Where the matrix and the server disagree, this specification follows the
server; the differences are listed in `openspec/deviations.md`. A link to a room and a link to a folder
or a file inside it are different links with independent limits and settings. A link is either primary
(permanent, one per entity in rooms) or additional. An invitation link is not a shared link and is not
covered here.

## Requirements

### Requirement: Entities that accept a shared link

The system SHALL allow creating shared links on an entity according to the section or room type that
contains it.

| Location            | Room | Folder | File |
|---------------------|:----:|:------:|:----:|
| My documents        | not applicable — a section cannot be linked | up to 6 additional | up to 6 additional |
| Public room         | 1 primary + up to 5 additional | 1 primary + up to 5 additional | 1 primary + up to 5 additional |
| Form filling room   | 1 primary, no additional | 1 primary, no additional | 1 primary, no additional |
| Collaboration room  | not allowed | up to 6 additional | up to 6 additional |
| Virtual Data Room   | not allowed | up to 6 additional | up to 6 additional |
| Custom room         | 1 primary + up to 5 additional | up to 6 additional | up to 6 additional |

#### Scenario: Link to a Collaboration room itself

- **WHEN** a Room Owner attempts to create a shared link to a Collaboration room or a Virtual Data Room itself
- **THEN** the system rejects the request (403, "The role is not available for this user type")

#### Scenario: Link to a file in a Collaboration room

- **WHEN** a Room Owner creates a shared link to a file or a folder inside a Collaboration room or a Virtual Data Room
- **THEN** the link is created

#### Scenario: Link to a Custom room

- **WHEN** a Room Owner creates a shared link to a Custom room
- **THEN** the link is created

### Requirement: Link count limits

The system SHALL limit the number of additional links on an entity to 6 for files and folders in My
documents, Collaboration rooms, Virtual Data Rooms and Custom rooms, and to 5 for a Public room, a Custom
room and the files and folders inside a Public room, where one more link is the primary link. In a Form
filling room an entity SHALL have only its primary link. A request beyond the limit SHALL be rejected with
403 and the message "The maximum number of links may not exceed {N}".

#### Scenario: Seventh additional link on a file

- **WHEN** a file in My documents already has 6 additional links and a seventh is requested
- **THEN** the system rejects the request (403, "…may not exceed 6")

#### Scenario: Sixth additional link in a Public room

- **WHEN** a Public room already has its primary link and 5 additional links and another additional link is requested
- **THEN** the system rejects the request (403, "…may not exceed 5")

#### Scenario: Second primary link in a Public room

- **WHEN** a second primary link is requested for a Public room or for a file inside it
- **THEN** the system rejects the request (403, "…may not exceed 1")

#### Scenario: Additional link in a Form filling room

- **WHEN** an additional link is requested for a Form filling room or for a folder or file inside it
- **THEN** the system rejects the request (403, "…may not exceed 0")

### Requirement: Room links and item links are independent

A link to a room SHALL NOT be treated as a link to a folder or file inside the room, and a link to a folder
or a file SHALL NOT be treated as a link to its room: each entity has its own links, its own limit and its
own settings.

A folder link SHALL open the folder and every file in it with the folder link's access right. A link
created on a file SHALL open only that file, with its own access right, and SHALL NOT open the parent
folder or sibling files. Changing the access right of one link SHALL NOT change the access right of any
other link.

#### Scenario: Room links do not consume the file limit

- **WHEN** a Custom room has its 6 room links and a link to a file inside it is requested
- **THEN** the file link is created

#### Scenario: File opened through a folder link

- **WHEN** a folder has a view-only link, a file inside it has its own link with the editing right, and the file is opened through the folder link
- **THEN** the file opens for viewing

#### Scenario: File opened through its own link

- **WHEN** the same file is opened through its own link
- **THEN** it opens for editing, and the parent folder and sibling files are not accessible with that link (403)

#### Scenario: Folder link rights change

- **WHEN** the access right of a folder link is changed
- **THEN** the access rights of the links created on files inside the folder stay as they were

### Requirement: Link settings by room type

The system SHALL offer link settings according to the section or room type that contains the entity.

| Location            | Access rights | Who has access | Password | Deny download | Lifetime |
|---------------------|:-------------:|:--------------:|:--------:|:-------------:|:--------:|
| My documents        | ✔ | ✔ | ✔ | ✔ | ✔ |
| Public room         | ✔ | ✗ | ✔ | ✔ | additional links only |
| Form filling room   | ✗ | ✔ | ✔ | ✔ | room link ✗ |
| Collaboration room  | ✔ | ✔ | ✔ | ✔ | ✔ |
| Virtual Data Room   | ✔ | ✗ | ✔ | ✔ | ✔ |
| Custom room         | ✔ | ✔ | ✔ | ✔ | ✔ |

"Who has access" is the choice between "Anyone with the link" and "DocSpace users only". "Lifetime" is the
link expiration. Where a setting is not offered, its value is fixed:

- Public room, who has access: every link SHALL be available to anyone with the link.
- Virtual Data Room, who has access: every link SHALL be available to DocSpace users only.
- Form filling room, access rights: the room link SHALL grant reading or form filling; a link to a form or
  folder inside SHALL grant form filling only.
- Form filling room and Public room, lifetime: the primary link of the room itself SHALL be perpetual; a
  lifetime MAY be set on additional room links and on links of files and folders inside.

#### Scenario: Form filling room link with the editing right

- **WHEN** a link in a Form filling room is created or edited with the editing, review or comment right
- **THEN** the system rejects the request (403, "The role is not available for this user type")

#### Scenario: Form link inside a Form filling room with the read right

- **WHEN** a link to a form or folder inside a Form filling room is requested with the read right
- **THEN** the system rejects the request (403)

#### Scenario: Primary room link does not expire

- **WHEN** an expiration date is set on the primary link of a Public room or a Form filling room
- **THEN** the request succeeds and the stored link has no expiration

#### Scenario: Additional link of a Public room expires

- **WHEN** an expiration date is set on an additional link of a Public room
- **THEN** the expiration date is saved

#### Scenario: Password and download restriction everywhere

- **WHEN** a password or the deny-download flag is set on a link in any section or room type
- **THEN** the setting is saved

### Requirement: A fixed "who has access" value silently skips the update

When a request for a link in a Public room or a Virtual Data Room carries a "who has access" value that
differs from the fixed one, the system SHALL answer success and SHALL NOT apply any part of that request —
title, password, access right and revocation included. A request that omits the value, or sends the fixed
one, SHALL be applied.

#### Scenario: Public room link restricted to DocSpace users

- **WHEN** a link in a Public room is created or edited with access limited to DocSpace users only
- **THEN** the request succeeds and the link stays available to anyone with the link

#### Scenario: VDR link opened to anyone

- **WHEN** a link in a Virtual Data Room is edited with access for anyone with the link and a new password in the same request
- **THEN** the request succeeds, the link stays for DocSpace users only and the password is not changed

#### Scenario: Revoking a VDR link with the wrong audience value

- **WHEN** a VDR link is revoked in a request that also sets access for anyone with the link
- **THEN** the request succeeds and the link is not revoked

#### Scenario: Anonymous visitor opens a VDR link

- **WHEN** a visitor who is not signed in to the portal opens a link to a file in a Virtual Data Room
- **THEN** the system requires authentication as a DocSpace user before granting access

### Requirement: Deny-download on links combines with the VDR room setting

The deny-download flag of a link SHALL be stored as requested in every room type. The effective rule for a
link visitor in a Virtual Data Room SHALL be the stricter of the room-wide restriction on downloading and
copying content and the link's own flag: when the room setting is on, a link with the flag off still
denies downloading; when the room setting is off, the link's flag alone decides. The room's owner, Room
managers, Content creators and Editors are not restricted.

#### Scenario: Room-wide restriction is on

- **WHEN** a VDR has the room-wide restriction turned on and a visitor downloads a file through a link whose deny-download flag is off
- **THEN** the download is refused (403)

#### Scenario: Room-wide restriction is off

- **WHEN** a VDR has the room-wide restriction turned off, one file link has deny-download on and another has it off
- **THEN** the download through the first link is refused and through the second succeeds

### Requirement: Roles that manage links

The system SHALL allow only the Room Owner and Room managers to create, edit and delete shared links — both
links to the room and links to files and folders inside it. Content creator, Editor, Form Filler, Reviewer,
Commentator and Viewer SHALL NOT create, edit or delete any shared link, not even on files they created.
Any member MAY read the room's primary link; reading the primary link of a file requires the right to
manage links. A DocSpace admin who is not a member MAY read the primary link of a Public room but SHALL NOT
create, edit or delete links there.

#### Scenario: Room manager creates a link

- **WHEN** a Room manager creates, edits or deletes a link to the room or to a file or folder inside it
- **THEN** the operation succeeds

#### Scenario: Content creator cannot create a file link

- **WHEN** a Content creator attempts to create, edit or delete a link to a file, even one they created
- **THEN** the system rejects the request (403)

#### Scenario: Editor cannot edit a room link

- **WHEN** an Editor, Form Filler, Reviewer, Commentator or Viewer attempts to create, edit or delete a room link
- **THEN** the system rejects the request (403)

#### Scenario: Member reads the room's primary link

- **WHEN** a Viewer requests the primary link of a Custom room
- **THEN** the link is returned

#### Scenario: Non-member admin cannot create a link

- **WHEN** a DocSpace admin who is not a member creates a link in a Public room
- **THEN** the system rejects the request (403)
