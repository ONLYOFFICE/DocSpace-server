# Spec Delta

## ADDED Requirements

### Requirement: Deleting a profile deletes its project threads

When a profile is deleted (`startRemove`, `deleteMember`, `removeUsers`, or `startReassign` with
`deleteProfile` true), the threads the user created in any AI project, one the user owned included, SHALL be
deleted as by `aiThreadsDelete` by the data job, with their files. Threads of other members SHALL stay. No
file or folder inside a project SHALL pass to another creator, so the new owner of the project or the
recipient of the data SHALL NOT read them.

#### Scenario: Data of a member removed

- **WHEN** the data of a disabled member who has a thread with an artifact and a sent attachment in a project is removed with `startRemove`, the job completes and the background pass has run
- **THEN** both files answer 404 and the project's used space equals what it was before the member's thread

#### Scenario: Owner of the project before the background pass

- **WHEN** the data of a disabled member with a thread in a project is removed with `startRemove` and the job completes
- **THEN** the project's owner reading the member's artifact gets 403 or 404, never 200, and 404 after the background pass

#### Scenario: Member deleted at once

- **WHEN** a disabled member who has a thread with an artifact in a project is deleted with `deleteMember`, and the background pass has run
- **THEN** the artifact answers 404 and the project's used space equals what it was before the member's thread

#### Scenario: Owner of the project deleted

- **WHEN** the data of a disabled room admin who owns a project and has a thread in it is removed with `startRemove`, while a member of the project has a thread too, and the background pass has run
- **THEN** the project passes to the caller, the files of the former owner's thread answer 404, and the member still reads their thread and lists its artifact

#### Scenario: Data transferred with the profile deleted

- **WHEN** a disabled member's data is transferred with `startReassign` and `deleteProfile` true to the project's owner, who then reads the member's artifact
- **THEN** the read answers 403 or 404, never 200, and 404 after the background pass

### Requirement: Transferring data keeps the contents of a project with their creator

`startReassign` with `deleteProfile` false SHALL NOT change the creator of a file or folder inside an AI
project, and SHALL NOT delete the threads the user created in a project the user does not own. Once enabled
again, such a member SHALL still read their thread and its artifact, and the recipient SHALL be refused the
artifact with 403, the project's owner included.

#### Scenario: Member's data transferred to the project's owner

- **WHEN** a disabled member who has a thread with an artifact in a project has their data transferred with `startReassign` and `deleteProfile` false to the project's owner, and the member is enabled again
- **THEN** the member reads the thread and the artifact, and the project's owner reading the artifact gets 403

### Requirement: Transferring a project deletes its former owner's threads

When `startReassign` with `deleteProfile` false hands a project the user owns to the recipient, the threads the
user created in that project SHALL be deleted as by `aiThreadsDelete` by the data job, with their files. The
user gets no access to the project. Threads of other members of the project SHALL stay.

#### Scenario: Owner's data transferred

- **WHEN** the data of a disabled room admin who owns a project and has a thread there with an artifact and a sent attachment is transferred with `startReassign` and `deleteProfile` false, and the background pass has run
- **THEN** the recipient owns the project (its `createdBy`), both files answer 404 and the project's used space equals what it was before the former owner's thread

#### Scenario: Members of the transferred project

- **WHEN** a project whose member has a thread with an artifact is handed over this way
- **THEN** the member still reads their thread and lists its artifact

#### Scenario: Former owner's thread in another project

- **WHEN** the former owner also has a thread with an artifact in a project owned by someone else, where the former owner is a member, and is enabled again after the transfer
- **THEN** the former owner still reads that thread and its artifact
