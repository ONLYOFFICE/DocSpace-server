# Spec Delta

## ADDED Requirements

### Requirement: Who is offered the plugin agent

The chat SHALL offer the tools `plugin_agent` and `install_plugin` to the model only when the installation
configures a plugin agent and the caller is the owner or a DocSpace admin. They SHALL NOT be offered to any
other member, on an installation without that configuration, or in a round where the caller's role could not
be read.

#### Scenario: DocSpace admin on a configured installation

- **WHEN** a DocSpace admin sends a message on an installation that configures a plugin agent
- **THEN** the model is offered `plugin_agent` and `install_plugin`

#### Scenario: Room admin or user

- **WHEN** a room admin or a user sends a message on the same installation
- **THEN** neither tool is offered to the model

#### Scenario: Installation without a plugin agent

- **WHEN** the owner sends a message on an installation that configures no plugin agent
- **THEN** neither tool is offered to the model

#### Scenario: Role cannot be read

- **WHEN** a chat round cannot read the caller's role
- **THEN** neither tool is offered for that round

### Requirement: Arguments of a plugin task

`plugin_agent` SHALL take `task`, a string of 1 to 20000 characters that is not blank, and an optional
`sourcesFileId`, a positive integer naming a sources archive of an earlier task. A call with other arguments
SHALL return an error to the model and start no task. The call SHALL pause for approval as a tool of an
approval-required group does, and a denied call SHALL start no task.

#### Scenario: Blank task

- **WHEN** the model calls `plugin_agent` with `task` missing, empty or made of white space only
- **THEN** the call returns an error and no task starts

#### Scenario: Task at and over the length limit

- **WHEN** the model calls `plugin_agent` with a `task` of 20000 characters and then one of 20001
- **THEN** the first call starts a task and the second returns an error without starting one

#### Scenario: Sources id that is not a positive integer

- **WHEN** the model calls `plugin_agent` with `sourcesFileId` 0, a negative number, a fraction or a string
- **THEN** the call returns an error and no task starts

#### Scenario: Sources id left out

- **WHEN** the model calls `plugin_agent` with a valid `task` and no `sourcesFileId`
- **THEN** the task starts a new plugin project

#### Scenario: Call denied

- **WHEN** the caller denies a pending `plugin_agent` call
- **THEN** no task starts and no file is written

### Requirement: Result of a plugin task

A plugin task SHALL take the package only after the project built, passed its type check and passed the
validator of the plugin skill. It SHALL then save two new files, the package `<name>.zip` and the sources
`<name>-src.zip`, in the folder the chat is opened in when the caller may create files there, else in My
documents, and return their ids with the plugin's name, version and direct dependencies. It SHALL install
nothing and change no existing file.

#### Scenario: Plugin built

- **WHEN** an approved `plugin_agent` task ends with a project that passes the three checks
- **THEN** the caller's folder holds the two new files and the call returns their ids, the plugin's name, its version and its direct dependencies
- **AND** the list of the portal's plugins is unchanged

#### Scenario: Folder the caller cannot write to

- **WHEN** the chat is opened in a folder where the caller may not create files
- **THEN** the two files are saved in the caller's My documents

#### Scenario: Sources archive without build output

- **WHEN** the sources archive of a finished task is opened
- **THEN** it holds the project files without the installed dependencies and without the build output

### Requirement: Continuing from a sources archive

With `sourcesFileId` the task SHALL start from the content of that file, read under the caller's rights, and
save its results as new files, as a task for a new plugin does. A file the caller cannot read, one that is
not a zip archive, or one larger than 32 MiB SHALL return an error to the model without starting a task.

#### Scenario: Follow-up task

- **WHEN** the model calls `plugin_agent` with the id of a `<name>-src.zip` an earlier task saved and a `task` asking for a change
- **THEN** the task works on that project and saves a new package and a new sources archive, leaving the earlier files as they are

#### Scenario: File the caller cannot read

- **WHEN** `sourcesFileId` names a file that does not exist or that the caller may not read
- **THEN** the call returns an error and no task starts

#### Scenario: Sources file at and over the size limit

- **WHEN** `sourcesFileId` names a zip archive of exactly 32 MiB and then one a byte larger
- **THEN** the first call starts a task and the second returns an error without starting one

#### Scenario: Not an archive

- **WHEN** `sourcesFileId` names a file that is not a zip archive
- **THEN** the call returns an error and no task starts

### Requirement: Limits and failures of a plugin task

A plugin task SHALL end when its time limit (30 minutes unless the installation sets another) or its budget
of tool calls (150 unless the installation sets another) is spent, and when the reply it belongs to is
stopped. A task that ends without a project that passes the three checks SHALL save no file and return a
failure with its reason to the model. Its sandbox SHALL be removed when the task ends, except when the
service stops while the task runs: then the sandbox expires on its own.

#### Scenario: Checks never pass

