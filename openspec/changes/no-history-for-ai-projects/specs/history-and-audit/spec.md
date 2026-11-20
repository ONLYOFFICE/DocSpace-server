# Spec Delta

## MODIFIED Requirements

### Requirement: Who may read an entry history

The system SHALL return the history of an entry to every caller who can read the entry (the Read right
on the entry), regardless of user type and room role, and SHALL refuse everyone else. An AI project and the
entries inside it have no history (see "An AI project has no history"). The
initiator of an entry is rendered as an account DTO: a caller without access to Contacts (a User or a
Guest) SHALL receive it without a `profileUrl`.

#### Scenario: Owner, DocSpace admin, Room manager and a Read member read a file log

- **WHEN** the portal owner, a DocSpace admin who is not a member, a Room admin invited as Room manager or a User invited with Read reads `file/{id}/log` or `folder/{id}/log` of an entry in a Custom room
- **THEN** the system answers 200

#### Scenario: Non-member is refused

- **WHEN** a User or a Guest who is not a member of the room reads the history of the room or of a file in it
- **THEN** the system answers 403

#### Scenario: Anonymous caller is refused

- **WHEN** the history of a room or of a file is read without authentication
- **THEN** the system answers 401

#### Scenario: Initiator without profile link for User and Guest

- **WHEN** a User or a Guest invited to the room with Read reads the log of a file in it
- **THEN** every entry's `initiator.profileUrl` is empty

### Requirement: Who may export a folder history

The system SHALL allow the report to every caller who can read the folder except a Guest, and SHALL
refuse the report when the portal is a SaaS portal whose login history and audit trail section is
hidden (402). An AI project and the folders inside it have no report (see "An AI project has no history").

#### Scenario: Readers of the room may export

- **WHEN** the owner, a non-member DocSpace admin, a Room admin invited as Room manager or with Editing, or a User invited with Read or Comment starts the report
- **THEN** the system answers 200 with a task

#### Scenario: Guest cannot export even with access

- **WHEN** a Guest invited to the room with Read, or a Guest without access, starts the report
- **THEN** the system answers 403

#### Scenario: Non-member and anonymous

- **WHEN** a User who is not a member starts the report
- **THEN** the system answers 403
- **AND** an unauthenticated caller is answered with 401 on starting, reading and stopping a report

## ADDED Requirements

### Requirement: An AI project has no history

The system SHALL keep no history for an AI project and the entries inside it. `getFileHistory`,
`getFolderHistory`, `createReportFolderHistory`, `getReportFolderHistory` and `terminateReportFolderHistory`
SHALL answer 403 for them to every caller, the project's owner and DocSpace admins included, and the project's
`security.HistoryExport` SHALL be false. Actions on them SHALL send no `update-history` event; the audit trail
still records them.

#### Scenario: History of a project is refused

- **WHEN** the owner of an AI project, a member of it or a DocSpace admin reads `folder/{id}/log` of the project
- **THEN** the system answers 403

#### Scenario: History of an own entry inside a project is refused

- **WHEN** a member uploads a file into an AI project and reads `file/{id}/log` of that file
- **THEN** the system answers 403

#### Scenario: Report of a project is refused

- **WHEN** the owner of an AI project starts, reads or stops the history report of the project
- **THEN** the system answers 403 to each call

#### Scenario: Project reports no history export right

- **WHEN** the owner of an AI project reads it with `getRoomInfo`
- **THEN** `security.HistoryExport` is false

#### Scenario: The other side of a copy keeps its history

- **WHEN** a member copies a file from a folder of their My documents into an AI project, or their file from the project into that folder
- **THEN** the history of that folder contains `FileCopied` for each copy
