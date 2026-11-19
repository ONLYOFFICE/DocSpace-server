# Spec Delta

## ADDED Requirements

### Requirement: Converting a member to Guest removes them from the projects

When `startUserTypeUpdate` converts a user to Guest, the job SHALL remove the user's role in every AI project,
one the user owned and passed on included, and SHALL delete the threads the user created in AI projects as by
`aiThreadsDelete`, with their files. Threads of other members SHALL stay. In an AI agent the user SHALL stay a
Viewer.

#### Scenario: Member converted

- **WHEN** a User who is a Content creator of a project and has a thread there with an artifact and a sent attachment is converted to Guest, the job completes and the background pass has run
- **THEN** `getRoomSecurityInfo` of the project no longer lists the user, both files answer 404 and the project's used space equals what it was before the user's thread

#### Scenario: Thread closed when the job completes

- **WHEN** a User who reads their thread in a project is converted to Guest, the job completes and the user is converted back to User
- **THEN** the thread answers 404 to the user

#### Scenario: Other members

- **WHEN** one of two members with a thread each is converted to Guest
- **THEN** the other member still reads their thread and lists its artifact

#### Scenario: Owner of a project converted

- **WHEN** a Room admin who owns a project and has a thread there with an artifact is converted to Guest with the project passed to another account, and the background pass has run
- **THEN** the other account owns the project (its `createdBy`), `getRoomSecurityInfo` does not list the converted user, and the artifact answers 404

#### Scenario: Member of an agent converted

- **WHEN** a User who is a Content creator of an AI agent is converted to Guest
- **THEN** `getRoomSecurityInfo` of the agent lists the user as Viewer
