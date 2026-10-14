# vdr-room-roles Specification

## Purpose

Defines the role matrix of a Virtual Data Room (VDR). A VDR offers a reduced role set — Room Owner,
Room manager, Content creator, Editor, Form Filler, Viewer (no Reviewer and no Commentator) — and a
form-filling workflow for PDF forms with ordered participants. Where this specification is silent,
`room-role-permissions` applies; link rules are in `shared-links`.

Sources: product matrix "Types and Roles. Access rights", sheet "VDR", checked against the integration
tests and a live run against the server (October 2026). Where the matrix and the server disagree, this
specification follows the server; the differences are listed in `openspec/deviations.md`. In the tables
below `✔` means the action is allowed and an empty cell means it is denied. Column abbreviations:
RO — Room Owner, RM — Room manager, CC — Content creator, ED — Editor, FF — Form Filler, VW — Viewer.

## Requirements

### Requirement: VDR role set

A Virtual Data Room SHALL offer only the roles Room Owner, Room manager, Content creator, Editor,
Form Filler and Viewer.

#### Scenario: Reviewer role is not available in a VDR

- **WHEN** a member is invited to a VDR with the Reviewer or Commentator role
- **THEN** the system rejects the request (403, "The role is not available for this user type")

### Requirement: VDR room management by role

The system SHALL allow room-level actions in a VDR by role as follows; the self-role and leave rules of
`room-role-permissions` apply.

| Action                                  | RO | RM | CC | ED | FF | VW |
|-----------------------------------------|:--:|:--:|:--:|:--:|:--:|:--:|
| Edit the room                           | ✔ | ✔ |   |   |   |   |
| Invite users                            | ✔ | ✔ |   |   |   |   |
| Assign member roles when inviting       | ✔ | ✔ |   |   |   |   |
| Change member roles                     | ✔ | ✔ |   |   |   |   |
| Remove users                            | ✔ | ✔ |   |   |   |   |
| View the member list                    | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room history                       | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View room info                          | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Archive the room                        | ✔ |   |   |   |   |   |
| Delete the room                         | ✔ |   |   |   |   |   |

#### Scenario: Content creator cannot invite to a VDR

- **WHEN** a Content creator, Editor, Form Filler or Viewer attempts to invite a user to a VDR
- **THEN** the system rejects the request (403)

#### Scenario: Room manager invites to a VDR

- **WHEN** a Room manager invites a user to a VDR with one of the VDR roles
- **THEN** the member is added

### Requirement: VDR files and folders by role

The system SHALL allow file and folder actions in a VDR by role as follows. "Own" means created by the
member. Version history of files stored in a third-party provider SHALL NOT be available to any role.

| Action                                          | RO | RM | CC | ED | FF | VW |
|-------------------------------------------------|:--:|:--:|:--:|:--:|:--:|:--:|
| View content                                    | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| View comments                                   | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Copy content to the clipboard                   | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Print                                           | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Download / save as                              | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Comment                                         | ✔ | ✔ | ✔ | ✔ | ✔ |   |
| Review                                          | ✔ | ✔ | ✔ | ✔ |   |   |
| Edit files                                      | ✔ | ✔ | ✔ | ✔ |   |   |
| View file version history                       | ✔ | ✔ | ✔ | ✔ |   |   |
| Create files and folders                        | ✔ | ✔ | ✔ |   |   |   |
| Upload files and folders                        | ✔ | ✔ | ✔ |   |   |   |
| Copy files and folders from My documents        | ✔ | ✔ | ✔ |   |   |   |
| Lock an unlocked file                           | ✔ | ✔ | ✔ |   |   |   |
| Unlock a file locked by someone else            | ✔ | ✔ |   |   |   |   |
| Manage version history                          | ✔ | ✔ | ✔ |   |   |   |
| Delete own files and folders                    | ✔ | ✔ | ✔ |   |   |   |
| Move own files and folders                      | ✔ | ✔ | ✔ |   |   |   |
| Rename own files and folders                    | ✔ | ✔ | ✔ |   |   |   |
| Delete other members' files and folders         | ✔ | ✔ |   |   |   |   |
| Move other members' files and folders           | ✔ | ✔ |   |   |   |   |
| Rename other members' files and folders         | ✔ | ✔ |   |   |   |   |
| Copy any file or folder out of the room         | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Copy any file or folder within the room         | ✔ | ✔ | ✔ |   |   |   |
| Create / edit / delete file and folder links    | ✔ | ✔ |   |   |   |   |

A Form Filler SHALL NOT open files that are not PDF forms. When the room setting that restricts copying
and downloading content is turned on, a Viewer SHALL NOT download files or copy them out of the room; the
restriction does not apply to the owner, Room managers, Content creators and Editors.

