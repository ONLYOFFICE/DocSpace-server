# form-filling-room-roles Specification

## Purpose

Defines the role matrix of a Form filling room (FFR). A Form filling room holds PDF files only, offers four
roles — Room Owner, Room manager, Content creator, Form Filler — and adds actions of its own: starting and
cancelling the collection of a form, the "In process" and "Complete" result folders, and collecting the
results into a spreadsheet or a database. Where this specification is silent, `room-role-permissions`
applies; link rules are in `shared-links`.

Sources: product matrix "Types and Roles. Access rights", sheet "FFR", checked against the integration
tests and a live run against the server (October 2026). Where the matrix and the server disagree, this
specification follows the server; the differences are listed in `openspec/deviations.md`. In the tables
below `✔` means the action is allowed and an empty cell means it is denied. Column abbreviations:
RO — Room Owner, RM — Room manager, CC — Content creator, FF — Form Filler.

## Requirements

### Requirement: Form filling room role set

A Form filling room SHALL offer only the roles Room Owner, Room manager, Content creator and Form Filler.

#### Scenario: Editor role is not available in a Form filling room

- **WHEN** a member is invited to a Form filling room with the Editor, Reviewer, Commentator or Viewer role
- **THEN** the system rejects the request (403, "The role is not available for this user type")

### Requirement: Form filling room management by role

The system SHALL allow room-level actions in a Form filling room by role as follows; the self-role and
leave rules of `room-role-permissions` apply.

| Action                                          | RO | RM | CC | FF |
|-------------------------------------------------|:--:|:--:|:--:|:--:|
| Edit the room                                   | ✔ | ✔ |   |   |
| Invite users                                    | ✔ | ✔ |   |   |
| Assign member roles when inviting               | ✔ | ✔ |   |   |
| Change member roles                             | ✔ | ✔ |   |   |
| Remove users                                    | ✔ | ✔ |   |   |
| View the member list                            | ✔ | ✔ | ✔ | ✔ |
| View room history                               | ✔ | ✔ | ✔ | ✔ |
| View room info                                  | ✔ | ✔ | ✔ | ✔ |
| Archive the room                                | ✔ |   |   |   |
| Delete the room                                 | ✔ |   |   |   |
| Collect results of any form into the spreadsheet | ✔ | ✔ | ✔ |   |
| Turn collecting results into XLSX on or off     | ✔ | ✔ |   |   |
| Turn synchronization with a database on or off  | ✔ | ✔ |   |   |

#### Scenario: Content creator collects results of the owner's form

- **WHEN** a Content creator requests the results spreadsheet of a form started by the owner
- **THEN** the collection task is queued

#### Scenario: Form Filler cannot collect results

- **WHEN** a Form Filler requests the results spreadsheet of a form
- **THEN** the system rejects the request (403)

#### Scenario: Content creator cannot switch result collection

- **WHEN** a Content creator attempts to turn XLSX collection or database synchronization on or off
- **THEN** the system rejects the request (403)

#### Scenario: Form Filler cannot invite

- **WHEN** a Form Filler or a Content creator attempts to invite a user to a Form filling room
- **THEN** the system rejects the request (403)

### Requirement: Only PDF files enter a Form filling room

The system SHALL accept an upload to a Form filling room, or a copy or move from My documents into it,
only when the file is a PDF. The check is by file format: an ordinary PDF that is not an ONLYOFFICE form is
accepted as well. The destination check (`checkdestfolder`) SHALL warn about a PDF that is not a form, so
that a client can refuse it before uploading.

#### Scenario: Uploading a non-PDF file

- **WHEN** a member with upload rights uploads a docx or copies one from My documents into the room
- **THEN** the system rejects the file ("The file cannot be uploaded to this room. Please try to upload the ONLYOFFICE PDF form.")

#### Scenario: Copying an ONLYOFFICE PDF form from My documents

- **WHEN** a member with the right to copy from My documents copies an ONLYOFFICE PDF form into the room
- **THEN** the form is added to the room

#### Scenario: Moving a PDF into the room starts collecting

- **WHEN** a PDF is moved from My documents into a Form filling room
- **THEN** the file arrives with collecting already started and is visible to Form Fillers

### Requirement: PDF forms and folders by role

The system SHALL allow actions on PDF forms and folders in a Form filling room by role as follows.
"Own" means created by the member.

