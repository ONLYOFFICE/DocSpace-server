# room-role-permissions Specification

## Purpose

Defines the roles a member can hold inside a room — Room Owner, Room manager, Content creator, Editor,
Form Filler, Reviewer, Commentator, Viewer — which room types and user types allow each of them, and what
each role may do with the room and with its files and folders, both while the room is active and after it
is archived. This is the general matrix; room types with their own actions are specified separately
(`vdr-room-roles`, `form-filling-room-roles`). Rules for links are in `shared-links`.

Sources: product matrix "Types and Roles. Access rights", sheet "Roles", checked against the integration
tests and a live run against the server (October 2026). Where the matrix and the server disagree, this
specification follows the server; the differences are listed in `openspec/deviations.md` and, with the
evidence for every row, in `verification.md` next to this file. In the tables below `✔` means the action is
allowed and an empty cell means it is denied. Column abbreviations: RO — Room Owner, RM — Room manager,
CC — Content creator, ED — Editor, FF — Form Filler, RV — Reviewer, CM — Commentator, VW — Viewer. The
matrices themselves are stated in the first scenario of each requirement.

## Requirements

### Requirement: Roles available in each room type

The system SHALL accept a member role only when the room type offers it: a Custom room every role but Form
Filler, a Public room only Room manager and Content creator, a Collaboration room Room manager, Content
creator, Editor and Viewer, a Virtual Data Room those four plus Form Filler, a Form filling room Room
manager, Content creator and Form Filler. The Room Owner is the room's creator, not an assignable role.

#### Scenario: Role availability matrix by room type

- **WHEN** a member is invited to a room of the given type with the given role
- **THEN** the invitation is accepted or refused according to this table

| Room type          | RM | CC | ED | FF | RV | CM | VW |
|--------------------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Custom room        | ✔ | ✔ | ✔ |   | ✔ | ✔ | ✔ |
| Public room        | ✔ | ✔ |   |   |   |   |   |
| Collaboration room | ✔ | ✔ | ✔ |   |   |   | ✔ |
| Virtual Data Room  | ✔ | ✔ | ✔ | ✔ |   |   | ✔ |
| Form filling room  | ✔ | ✔ |   | ✔ |   |   |   |

#### Scenario: Form Filler is refused in a Custom room

- **WHEN** a member is invited to a Custom, Public or Collaboration room as Form Filler
- **THEN** the system rejects the request (403, "The role is not available for this user type")

#### Scenario: Public room accepts only Content creator and Room manager

- **WHEN** a member is invited to a Public room as Editor, Reviewer, Commentator or Viewer
- **THEN** the system rejects the request (403)

### Requirement: Roles available to each user type

The system SHALL allow a room role to be assigned to a member only when the member's user type permits it:
the Owner, DocSpace admins and Room admins may hold every role including Room Owner and Room manager; a
User or a Guest may hold any role except Room Owner and Room manager.

#### Scenario: Role availability matrix by user type

- **WHEN** a role is assigned to a member of the given user type
- **THEN** the assignment is accepted or refused according to this table

| User type      | RO | RM | CC | ED | FF | RV | CM | VW |
|----------------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| DocSpace Owner | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| DocSpace admin | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Room admin     | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| User           |   |   | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Guest          |   |   | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |

#### Scenario: User cannot become Room manager

- **WHEN** the Room manager role is assigned to a member whose type is User or Guest, by anyone including the DocSpace Owner
- **THEN** the system rejects the assignment (403, "The role is not available for this user type")

#### Scenario: Guest can be Content creator

- **WHEN** the Content creator role is assigned to a Guest
- **THEN** the role is assigned

#### Scenario: User cannot own a room

- **WHEN** room ownership is transferred to a member whose type is User or Guest
- **THEN** the system rejects the transfer (403)

### Requirement: Room management by role

The system SHALL allow room-level actions in an active room by role: editing the room, inviting, assigning
and changing roles, removing members and managing room links need the Room Owner or a Room manager;
viewing the member list, history and info is open to every role; archiving and deleting the room is the
Room Owner's alone; every role but the Room Owner may leave the room. Invitations take effect immediately;
there is no moderation of invitations.

#### Scenario: Room management matrix by role

- **WHEN** a member with the given role performs a room-level action in an active room
- **THEN** the outcome follows this table

| Action                                  | RO | RM | CC | ED | FF | RV | CM | VW |
|-----------------------------------------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Edit the room                           | ✔ | ✔ |   |   |   |   |   |   |
| Invite users                            | ✔ | ✔ |   |   |   |   |   |   |
| Assign member roles when inviting       | ✔ | ✔ |   |   |   |   |   |   |
| Change member roles                     | ✔ | ✔ |   |   |   |   |   |   |
| Remove users                            | ✔ | ✔ |   |   |   |   |   |   |
| Create / edit / delete room links       | ✔ | ✔ |   |   |   |   |   |   |
| Leave the room                          |   | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View the member list                    | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room history                       | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room info                          | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Archive the room                        | ✔ |   |   |   |   |   |   |   |
| Delete the room                         | ✔ |   |   |   |   |   |   |   |

