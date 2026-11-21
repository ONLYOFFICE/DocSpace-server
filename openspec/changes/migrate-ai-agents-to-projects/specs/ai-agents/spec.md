# Spec Delta

## ADDED Requirements

### Requirement: Conversion of an AI agent into an AI project

An AI agent of an active portal SHALL be converted with no request from a user: an AI project is created with
the agent's title, owner, colour, cover, logo and custom tags, and the agent stops being a room of the AI room
type. An agent SHALL be converted once, and an agent the conversion has not reached yet SHALL stay unchanged.

#### Scenario: Agent converted

- **WHEN** the conversion has run for a portal that holds one agent
- **THEN** `getRoomsFolder` of the AI rooms section lists a project with the agent's title whose `createdBy` is the agent's owner, and no room of the AI room type is listed in any section

#### Scenario: Conversion run twice

- **WHEN** the conversion runs a second time for the same portal
- **THEN** the AI rooms section still lists one project for the agent

#### Scenario: Pinned agent

- **WHEN** a member had pinned the agent and the conversion has run
- **THEN** the project is pinned for that member

### Requirement: The room of a converted agent becomes a Knowledge room

An agent whose Knowledge folder holds at least one file SHALL keep its id as a Knowledge room linked to the
project and titled `<agent title> (<title of its Knowledge folder>)`; a title over the length limit of a room
title SHALL be cut inside the agent title. The knowledge files SHALL keep their ids and lie directly in the
room, and the Knowledge folder SHALL be gone. The room SHALL be made also while vectorization is off.

#### Scenario: Knowledge kept

- **WHEN** the conversion has run for an agent "Sales bot" with one knowledge file
- **THEN** `getRoomInfo` with the agent's id answers a Knowledge room titled "Sales bot (Knowledge)", `getProjectSources` of the project lists that room, `getFileInfo` of the file answers the same file id with the room as its `folderId`, and the id of the former Knowledge folder answers 404

#### Scenario: Title at the limit

- **WHEN** the agent's title has the greatest length a room title may have
- **THEN** the title of the Knowledge room has that same length and ends with ` (Knowledge)`

#### Scenario: Vectorization off

- **WHEN** the conversion runs for an agent with a knowledge file on a portal where vectorization is turned off
- **THEN** the Knowledge room is made and linked all the same

### Requirement: An agent without knowledge leaves no room

An agent whose Knowledge folder holds no file SHALL be deleted once its project exists: the project has no linked
room and the agent's id answers 404.

#### Scenario: Empty Knowledge folder

- **WHEN** the conversion has run for an agent whose Knowledge folder is empty
- **THEN** `getProjectSources` of the project answers an empty list and `getRoomInfo` with the agent's id answers 404

### Requirement: Threads of an agent become threads of the project

Every thread started with the agent as `entityId` SHALL become a thread of the project with its messages,
whoever created it. `aiThreadsList` SHALL list it to its creator for the project's id and no longer for another
`entityId` or without one.

#### Scenario: Thread follows the agent

- **WHEN** a member has a thread with messages in an agent and the conversion has run
- **THEN** `aiThreadsList` with the project's id lists the thread to that member, `aiThreadsList` without an entity does not, and `aiThreadsReadMessages` of the thread answers the same messages

#### Scenario: Thread started elsewhere

- **WHEN** the same member has a thread started without an entity
- **THEN** that thread is still listed without an entity and is not listed for the project

### Requirement: The model of an agent becomes the model of the project

The Chat assignment scoped to the agent SHALL become the model of the project: `getProjectModel` answers the
profile the agent was bound to, and null when the agent had no assignment of its own.

#### Scenario: Agent bound to a profile

- **WHEN** the agent carried a Chat assignment to profile A while the portal's Chat binding is B, and the conversion has run
- **THEN** `getProjectModel` of the project answers A

#### Scenario: Agent without an assignment

- **WHEN** the agent carried no Chat assignment of its own
- **THEN** `getProjectModel` of the project answers null

### Requirement: Members of a converted agent

The project SHALL take the agent's members as users only: a Room manager stays Room manager, a Content creator
stays Content creator, a Viewer becomes Content creator. Each member of a group invited to the agent, as the
group is at the conversion, SHALL become a Content creator unless the user holds a role of their own in the
agent. A Guest and an invitation link SHALL NOT be carried over.

#### Scenario: Roles carried over

- **WHEN** an agent with a Room manager, a Content creator and a Viewer is converted
- **THEN** `getRoomSecurityInfo` of the project lists the first as Room manager and the other two as Content creator

#### Scenario: Group invited to the agent

- **WHEN** a group of two Users was invited to the agent as Viewer, one of the two being also a Room manager of the agent by their own role
- **THEN** `getRoomSecurityInfo` of the project lists no group, the first User as Content creator and the second as Room manager

#### Scenario: Joining the group later

- **WHEN** a User joins that group after the conversion
- **THEN** the User is not a member of the project

#### Scenario: Guest of the agent

- **WHEN** a Guest was a Viewer of the agent
- **THEN** `getRoomSecurityInfo` of the project does not list the Guest