- **WHEN** the project still fails the build, the type check or the validator when the budget of tool calls is spent
- **THEN** the call returns a failure naming the last failed check and no file is saved

#### Scenario: Time limit

- **WHEN** a task is still running when its time limit ends
- **THEN** the task is stopped, the call returns a failure and no file is saved

#### Scenario: Reply stopped

- **WHEN** the caller stops the reply while the task runs
- **THEN** the task is stopped, its sandbox is removed and no file is saved

#### Scenario: Sandbox cannot be created

- **WHEN** the sandbox service is unreachable or has no free sandbox when a task starts
- **THEN** the call returns a failure that says the plugin agent is unavailable or busy and no task runs

### Requirement: Concurrent plugin tasks

The service SHALL run at most 2 plugin tasks at a time unless the installation sets another number. A call
that would start one more SHALL return an error that says the plugin agent is busy and start no task; it
SHALL NOT wait for a free place.

#### Scenario: Limit reached

- **WHEN** 2 tasks are running and a third `plugin_agent` call is approved
- **THEN** the third call returns the busy error at once and the two tasks continue

#### Scenario: Place freed

- **WHEN** one of the running tasks ends and `plugin_agent` is called again
- **THEN** the new task starts

### Requirement: Steps of a plugin task

While a plugin task runs, the pending `plugin_agent` call SHALL show the caller the task's steps in the order
they happen, one line for each tool call of the task. The lines SHALL NOT be stored as chat text and SHALL NOT
carry the content of files or the output of commands.

#### Scenario: Steps shown

- **WHEN** a task writes a file and then runs the build
- **THEN** the pending call shows a line for the write and then a line for the build

### Requirement: Installing a built plugin

`install_plugin` SHALL take `fileId`, a positive integer, and SHALL pause for approval on every call: neither
a pre-approval nor a tool permission mode lets it run unasked. Once approved it SHALL read that file under the
caller's rights and upload it with `addWebPluginFromFile` under the caller's session, returning the plugin's
name, version and `enabled` on 200 and the refusal's status and message otherwise. A denied call SHALL
install nothing.

#### Scenario: Approved installation

- **WHEN** a DocSpace admin approves `install_plugin` for the package a task saved
- **THEN** the plugin appears in `getWebPlugins` with `enabled` true and the call returns its name and version

#### Scenario: Denied installation

- **WHEN** the caller denies the pending `install_plugin` call
- **THEN** `getWebPlugins` is unchanged and the reply finishes

#### Scenario: Pre-approval does not apply

- **WHEN** the caller holds an `allowAlways` entry for `install_plugin`, or a tool permission mode that runs tools unasked, and the model calls the tool
- **THEN** the call still pauses for approval

#### Scenario: New version of an installed plugin

- **WHEN** an approved `install_plugin` uploads a package whose `name` is already installed in the portal
- **THEN** the plugin is replaced as a re-upload under the same name is, keeping its stored `settings`

#### Scenario: Portal refuses the package

- **WHEN** `addWebPluginFromFile` answers 400 or 403, such as "Wrong plugin archive. Plugin already exist" or "Forbidden action"
- **THEN** the call returns that status and message to the model and nothing is installed

#### Scenario: File id that is not a positive integer

- **WHEN** the model calls `install_plugin` with `fileId` missing, 0, negative, a fraction or a string
- **THEN** the call returns an error without pausing for approval

#### Scenario: File the caller cannot read

- **WHEN** `fileId` names a file that does not exist or that the caller may not read
- **THEN** the approved call returns an error and nothing is installed

### Requirement: Package registry of a plugin task

The service SHALL answer `GET` and `HEAD` under `/plugin-agent/npm/` with the package metadata and archives of
the public npm registry for requests that carry the token of a running plugin task, keeping fetched archives
for later requests; archive links in the metadata SHALL point back at this route. The route SHALL answer 401
without such a token, 405 for any other method, 404 for a package the registry does not have and 502 while the
registry does not answer and the archive is not kept.

#### Scenario: Install inside a task

- **WHEN** a running task requests the metadata of a package and then the archive the metadata links to
- **THEN** both answer 200 and the archive link names this route, not the public registry

#### Scenario: No token or a wrong one

- **WHEN** a request carries no token or a token no running task holds
- **THEN** the route answers 401

#### Scenario: Token of an ended task

- **WHEN** a request carries the token of a task that has ended
- **THEN** the route answers 401

#### Scenario: Publishing refused

- **WHEN** a request uses `PUT`, `POST` or `DELETE`
- **THEN** the route answers 405 and nothing is sent to the public registry

#### Scenario: Unknown package

- **WHEN** a running task requests a package the public registry does not have
- **THEN** the route answers 404

#### Scenario: Registry does not answer

- **WHEN** the public registry does not answer and the requested archive was not fetched before
- **THEN** the route answers 502

#### Scenario: Kept archive

- **WHEN** an archive that was fetched before is requested while the public registry does not answer
- **THEN** the route answers 200 with the kept archive