| Action                                              | RO | RM | CC | FF |
|-----------------------------------------------------|:--:|:--:|:--:|:--:|
| View content                                        | ✔ | ✔ | ✔ | ✔ |
| Copy content to the clipboard                       | ✔ | ✔ | ✔ | ✔ |
| Print                                               | ✔ | ✔ | ✔ | ✔ |
| Download / save as                                  | ✔ | ✔ | ✔ | ✔ |
| View comments                                       | ✔ | ✔ | ✔ |   |
| Create PDF forms in the room                        | ✔ | ✔ | ✔ |   |
| Upload PDF forms to the room                        | ✔ | ✔ | ✔ |   |
| Copy PDF forms and folders from My documents        | ✔ | ✔ | ✔ |   |
| Edit PDF forms                                      | ✔ | ✔ | ✔ |   |
| Lock an unlocked file                               | ✔ | ✔ | ✔ |   |
| Unlock a file locked by someone else                | ✔ | ✔ |   |   |
| View file version history                           | ✔ | ✔ | ✔ |   |
| Manage version history                              | ✔ | ✔ | ✔ |   |
| Delete own PDF forms and folders                    | ✔ | ✔ | ✔ |   |
| Move own PDF forms and folders                      | ✔ | ✔ | ✔ |   |
| Rename own PDF forms and folders                    | ✔ | ✔ | ✔ |   |
| Delete other members' PDF forms and folders         | ✔ | ✔ |   |   |
| Move other members' PDF forms and folders           | ✔ | ✔ |   |   |
| Rename other members' PDF forms and folders         | ✔ | ✔ |   |   |
| Copy any form out of the room                       | ✔ | ✔ | ✔ | ✔ |
| Copy any form within the room                       | ✔ | ✔ | ✔ |   |
| Edit the room's primary link                        | ✔ | ✔ |   |   |

Once collecting has started, a Content creator SHALL edit the form only if they created it or started its
collection. A Form Filler SHALL see every form whose collection has started; inside the system folders
("In process", "Complete") a Form Filler SHALL view only the forms they themselves started or completed.

#### Scenario: Form Filler opens another member's result

- **WHEN** a Form Filler requests a form in a system folder that was started or completed by another member
- **THEN** the system denies access

#### Scenario: Form Filler does not see comments

- **WHEN** a Form Filler opens a PDF form
- **THEN** the content is shown with printing, downloading and copying available and without comments

#### Scenario: Content creator renames own form

- **WHEN** a Content creator renames a form they created
- **THEN** the rename succeeds
- **AND** renaming or deleting a form created by another member is rejected (403)

#### Scenario: Form Filler copies a started form out

- **WHEN** a Form Filler copies a form whose collection has started to My documents
- **THEN** the copy is created

### Requirement: Form collection lifecycle by role

A PDF form in a Form filling room is either not started ("editable") or started ("fillable"); filling a
started form produces results in the "In process" and "Complete" folders. The system SHALL allow the
lifecycle actions by role as follows.

| Action                                         | RO | RM | CC | FF |
|------------------------------------------------|:--:|:--:|:--:|:--:|
| See PDF forms that are not started (editable)  | ✔ | ✔ | ✔ |   |
| Start collecting any PDF form                  | ✔ | ✔ | ✔ |   |
| Cancel collecting of other members' forms      | ✔ | ✔ |   |   |
| Cancel collecting of own forms                 | ✔ | ✔ | ✔ |   |
| See started forms (fillable)                   | ✔ | ✔ | ✔ | ✔ |
| See "Complete" forms in the room               | ✔ | ✔ | ✔ | ✔ |
| See "In process" forms in the room             | ✔ | ✔ | ✔ | ✔ |
| Delete "Complete" forms in the room            | ✔ | ✔ | ✔ | ✔ |
| Delete "In process" forms in the room          | ✔ | ✔ | ✔ | ✔ |

"Own" in cancelling means forms the member created or whose collection the member started. A Content
creator and a Form Filler SHALL see and delete only their own "Complete" and "In process" results.

#### Scenario: Content creator starts and cancels collecting of the owner's form

- **WHEN** a Content creator starts collecting a form created by the owner and later cancels that collection
- **THEN** both operations succeed

#### Scenario: Content creator cancels someone else's collection

- **WHEN** a Content creator attempts to cancel collecting for a form that is not theirs and whose collection they did not start
- **THEN** the system rejects the request (403)

#### Scenario: Room manager cancels the owner's collection

- **WHEN** a Room manager cancels collecting of a form started by the owner
- **THEN** the collection is cancelled

#### Scenario: Form Filler cannot start or cancel

- **WHEN** a Form Filler attempts to start or cancel collecting
- **THEN** the system rejects the request (403)

#### Scenario: Form Filler lists results

- **WHEN** a Form Filler lists the "In process" or "Complete" folder
- **THEN** the list contains only the forms that Form Filler started or completed

#### Scenario: Form Filler does not see a form that is not started

- **WHEN** a Form Filler lists the contents of a Form filling room
- **THEN** the list includes started forms and excludes forms that are not started

#### Scenario: Form Filler deletes own results

- **WHEN** a Form Filler deletes their own form from the "In process" or "Complete" folder
- **THEN** the form is deleted