### Requirement: Access to the Knowledge room of a converted agent

In the Knowledge room made from an agent, the users and groups that were Room manager or Content creator of the
agent SHALL keep that role, a Guest included. Viewers and invitation links SHALL be removed from the room: a Viewer
reads it through the project only. An external link of a knowledge file SHALL be removed.

#### Scenario: Content creator keeps the room

- **WHEN** an agent with a Room manager and a Content creator is converted
- **THEN** `getRoomSecurityInfo` of the Knowledge room lists both with the role they had in the agent

#### Scenario: Viewer reads through the project

- **WHEN** a Viewer of the agent reads a knowledge file after the conversion
- **THEN** the read answers 200 and `getRoomSecurityInfo` of the Knowledge room does not list that user

#### Scenario: Guest of the agent

- **WHEN** a Guest who was a Viewer of the agent reads a knowledge file after the conversion
- **THEN** the read answers 403

#### Scenario: External link of a knowledge file

- **WHEN** a knowledge file had an external link and the conversion has run
- **THEN** the link is no longer listed among the links of the file

### Requirement: Chat outputs of a converted agent move to My documents

The Chat outputs folder of an agent SHALL move, with its content and the ids of its files, into the My documents
of the agent's owner as an ordinary folder titled `<agent title> (<title of the Chat outputs folder>)`; an empty
one SHALL be deleted. Its size SHALL be added to that user's used space with no check of the quota. If the
owner's account is deleted or disabled, the portal owner SHALL take the folder.

#### Scenario: Outputs moved

- **WHEN** the conversion has run for an agent "Sales bot" whose Chat outputs folder holds a file
- **THEN** `getMyFolder` of the agent's owner lists a folder "Sales bot (Chat outputs)", `getFileInfo` of the file answers the same file id inside that folder, and another member of the agent reading the file gets 403

#### Scenario: Empty Chat outputs

- **WHEN** the Chat outputs folder of the agent holds nothing
- **THEN** `getMyFolder` of the agent's owner lists no folder for the agent

#### Scenario: Owner over the quota

- **WHEN** the files of Chat outputs are larger than the free space left in the personal quota of the agent's owner
- **THEN** the conversion completes, the folder is in the owner's My documents and the owner's used space is above the quota

#### Scenario: Owner disabled

- **WHEN** the account of the agent's owner is disabled
- **THEN** the folder is listed by `getMyFolder` of the portal owner

### Requirement: The background pass converts agents by itself

A background pass SHALL convert the agents of every active portal changed since its previous pass, and of every
active portal on its first pass. A portal restored or transferred from an archive that holds agents SHALL be
converted by the first pass that starts after the portal became active; until that pass it holds agents. A
portal that is not active SHALL be skipped.

#### Scenario: Restored archive

- **WHEN** an archive made while the portal held an agent is restored and a pass has started after the restore completed
- **THEN** the restored portal holds a project for the agent and no room of the AI room type

#### Scenario: Portal being restored

- **WHEN** a pass runs while a portal is still being restored
- **THEN** the pass changes nothing in that portal and a later pass converts it

#### Scenario: Unchanged portal

- **WHEN** a pass runs and a portal without agents was not changed since the previous pass
- **THEN** the pass reads nothing of that portal

### Requirement: A failed conversion is retried and then left

An agent whose conversion fails SHALL stay an agent and SHALL NOT stop the other agents of the portal. The pass
SHALL try the portal again on its next passes; after the configured number of passes that ended with a failed
agent it SHALL leave the portal alone, and no route resumes it.

#### Scenario: One agent of two fails

- **WHEN** the conversion of one of two agents of a portal fails
- **THEN** the other agent is converted and the failed one is still a room of the AI room type

#### Scenario: Failure repeated

- **WHEN** the same agent fails on every pass up to the configured number
- **THEN** the following passes do not read the portal

#### Scenario: Success after a failure

- **WHEN** an agent that failed on one pass is converted on the next
- **THEN** the portal is no longer retried and its count of failed passes starts from zero

### Requirement: Starting the conversion by request

`POST internal/ai/agents/migration`, an internal route kept for the integration tests, SHALL convert the agents
of the caller's portal before it answers and answer 200 with `migrated` and `failed`, the numbers of agents
converted and failed by this call. It SHALL need the portal owner or a DocSpace admin (other users 403, no
sign-in 401) and work while the AI switch is off. A repeated call converts nothing again.

#### Scenario: Owner starts the conversion

- **WHEN** the portal owner calls the route for a portal with two agents
- **THEN** the call answers 200 with `migrated` 2 and `failed` 0

#### Scenario: Portal without agents

- **WHEN** the route is called a second time, or for a portal that never held an agent
- **THEN** the call answers 200 with `migrated` 0 and `failed` 0

#### Scenario: Room admin refused

- **WHEN** a Room admin or a User calls the route
- **THEN** the system answers 403 and no agent is converted

#### Scenario: AI switched off

- **WHEN** the portal owner calls the route while the AI switch of the portal is off
- **THEN** the call answers 200 and the agents are converted
