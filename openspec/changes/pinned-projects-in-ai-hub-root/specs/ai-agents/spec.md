# Spec Delta

## ADDED Requirements

### Requirement: Pinned projects in the root folders

`getRootFolders` SHALL return in `folders` of the AI rooms section only the AI projects the caller pinned and may
see, sorted as the listing of the section sorts pinned rooms. `total` and `count` of that section SHALL equal their
number and `startIndex` SHALL be 0, whatever paging and filters the request carries. The other sections SHALL keep
their behaviour.

#### Scenario: Only pinned projects are listed

- **WHEN** the owner creates three AI projects and an Instructions room, pins two of the projects and then the Instructions room, and calls `getRootFolders` with `filterType=2` and `count=1`
- **THEN** `folders` of the AI rooms section holds the two pinned projects with `total` and `count` 2 and `startIndex` 0
- **AND** neither the Instructions room nor the unpinned project is in it

#### Scenario: Nothing pinned

- **WHEN** the caller has pinned no AI project, or only an Instructions room
- **THEN** `folders` of the AI rooms section is empty and its `total` is 0, and the section itself is still listed

#### Scenario: Paging and filters do not cut the list

- **WHEN** ten AI projects are pinned, the most the pin limit of the AI rooms section allows, and `getRootFolders` is called with `count=1`, `startIndex=5`, a `filterValue` no project title contains and `sortBy=AZ`
- **THEN** `folders` of the AI rooms section holds all ten projects in the order of their titles, with `total` and `count` 10 and `startIndex` 0

#### Scenario: Unpinned project leaves the list

- **WHEN** a pinned project is unpinned with `unpinRoom` and `getRootFolders` is called again
- **THEN** the project is no longer in `folders` of the AI rooms section and `getRoomsFolder` with `searchArea=AiHub` still lists it

#### Scenario: Other sections are not changed

- **WHEN** the owner has two Custom rooms and a pinned AI project and calls `getRootFolders` with `count=1`
- **THEN** the Rooms section holds one room with `total` 2, as before the change

### Requirement: Who finds a pinned project in the root folders

A pinned AI project SHALL be listed in the root folders when the caller is a DocSpace admin, created the project
or holds a share of their own on it; a share held through a group SHALL NOT count. A pin SHALL be personal: the
pins of other accounts SHALL NOT be listed. A pin on a project the caller may no longer see SHALL be skipped and
kept, so the project is listed again once the caller regains access.

#### Scenario: Member pins a project

- **WHEN** the owner invites a User to a project as Content creator and the User pins it
- **THEN** the project is in `folders` of the AI rooms section of the User's `getRootFolders`
- **AND** it is not in the owner's list, since the owner did not pin it

#### Scenario: Member loses access and regains it

- **WHEN** a member who pinned a project is removed from it and calls `getRootFolders`, and is then invited again
- **THEN** the project is not listed while the member is out of it, and is listed again after the new invitation without a new pin

#### Scenario: DocSpace admin outside the project

- **WHEN** a DocSpace admin who was never invited to a project pins it
- **THEN** the project is in `folders` of the AI rooms section of the admin's `getRootFolders`, as it is in the admin's listing of the section

#### Scenario: Creator of the project

- **WHEN** a Room admin creates a project, pins it and calls `getRootFolders`
- **THEN** the project is listed, although the creator holds no share record on it
