# Spec Delta

## ADDED Requirements

### Requirement: Who is offered the plugin agent

The chat SHALL offer the tools `plugin_agent` and `install_plugin` to the model only when the installation
configures a plugin agent with its plugin skill and the caller is the owner or a DocSpace admin. They SHALL NOT be offered to any
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

### Requirement: Reading whether the plugin agent is offered

`aiSettingsGetPluginAgent` SHALL return `available`, true exactly when the chat offers the caller
`plugin_agent` and `install_plugin`, and false otherwise without naming a reason. A caller who may not use
AI SHALL get 403.

#### Scenario: Offered

- **WHEN** a DocSpace admin calls `aiSettingsGetPluginAgent` on an installation that configures a plugin agent
- **THEN** the answer is 200 with `available` true

#### Scenario: Not offered

- **WHEN** a room admin calls it on the same installation, or the owner calls it on an installation without a plugin agent
- **THEN** the answer is 200 with `available` false

#### Scenario: AI switched off

- **WHEN** a caller the AI service refuses (a guest, or a portal with AI switched off) calls it
- **THEN** the answer is 403

### Requirement: Arguments of a plugin task

`plugin_agent` SHALL take `task`, a string of 1 to 20000 characters counted without the white space around
it, and an optional `sourcesFileId`, a positive integer naming a sources archive of an earlier task. A call
with other arguments SHALL return an error to the model and start no task. The call SHALL pause for approval
unless the caller's tool permission mode runs every tool unasked or the caller pre-approved the tool, and a
denied call SHALL start no task.

#### Scenario: Blank task

- **WHEN** the model calls `plugin_agent` with `task` missing, empty or made of white space only
- **THEN** the call returns an error and no task starts

#### Scenario: Task at and over the length limit

- **WHEN** the model calls `plugin_agent` with a `task` of 20000 characters, with or without white space around them, and then one of 20001
- **THEN** the first call starts a task and the second returns an error without starting one

#### Scenario: Sources id that is not a positive integer

- **WHEN** the model calls `plugin_agent` with `sourcesFileId` 0, a negative number, a fraction or a string
- **THEN** the call returns an error and no task starts

#### Scenario: Sources id left out

- **WHEN** the model calls `plugin_agent` with a valid `task` and no `sourcesFileId`
- **THEN** the task starts a new plugin project

#### Scenario: Asked under the default mode

- **WHEN** the caller's tool permission mode is the default one, the tool is not pre-approved, and the model calls `plugin_agent`
- **THEN** the call pauses for the caller's approval

#### Scenario: Call denied

- **WHEN** the caller denies a pending `plugin_agent` call
- **THEN** no task starts and no file is written

### Requirement: Result of a plugin task