#### Scenario: Editor cannot invite

- **WHEN** a Content creator, Editor, Reviewer, Commentator or Viewer invites a user, changes another member's role or removes a member
- **THEN** the system rejects the request (403) and the member list is unchanged

#### Scenario: Room manager manages members

- **WHEN** a Room manager invites a user with a role, changes a member's role, demotes another Room manager or removes a member
- **THEN** the change is applied immediately

#### Scenario: Room manager invites a foreign guest

- **WHEN** a Room manager invites a guest they did not invite to the portal
- **THEN** the request succeeds with an empty result and the guest is not added (see `user-type-permissions`)

#### Scenario: Room manager cannot archive the room

- **WHEN** a Room manager attempts to archive or delete the room
- **THEN** the system rejects the request (403)

#### Scenario: Member cannot raise own role

- **WHEN** a Viewer or an Editor sets a higher role for themselves
- **THEN** the system rejects the request (403); no member other than a Room manager changes their own role

#### Scenario: Room manager lowers own role

- **WHEN** a Room manager sets the Viewer role for themselves
- **THEN** the role is lowered and a later attempt to restore it is rejected (403)

#### Scenario: Member leaves the room

- **WHEN** a member removes only themselves from the room
- **THEN** the membership is removed and the room is no longer accessible to them

#### Scenario: Owner cannot leave the room

- **WHEN** the Room Owner removes themselves from the room
- **THEN** the request succeeds without any change

### Requirement: Working with file content by role

The system SHALL allow content-level actions on files of an active room by role: every role views,
prints, copies to the clipboard and downloads; every role but Viewer comments; Room Owner, Room manager,
Content creator, Editor and Reviewer review; Room Owner, Room manager, Content creator, Editor and Form
Filler fill forms; Room Owner, Room manager, Content creator and Editor edit. Form filling applies to PDF
forms in Virtual Data Rooms and Form filling rooms only (see those specifications).

#### Scenario: Content action matrix by role

- **WHEN** a member with the given role opens a file of an active room
- **THEN** the editor offers the actions of this table

| Action                               | RO | RM | CC | ED | FF | RV | CM | VW |
|--------------------------------------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| View content                         | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View comments                        | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Copy content to the clipboard        | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Print                                | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Download / save as                   | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Comment                              | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |   |
| Review                               | ✔ | ✔ | ✔ | ✔ |   | ✔ |   |   |
| Fill in form fields                  | ✔ | ✔ | ✔ | ✔ | ✔ |   |   |   |
| Edit files                           | ✔ | ✔ | ✔ | ✔ |   |   |   |   |

#### Scenario: Viewer opens a file

- **WHEN** a Viewer opens a file
- **THEN** the file opens for viewing with printing, downloading and copying available, and without commenting, reviewing, form filling or editing

#### Scenario: Commentator comments but does not review

- **WHEN** a Commentator opens a file
- **THEN** commenting is available and reviewing, form filling and editing are not

#### Scenario: Reviewer does not fill forms

- **WHEN** a Reviewer opens a file
- **THEN** reviewing and commenting are available and form filling and editing are not

### Requirement: Managing files and folders by role

The system SHALL allow file- and folder-management actions in an active room by role: Room Owner, Room
manager and Content creator create, upload, copy in, lock and manage versions; Room Owner and Room manager
also act on other members' entries, unlock foreign locks and manage file and folder links; a Content
creator deletes, moves and renames only entries they created ("own" means created by the member); every
role copies out of the room. The matrix scenario holds the full table.

#### Scenario: File management matrix by role

- **WHEN** a member with the given role performs a file or folder action in an active room
- **THEN** the outcome follows this table

| Action                                              | RO | RM | CC | ED | FF | RV | CM | VW |
|-----------------------------------------------------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Create files and folders                            | ✔ | ✔ | ✔ |   |   |   |   |   |
| Upload files and folders                            | ✔ | ✔ | ✔ |   |   |   |   |   |
| Copy files and folders from My documents into the room | ✔ | ✔ | ✔ |   |   |   |   |   |
| Lock an unlocked file                               | ✔ | ✔ | ✔ |   |   |   |   |   |
| Unlock a file locked by someone else                | ✔ | ✔ |   |   |   |   |   |   |
| View file version history                           | ✔ | ✔ | ✔ | ✔ |   |   |   |   |
| Manage version history                              | ✔ | ✔ | ✔ |   |   |   |   |   |
| Save a docxf as oform                               | ✔ | ✔ | ✔ |   |   |   |   |   |
| Delete own files and folders                        | ✔ | ✔ | ✔ |   |   |   |   |   |
| Move own files and folders                          | ✔ | ✔ | ✔ |   |   |   |   |   |
| Rename own files and folders                        | ✔ | ✔ | ✔ |   |   |   |   |   |
| Delete other members' files and folders             | ✔ | ✔ |   |   |   |   |   |   |
| Move other members' files and folders               | ✔ | ✔ |   |   |   |   |   |   |
| Rename other members' files and folders             | ✔ | ✔ |   |   |   |   |   |   |
| Copy any file or folder out of the room             | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Copy any file or folder within the room             | ✔ | ✔ | ✔ |   |   |   |   |   |
| Create / edit / delete file and folder links        | ✔ | ✔ |   |   |   |   |   |   |