#### Scenario: Form Filler opens an ordinary document

- **WHEN** a Form Filler requests a file in the VDR that is not a PDF form
- **THEN** the system denies access (403)

#### Scenario: Viewer in a room with the download restriction

- **WHEN** the VDR has the copy and download restriction turned on and a Viewer attempts to download a file, request its view URL or copy it to My documents
- **THEN** the system rejects the request (403)

#### Scenario: Editor in a room with the download restriction

- **WHEN** the same room has the restriction turned on and an Editor downloads a file
- **THEN** the download succeeds

#### Scenario: Content creator renames own entries

- **WHEN** a Content creator renames a file or folder they created
- **THEN** the rename succeeds
- **AND** renaming a file created by the owner or by another Content creator is rejected (403)

#### Scenario: Content creator copies but does not move others' files

- **WHEN** a Content creator copies a file created by the owner within the room or to another VDR where they are Content creator
- **THEN** the copy is created
- **AND** moving the same file is rejected (403)

#### Scenario: Viewer cannot comment in a VDR

- **WHEN** a Viewer attempts to add a comment to a file in a VDR
- **THEN** the system rejects the request (403)

#### Scenario: Editor edits but does not create

- **WHEN** an Editor edits an existing file in a VDR
- **THEN** the edit succeeds
- **AND** an attempt by the same Editor to create, upload, delete or rename a file is rejected (403)

### Requirement: PDF form filling in a VDR

The system SHALL allow the PDF form workflow in a VDR by role as follows. A "configured" form is a PDF
form for which filling has been set up with assigned participants (roles); a "complete" form is one whose
filling has finished. No role SHALL be able to edit a complete form.

| Action                                                    | RO | RM | CC | ED | FF | VW |
|-----------------------------------------------------------|:--:|:--:|:--:|:--:|:--:|:--:|
| Create PDF forms in the room                              | ✔ | ✔ | ✔ |   |   |   |
| Upload PDF forms to the room                              | ✔ | ✔ | ✔ |   |   |   |
| Edit PDF forms that are not configured                    | ✔ | ✔ | ✔ | ✔ |   |   |
| See PDF forms that are not configured                     | ✔ | ✔ | ✔ | ✔ |   | ✔ |
| Configure filling (assign participants)                   | ✔ | ✔ | ✔ |   |   |   |
| Stop filling of own forms                                 | ✔ | ✔ | ✔ |   |   |   |
| Stop filling of other members' forms                      | ✔ | ✔ |   |   |   |   |
| See configured forms the member does NOT take part in     | ✔ | ✔ | ✔ | ✔ |   | ✔ |
| See configured forms the member takes part in             | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Fill the form when their turn has come                    | ✔ | ✔ | ✔ | ✔ | ✔ |   |
| See complete forms in the room                            | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Edit complete forms in the room                           |   |   |   |   |   |   |

A Content creator SHALL configure filling for any form in the room, but SHALL stop filling only for forms
they created — configuring someone else's form does not grant the right to stop it. A Form Filler SHALL
see a configured form as soon as they are assigned to it, including before their turn; the turn governs
the right to fill, not visibility. A Form Filler SHALL see only their own complete forms.

#### Scenario: Form Filler sees only forms assigned to them

- **WHEN** a Form Filler lists the contents of a VDR
- **THEN** the list includes configured forms in which the Form Filler takes part and their own complete forms
- **AND** it excludes PDF forms that are not configured, configured forms in which the Form Filler does not take part and complete forms of other members

#### Scenario: Form Filler before their turn

- **WHEN** a form is configured with several participants in order and the turn of a Form Filler on a later step has not come yet
- **THEN** the form is visible to that Form Filler, with filling not yet available

#### Scenario: Content creator configures the owner's form

- **WHEN** a Content creator assigns participants to a form created by the owner
- **THEN** the configuration is saved

#### Scenario: Content creator cannot stop the owner's form

- **WHEN** a Content creator attempts to stop the filling of a form created by the owner, even one whose filling the Content creator configured
- **THEN** the system rejects the request (403)

#### Scenario: Content creator stops own form

- **WHEN** a Content creator stops the filling of a form they created
- **THEN** the filling is stopped

#### Scenario: Viewer sees forms they do not take part in

- **WHEN** a Viewer lists the contents of a VDR
- **THEN** the list includes PDF forms that are not configured and configured forms regardless of participation

#### Scenario: Editor cannot configure filling

- **WHEN** an Editor, Viewer or Form Filler attempts to configure or to stop the filling of a PDF form
- **THEN** the system rejects the request (403)

#### Scenario: Complete form is immutable

- **WHEN** any member, including the Room Owner, attempts to edit a complete form
- **THEN** the system rejects the request