A plugin task SHALL take the package only after the project built, passed its type check and passed the
validator of the plugin skill, with a package and a sources archive of at most 32 MiB each. It SHALL then
save two new files, the package `<name>.zip` and the sources `<name>-src.zip`, and return their ids with that
name (the project's package name made fit for a file title), the version and the direct dependencies. It
SHALL install nothing and change no existing file.

#### Scenario: Plugin built

- **WHEN** a `plugin_agent` task ends with a project that passes the three checks
- **THEN** the two new files are saved and the call returns their ids, the name, the version and the direct dependencies
- **AND** the list of the portal's plugins is unchanged

#### Scenario: Sources archive without build output

- **WHEN** the sources archive of a finished task is opened
- **THEN** it holds the project files without the installed dependencies and without the build output

#### Scenario: Package over the size limit

- **WHEN** the build leaves a package larger than 32 MiB
- **THEN** the check of the package fails, the model is told so, and nothing is saved

### Requirement: Where the results of a plugin task are saved

The two files SHALL go into the caller's My documents, wherever the chat is opened and whatever
conversation the task runs in; when that section cannot be opened the call SHALL start no task. A save the
workspace refuses SHALL be reported as not saved; a save whose answer cannot be read SHALL be reported as
possibly saved. When only the sources are not saved, the call SHALL still return the package's id with a
warning.

#### Scenario: Chat opened in a room, or in a conversation of an AI project

- **WHEN** the task runs in a chat opened in a room the caller may create files in, or in a conversation of an AI project
- **THEN** both files are saved in the caller's My documents, not in that room and not among the conversation's artifacts

#### Scenario: My documents cannot be opened

- **WHEN** the workspace refuses the caller's My documents section when the tool is called
- **THEN** the call returns an error and no task starts

#### Scenario: Workspace refuses the package

- **WHEN** the workspace answers the save of the package with a 4xx
- **THEN** the call returns that the plugin was built and no file was saved

#### Scenario: Answer to saving the package cannot be read

- **WHEN** the workspace answers the save of the package with a 5xx, an unreadable body or not in time
- **THEN** the call returns that the file may or may not have been saved

#### Scenario: Sources not saved

- **WHEN** the package is saved and the save of the sources fails
- **THEN** the call returns the package's id, no sources id and a warning

### Requirement: Continuing from a sources archive

With `sourcesFileId` the task SHALL start from the content of that file, read under the caller's rights, and
save its results as new files, as a task for a new plugin does. A file the caller cannot read, one whose name
does not end in `.zip` or whose content is not a zip archive, or one larger than 32 MiB SHALL return an error
to the model without starting a task.

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

- **WHEN** `sourcesFileId` names a file with another extension, or a `.zip` file whose content is not a zip archive
- **THEN** the call returns an error and no task starts

#### Scenario: Archive that does not unpack

- **WHEN** `sourcesFileId` names a zip archive that cannot be unpacked
- **THEN** the call returns an error, no task runs and the sandbox is removed

### Requirement: Limits and failures of a plugin task

A plugin task SHALL end when its time limit (30 minutes unless the installation sets another) or its budget
of tool calls (150 unless set otherwise) is spent, when its reply is stopped, and when the model's provider
fails a request. A task that ends without a passing project SHALL save no file and return a failure with its
reason. Its sandbox SHALL be removed when the task ends, unless the service stops first or the sandbox
service refuses the removal: it then expires on its own.

#### Scenario: Checks never pass

- **WHEN** the project still fails the build, the type check or the validator when the budget of tool calls is spent
- **THEN** the call returns a failure naming the last failed check and no file is saved

#### Scenario: Time limit

- **WHEN** a task is still running when its time limit ends
- **THEN** the task is stopped, the call returns a failure and no file is saved

#### Scenario: Reply stopped

- **WHEN** the caller stops the reply while the task runs
- **THEN** the task is stopped, its sandbox is removed and no file is saved

#### Scenario: Provider fails a request

- **WHEN** the model's provider fails a request of the task
- **THEN** the task ends, the call returns that the provider failed and no file is saved

#### Scenario: Model stops without a plugin

- **WHEN** the model answers three times in a row without calling a tool
- **THEN** the task ends, the call returns that the agent stopped and no file is saved

#### Scenario: Sandbox cannot be created

- **WHEN** the sandbox service is unreachable or has no free sandbox when a task starts
- **THEN** the call returns a failure that says the plugin agent is unavailable or busy and no task runs

### Requirement: Concurrent plugin tasks

Each instance of the service SHALL run at most 2 plugin tasks at a time, whatever portal they belong to,
unless the installation sets another number. A call that would start one more SHALL return an error that says
the plugin agent is busy and start no task; it SHALL NOT wait for a free place.

#### Scenario: Limit reached

- **WHEN** 2 tasks are running in an instance and it gets a third `plugin_agent` call
- **THEN** the third call returns the busy error at once and the two tasks continue

#### Scenario: Place freed

- **WHEN** one of the running tasks ends and `plugin_agent` is called again
- **THEN** the new task starts

### Requirement: Steps of a plugin task

While a plugin task runs, the pending `plugin_agent` call SHALL show the caller the task's steps in the order
they happen, one line for each tool call: the path the call is about, the start of the first line of a
command, or a fixed line for the hand-over. The lines SHALL NOT be stored as chat text and SHALL NOT carry a
file's content or a command's output.

#### Scenario: Steps shown

- **WHEN** a task writes a file and then runs the build
- **THEN** the pending call shows a line with the file's path and then a line with the build command

#### Scenario: Hand-over

- **WHEN** a task hands the plugin over
- **THEN** the pending call shows the line "Checking and packaging the plugin"

### Requirement: Installing a built plugin

`install_plugin` SHALL take `fileId`, a positive integer, and SHALL install only on a call the caller was
asked about. Under the tool permission mode that runs every tool unasked it SHALL install nothing; with the
tool pre-approved it SHALL install nothing and withdraw the pre-approval, so the next call asks.
Otherwise the call pauses for approval and, once approved, uploads the file with `addWebPluginFromFile` under
the caller's session and returns the plugin's name, version and `enabled`.

#### Scenario: Approved installation

- **WHEN** a DocSpace admin approves `install_plugin` for the package a task saved, without pre-approving the tool
- **THEN** the plugin appears in `getWebPlugins` with `enabled` true and the call returns its name and version

#### Scenario: Denied installation

- **WHEN** the caller denies the pending `install_plugin` call
- **THEN** `getWebPlugins` is unchanged and the reply finishes

#### Scenario: Permission mode that asks nothing

- **WHEN** the caller's tool permission mode runs every tool unasked and the model calls `install_plugin`
- **THEN** nothing is installed and the call returns that the mode has to be changed to one that asks

#### Scenario: Pre-approved tool

- **WHEN** the caller's mode skips the dialog for pre-approved tools, the caller holds an `allowAlways` entry for `install_plugin`, and the model calls it
- **THEN** nothing is installed, the entry is removed, and the call returns that the tool has to be called again

#### Scenario: Approved with "always allow"

- **WHEN** the caller approves the pending call with `allowAlways` under that mode
- **THEN** nothing is installed by that call, the entry is removed, and the next `install_plugin` call pauses for approval

#### Scenario: Mode that asks for every call

- **WHEN** the caller's mode asks for every tool call and the caller approves `install_plugin`, with or without an `allowAlways` entry for it
- **THEN** the plugin is installed

#### Scenario: New version of an installed plugin

- **WHEN** an approved `install_plugin` uploads a package whose `name` is already installed in the portal
- **THEN** the plugin is replaced as a re-upload under the same name is, keeping its stored `settings`

#### Scenario: File id that is not a positive integer

- **WHEN** the model calls `install_plugin` with `fileId` missing, 0, negative, a fraction or a string
- **THEN** the call returns an error and nothing is installed

#### Scenario: File that is not a readable archive

- **WHEN** `fileId` names a file that does not exist, that the caller may not read, that is larger than 32 MiB, whose name does not end in `.zip` or whose content is not a zip archive
- **THEN** the call returns an error and nothing is installed

### Requirement: Refusals and unknown answers of an installation

When `addWebPluginFromFile` answers 400, `install_plugin` SHALL return the status and the portal's message
and say that nothing was installed. When it answers 403, the call SHALL return the status and the message
without saying whether the plugin was installed: the portal answers 403 also after an installation whose
domains overflow the Content Security Policy header. After a 5xx, an unreadable answer or none in time, the
call SHALL say that the plugin may have been installed.

#### Scenario: Portal refuses the package

- **WHEN** `addWebPluginFromFile` answers 400 "Wrong plugin archive. Plugin already exist"
- **THEN** the call returns that status and message and that nothing was installed

#### Scenario: Portal answers 403

- **WHEN** `addWebPluginFromFile` answers 403 "Forbidden action"
- **THEN** the call returns that status and message and that it is not known whether the plugin was installed

#### Scenario: Portal's answer cannot be read

- **WHEN** `addWebPluginFromFile` answers 5xx, a 200 whose body cannot be read, or nothing in time
- **THEN** the call returns that the plugin may have been installed

### Requirement: Package registry of a plugin task

Where the installation names this service's address for sandboxes, the service SHALL answer `GET` and
`HEAD` under `/plugin-agent/npm/` with metadata and archives of the configured npm registry, for
requests that carry the token of a running plugin task, and keep fetched archives. Archive links to that
registry SHALL point at this route. It SHALL answer 401 without such a token or address and, with both,
405 for another method, 404 for a path or package the registry lacks, and 502 otherwise.

#### Scenario: Install inside a task

- **WHEN** a running task requests the metadata of a package and then the archive the metadata links to
- **THEN** both answer 200 and the archive link names this route, not the registry

#### Scenario: No token or a wrong one

- **WHEN** a request of any method carries no token or a token no running task holds
- **THEN** the route answers 401

#### Scenario: Token of an ended task

- **WHEN** a request carries the token of a task that has ended
- **THEN** the route answers 401

#### Scenario: Installation names no address of the service

- **WHEN** a request carries the token of a running task and the installation names no address of this service for sandboxes
- **THEN** the route answers 401 and nothing is sent to the registry

#### Scenario: Publishing refused

- **WHEN** a request with the token of a running task uses `PUT`, `POST` or `DELETE`
- **THEN** the route answers 405 and nothing is sent to the registry

#### Scenario: Unknown package or path

- **WHEN** a running task requests a package the registry does not have, or a path that names neither a package nor an archive
- **THEN** the route answers 404

#### Scenario: Registry does not give the package

- **WHEN** the registry does not answer, answers anything but 200 and 404, redirects to another address, or sends more than 64 MiB of metadata or 128 MiB of archive, and the archive was not fetched before
- **THEN** the route answers 502 and no other address is asked

#### Scenario: Kept archive

- **WHEN** an archive that was fetched before is requested while the registry does not answer
- **THEN** the route answers 200 with the kept archive

## MODIFIED Requirements

### Requirement: Pre-approving tools

`aiToolsSetAllowAlways`, `aiToolsGetAllowAlways` and `aiToolsIsAllowAlways` SHALL keep, per user, the flat
`serverType_toolName` pre-approvals: setting is idempotent, revoking one entry touches no other, a falsy,
missing or string value grants nothing (400, never 500), and a grant for one server type never answers for
another. The list SHALL be independent of the disabled list; `allowAlways` on an approval lands in the same
list under the server-prefixed key, where the read finds it.

#### Scenario: One user's approval

- **WHEN** a member pre-approves a tool and the owner reads their own list
- **THEN** the owner's list does not carry it

#### Scenario: Approval with allowAlways

- **WHEN** the owner approves a pending call of a tool other than `install_plugin` with `allowAlways` true and the model calls the same tool again
- **THEN** the second call runs without a pause and `aiToolsIsAllowAlways` answers true for it

#### Scenario: Same name on another server

- **WHEN** the owner pre-approves a host tool `delete_file`
- **THEN** the DocSpace `delete_file` still pauses for approval

#### Scenario: Pre-approval of install_plugin does not last

- **WHEN** a DocSpace admin pre-approves `install_plugin`, through `aiToolsSetAllowAlways` or with `allowAlways` on an approval, and the tool is called under the mode that skips the dialog for pre-approved tools
- **THEN** that call installs nothing and `aiToolsIsAllowAlways` answers false for the tool afterwards
