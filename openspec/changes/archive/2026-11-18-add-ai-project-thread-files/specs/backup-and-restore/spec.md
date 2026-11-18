# Spec Delta

## MODIFIED Requirements

### Requirement: Contents of a portal archive

A portal archive SHALL hold the rows of the portal from every table declared by a backup module
(tenants, audit, files, settings, webhooks and their journal, core users and groups, identity, AI)
and the files of the allowed storage modules (documents, logos, white label, room logos, web
plugins, MCP icons). It SHALL leave out thumbnails, earlier archives stored in Documents, and tables
tied to the source installation or transient (short links, account links, tariff rows, queues).

#### Scenario: Every tenant table is accounted for

- **WHEN** the tables with a tenant column of the schema are compared with the declared ones
- **THEN** each is either declared, intentionally skipped with a reason, or listed as a known gap

#### Scenario: Application settings are restored

- **WHEN** the portal had switched an application (for example the AI rooms) off and saved its settings document
- **THEN** the restored portal carries the same application id, switch state, settings and last-modified time under its new tenant (bug 84721)

#### Scenario: AI room links and skills follow their folders and files

- **WHEN** an Instructions or Knowledge room linked to an AI project, or a skill file of an Instructions room, is restored
- **THEN** the link points at the new ids of the project and the room, and the skill row at the new id of its file, its name and description copied as they were

#### Scenario: A project thread follows its project and its file sets

- **WHEN** a thread of an AI project that has artifacts and attachments is restored
- **THEN** the thread points at the new ids of the project and of both file sets, under the new tenant and the new id of its creator
- **AND** a thread without file sets is restored with none, and an archive made before threads had file sets is restored as it is

#### Scenario: Document agent settings follow their log folder

- **WHEN** a portal whose document agent settings name a task log folder is restored
- **THEN** the restored settings name the new id of that folder, or no folder when it was not restored, and every other field is copied as it was

#### Scenario: Earlier backups are not backed up again

- **WHEN** the portal keeps earlier archives in a Documents folder
- **THEN** those files are excluded from the new archive and their size is subtracted from the stored quota rows inside it
