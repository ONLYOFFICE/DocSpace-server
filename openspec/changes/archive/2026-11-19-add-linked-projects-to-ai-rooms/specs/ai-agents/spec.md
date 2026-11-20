# Spec Delta

## ADDED Requirements

### Requirement: Number of projects that use a room

A Knowledge or an Instructions room SHALL carry `linkedProjectsCount` in the answers of `getRoomsFolder`,
`getRoomInfo`, `createRoom` and `updateRoom`: the number of AI projects the room is linked to, 0 when there is
none. The number SHALL count every such project, whether the caller may read it or not. A room of another type,
an AI project included, SHALL NOT carry the field.

#### Scenario: Room linked to two projects

- **WHEN** the owner links a Knowledge room to two projects, lists the section with `getRoomsFolder` and `searchArea=AiHub` and reads the room with `getRoomInfo`
- **THEN** the room carries `linkedProjectsCount` 2 in both answers

#### Scenario: Room linked to no project

- **WHEN** the owner creates an Instructions room and reads it with `getRoomInfo`
- **THEN** the answer of `createRoom` and the answer of `getRoomInfo` carry `linkedProjectsCount` 0

#### Scenario: Room changed

- **WHEN** the owner renames a Knowledge room linked to one project with `updateRoom`
- **THEN** the answer carries `linkedProjectsCount` 1

#### Scenario: Rooms of other types

- **WHEN** the owner reads an AI project and a Custom room with `getRoomInfo` and lists the section with `getRoomsFolder` and `searchArea=AiHub`
- **THEN** neither the project nor the Custom room carries `linkedProjectsCount`

#### Scenario: Reader of the room outside the projects

- **WHEN** a User who is a Viewer of a Knowledge room linked to two projects, and a member of neither, reads the room with `getRoomInfo`
- **THEN** the room carries `linkedProjectsCount` 2

#### Scenario: Number follows the links

- **WHEN** a room is linked to two projects, a Room manager of the first unlinks it with `unlinkProjectSources`, and the second project is then deleted with `deleteRoom`
- **THEN** `getRoomInfo` of the room carries `linkedProjectsCount` 1 after the unlink and 0 after the deletion has completed

### Requirement: Listing the projects that use a room

`getRoomProjects` SHALL answer one page of the AI projects a Knowledge or an Instructions room is linked to, each
with `id`, `title` and `logo`, sorted by title and then by id, with the number of listed projects as the total;
an empty page when it lists none. It SHALL need read access to the room (403) and answer 404 for any other id:
an unknown one, an AI project, a room of another type. `count` SHALL be 1 to 100, 100 when omitted, and
`startIndex` 0 or more; any other value answers 400.

#### Scenario: Projects sorted by title

- **WHEN** the owner links a Knowledge room to the projects "Gamma", "Alpha" and "Beta", in that order, and calls `getRoomProjects` for the room
- **THEN** the page holds "Alpha", "Beta" and "Gamma" in that order, each with its `id`, `title` and `logo`, and the total is 3

#### Scenario: Projects with one title

- **WHEN** the owner links an Instructions room to two projects that both have the title "Twin"
- **THEN** the page lists the project with the lower id first

#### Scenario: Room linked to no project

- **WHEN** the owner calls `getRoomProjects` for a Knowledge room no project links
- **THEN** the page is empty and the total is 0

#### Scenario: Id that is no Knowledge or Instructions room

- **WHEN** the owner calls `getRoomProjects` with the id of an AI project, of a Custom room, and with an id no room has
- **THEN** each call answers 404

#### Scenario: Caller who cannot read the room

- **WHEN** a User who has no role in an Instructions room and is a member of no project that links it calls `getRoomProjects` for it
- **THEN** the system answers 403

#### Scenario: One page

- **WHEN** the owner lists the three projects of a room with `count` 1 and `startIndex` 1
- **THEN** the page holds the second project by title and the total is 3

#### Scenario: Start index past the end

- **WHEN** the owner lists the three projects of a room with `startIndex` 3
- **THEN** the page is empty and the total is 3

#### Scenario: Count of zero

- **WHEN** the owner calls `getRoomProjects` with `count` 0
- **THEN** the system answers 400

#### Scenario: Negative count

- **WHEN** the owner calls `getRoomProjects` with `count` -1
- **THEN** the system answers 400

#### Scenario: Count above the maximum

- **WHEN** the owner calls `getRoomProjects` with `count` 101
- **THEN** the system answers 400

#### Scenario: Empty count