#### Scenario: Copying does not depend on authorship

- **WHEN** any member who can read a file copies it
- **THEN** the copy is created wherever the member can create content (their My documents, or a room where they are Content creator or higher); a Guest, who has no My documents, cannot copy from or to it, and a Viewer loses the right to copy out when the room denies downloading

#### Scenario: Content creator deletes own file

- **WHEN** a Content creator deletes, moves or renames a file or folder they created
- **THEN** the operation succeeds

#### Scenario: Content creator cannot delete another member's file

- **WHEN** a Content creator attempts to delete, move or rename a file created by another member
- **THEN** the system rejects the request (403)

#### Scenario: Content creator copies another member's file

- **WHEN** a Content creator copies a file created by the owner within the room, to another room where they are Content creator, or to My documents
- **THEN** the copy is created

#### Scenario: Viewer copies a file out of the room

- **WHEN** a Viewer, Reviewer, Commentator or Editor copies a file from the room to My documents
- **THEN** the copy is created

#### Scenario: Viewer cannot copy into the room

- **WHEN** an Editor or a Viewer copies a file into the room root
- **THEN** the system rejects the request (403)

#### Scenario: Content creator locks another member's file

- **WHEN** a Content creator locks an unlocked file created by the owner
- **THEN** the file is locked by the Content creator, and the Content creator can unlock it again; locking follows the lock holder, not the author

#### Scenario: Content creator cannot unlock someone else's lock

- **WHEN** a Content creator attempts to unlock a file locked by the owner, even a file the Content creator created
- **THEN** the system rejects the request (403)

#### Scenario: Room manager unlocks any file

- **WHEN** a Room manager unlocks a file locked by another member
- **THEN** the file is unlocked

#### Scenario: Editor cannot create files

- **WHEN** an Editor attempts to create a file or folder, open an upload session, lock a file, rename or delete a file
- **THEN** the system rejects the request (403)

#### Scenario: Editor views version history

- **WHEN** an Editor opens the version history of a file
- **THEN** the history is shown and no version-management action is available

### Requirement: Version history of third-party provider files

For files that live in a connected third-party storage provider the system SHALL NOT offer viewing or
managing version history to any role.

#### Scenario: Room Owner opens version history of a provider file

- **WHEN** any member, including the Room Owner, requests the version history of a file stored in a third-party provider
- **THEN** the system does not provide it

### Requirement: Archived room is read-only

Once a room is archived the system SHALL deny every role all modifying actions: editing the room,
inviting, assigning or changing roles, removing users, creating, uploading, editing, form filling,
reviewing, commenting, locking, managing version history, saving docxf as oform, deleting, moving and
renaming files and folders. Viewing, downloading and copying out stay available to every role; viewing
version history to Editor and above; restoring and deleting the room to the Room Owner only.

#### Scenario: Archived room matrix by role

- **WHEN** a member with the given role acts in an archived room
- **THEN** only the actions of this table succeed

| Action in an archived room                              | RO | RM | CC | ED | FF | RV | CM | VW |
|---------------------------------------------------------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| View the member list                                    | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room history                                       | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room info                                          | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View content                                            | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View comments                                           | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Copy content to the clipboard                           | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Print                                                   | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Download / save as                                      | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View file version history                               | ✔ | ✔ | ✔ | ✔ |   |   |   |   |
| Copy files and folders to My documents or own rooms     | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Restore the room                                        | ✔ |   |   |   |   |   |   |   |
| Delete the room                                         | ✔ |   |   |   |   |   |   |   |

#### Scenario: Non-member administrators restore an archived room

- **WHEN** the DocSpace Owner or a DocSpace admin who is not a member restores or deletes an archived room
- **THEN** the operation succeeds (see `user-type-permissions`); a Guest cannot copy out to My documents, having no such section

#### Scenario: Editor cannot edit in an archived room

- **WHEN** an Editor attempts to edit, comment on, rename, delete or lock a file in an archived room
- **THEN** the system rejects the request (403)

#### Scenario: Owner cannot modify an archived room

- **WHEN** the Room Owner attempts to rename a file, invite a user or change a role in an archived room
- **THEN** the system rejects the request (403)

#### Scenario: Viewer copies files out of an archived room

- **WHEN** any member, a Viewer included, copies a file from an archived room to My documents
- **THEN** the copy is created

#### Scenario: Reviewer has no version history in an archived room

- **WHEN** a Reviewer, Commentator or Viewer requests the version history of a file in an archived room
- **THEN** the system rejects the request (403)

#### Scenario: Only the Room Owner restores

- **WHEN** a Room manager attempts to restore or delete an archived room
- **THEN** the system rejects the request (403)