- **WHEN** the owner calls `getRoomProjects` with `count` sent without a value
- **THEN** the system answers 400

#### Scenario: Negative start index

- **WHEN** the owner calls `getRoomProjects` with `startIndex` -1
- **THEN** the system answers 400

#### Scenario: Largest count

- **WHEN** the owner calls `getRoomProjects` with `count` 100 and `startIndex` 0 for a room linked to three projects
- **THEN** the system answers 200 with the three projects

### Requirement: Who is listed among the projects of a room

`getRoomProjects` SHALL list a project only to a caller who may read it: a DocSpace admin, the creator of the
project, or a member who holds a role of their own in it. A project the caller may not read SHALL be left out
of the page and of the total, while `linkedProjectsCount` of the room still counts it.

#### Scenario: Member of one of two projects

- **WHEN** a User who is a Viewer of a Knowledge room and a Content creator of one of the two projects that link it calls `getRoomProjects` for the room
- **THEN** the page holds that project only and the total is 1, while `getRoomInfo` of the room carries `linkedProjectsCount` 2

#### Scenario: Reader of the room outside the projects

- **WHEN** a User who is a Viewer of a Knowledge room linked to two projects, and a member of neither, calls `getRoomProjects` for the room
- **THEN** the page is empty and the total is 0

#### Scenario: Member who reads the room through a project

- **WHEN** a User who is a Content creator of one of the two projects that link an Instructions room, and holds no role in the room, calls `getRoomProjects` for the room
- **THEN** the system answers 200 and the page holds that project only

#### Scenario: DocSpace admin outside the projects

- **WHEN** a DocSpace admin who is a member of neither of the two projects that link a room calls `getRoomProjects` for the room
- **THEN** the page holds both projects and the total is 2

#### Scenario: Creator of a project

- **WHEN** a Room admin creates a project and a Knowledge room, links the room to the project and calls `getRoomProjects` for the room
- **THEN** the page holds the project, although its creator holds no share record on it

### Requirement: Clients are told when the number of projects of a room changes

When `linkProjectSources` links a room, `unlinkProjectSources` unlinks one or `deleteRoom` deletes a project, the
clients of the accounts that may read each room whose `linkedProjectsCount` changed SHALL receive the event that
reports an updated folder for that room, after the change is stored. A call that changes the number of no room
SHALL send no such event for a room.

#### Scenario: Room linked

- **WHEN** a Room manager of a project links a Knowledge room while the owner of the portal listens to the AI rooms section
- **THEN** the owner's client receives the updated-folder event for the room

#### Scenario: Room unlinked

- **WHEN** a Room manager of a project unlinks a linked Instructions room while the owner of the portal listens to the AI rooms section
- **THEN** the owner's client receives the updated-folder event for the room

#### Scenario: Project deleted

- **WHEN** a project that links two rooms is deleted with `deleteRoom` while the owner of the portal listens to the AI rooms section, and the deletion has completed
- **THEN** the owner's client receives the updated-folder event for each of the two rooms

#### Scenario: Nothing linked anew

- **WHEN** a Room manager links a room the project already links, after a first link whose event the owner's client received
- **THEN** the owner's client receives no second updated-folder event for the room

### Requirement: Users only in an AI project

The members of an AI project SHALL be users only: a group SHALL NOT be invited to a project with any role
(403, "The role is not available for this user type"). A Guest SHALL NOT hold any role in a project (403,
nobody added), while a User joins through an invitation link as Content creator.

#### Scenario: Guest invited to a project

- **WHEN** the owner invites a Guest to a project as Content creator by id, by email or through an invitation link
- **THEN** the system answers 403 and the Guest is not a member

#### Scenario: Group invited to a project

- **WHEN** the owner invites a group to a project with any role
- **THEN** the system answers 403 and the group is not a member

## MODIFIED Requirements

### Requirement: Members of the AI rooms

An Instructions room SHALL offer the Room manager, Content creator, Editor and Viewer roles, a Knowledge room
the same without Editor, an AI project only Room manager and Content creator. Every member reads the project;
only a Room manager edits it; a non-member other than a DocSpace admin SHALL NOT read its model, sources or
skills (403).

#### Scenario: Content creator reads, Room manager writes

- **WHEN** a Content creator of the project reads the project model and then sets it
- **THEN** the read answers 200 and the write 403

#### Scenario: DocSpace admin outside the project

- **WHEN** a DocSpace admin who is not a member of a project calls `getProjectModel`, `getProjectSources` and `getRoomSkills` for it
- **THEN** each call answers 200
