# Spec Delta

## ADDED Requirements

### Requirement: Who is offered the chat sandbox

The chat SHALL offer the tools `sandbox_exec`, `sandbox_read_file`, `sandbox_write_file` and
`sandbox_save_file` to the model of every caller of the chat when the installation configures a chat
sandbox, and SHALL offer none of them otherwise. A call SHALL pause for approval only under the tool
permission mode that asks about every tool. A call made outside a thread SHALL answer the model an error and
reach no sandbox.

#### Scenario: Configured installation

- **WHEN** a user sends a message on an installation that configures a chat sandbox
- **THEN** the model is offered the four tools

#### Scenario: Installation without a chat sandbox

- **WHEN** the owner sends a message on an installation that configures none
- **THEN** none of the four tools is offered

#### Scenario: Default permission mode

- **WHEN** the model calls `sandbox_exec` for a caller whose tool permission mode is the default one
- **THEN** the command runs without a pending tool call

### Requirement: One sandbox per user, one directory per thread

Every sandbox tool call SHALL, unless it is refused first for its arguments or, for a save, for want of a
folder, ask the gateway for the sandbox of the session that is the caller's user id, renew it, and make the
directory `/workspace/{threadId}` in it. Calls of different threads of one user SHALL reach the same
sandbox, each in its own directory. A thread id that is not a name
of ASCII letters, digits, `-` and `_`, 100 characters at most, SHALL answer an error and reach nobody.

#### Scenario: Call in a thread

- **WHEN** the model calls `sandbox_exec` in a thread
- **THEN** the gateway is asked to create a sandbox for the caller's user id, to renew it and to make `/workspace/{threadId}`, and the command runs in that directory

#### Scenario: Another thread of the same user

- **WHEN** the model calls `sandbox_exec` in a second thread of the same user
- **THEN** the gateway is asked for the same session, and the command runs in the second thread's directory

#### Scenario: Thread id that is not a directory name

- **WHEN** the thread id contains `/` or a space, is `..`, or has 101 characters
- **THEN** the model is told that the conversation cannot have a directory and no request is made

#### Scenario: Thread id of the longest length

- **WHEN** the thread id has 100 characters
- **THEN** the command runs in `/workspace/{threadId}`

### Requirement: The sandbox is kept by use and never deleted by the chat

Every sandbox tool call that reaches the gateway SHALL ask for and renew the sandbox with the
installation's idle lifetime, a whole number of 660 to 3000 seconds and 900 by default, plus the command's
timeout for `sandbox_exec`. Nothing else SHALL renew it, and the chat SHALL NOT ask the gateway to delete
it, on a failed call and on thread deletion alike.

#### Scenario: Idle lifetime at the bounds

- **WHEN** the installation sets an idle lifetime of 660 or 3000 seconds
- **THEN** that lifetime is used

#### Scenario: Idle lifetime out of bounds

- **WHEN** the settings name an idle lifetime of 0, a negative number, 600, 659, 3001, a fraction or an empty value, and the environment names none
- **THEN** 900 seconds are used

#### Scenario: Idle lifetime of the environment out of bounds

- **WHEN** the environment names an idle lifetime of 659 seconds or an empty one, and the settings name 1800
- **THEN** 1800 seconds are used

#### Scenario: Default timeout

- **WHEN** the model calls `sandbox_exec` with the default timeout on an installation whose idle lifetime is 900 seconds
- **THEN** the sandbox is asked for and renewed with a lifetime of 960 seconds

#### Scenario: Longer command

- **WHEN** the model calls `sandbox_exec` with `timeout_seconds` 300 on an installation whose idle lifetime is 900 seconds
- **THEN** the sandbox is asked for with a lifetime of 1200 seconds

#### Scenario: Failed command call

- **WHEN** the gateway answers 500 to a command
- **THEN** the gateway is not asked to delete the sandbox

### Requirement: A failed sandbox call is a text for the model

A sandbox tool call SHALL answer the model a text and never fail the reply: that no sandbox is free, that
the sandbox is gone and a new call starts another, or that it cannot be reached. When the portal answers
who the caller is with an error status, the call SHALL answer that status; when its answer names no id, an
internal error. Neither SHALL ask the gateway for anything.

#### Scenario: No sandbox free

- **WHEN** the gateway answers that it has no room for a new sandbox
- **THEN** the model is told that no sandbox is free, and nothing is renewed or run

#### Scenario: Gateway fails

- **WHEN** the gateway answers 500 to the creation, the renewal or the directory
- **THEN** the model is told that the sandbox cannot be reached, and no command runs

#### Scenario: Sandbox gone

- **WHEN** the gateway answers that the sandbox is not there, to its renewal or to a command
- **THEN** the model is told that the sandbox is gone and that a new call starts an empty one

#### Scenario: Sandbox unknown at a write

- **WHEN** the gateway answers 404 to the write of a file
- **THEN** the model is told that the sandbox is gone and that a new call starts an empty one

#### Scenario: Portal does not name the caller

- **WHEN** `GET people/@self` answers 500
- **THEN** the model is told that the workspace answered 500, and the gateway is not called

#### Scenario: Portal names no id

- **WHEN** `GET people/@self` answers 200 without an `id`
- **THEN** the model is told that the call could not be completed due to an internal error, and the gateway is not called

#### Scenario: Reply stopped

- **WHEN** the client stops the reply while a command runs
- **THEN** the call ends and the model is told that it was stopped with the reply

### Requirement: Running a command in the sandbox

`sandbox_exec` SHALL take `command`, a non-blank string, and an optional `timeout_seconds`, a whole number
from 1 to 600, 60 by default. Other arguments SHALL answer an error to the model before any sandbox is
reached. The command SHALL run by a shell in the thread's directory, and the result SHALL carry
its exit code, its stdout and its stderr, each cut at 20000 characters with a note. A command that exits
non-zero is a result, not a failure.

#### Scenario: Command that fails

- **WHEN** the command exits with status 2 and writes to stderr
- **THEN** the result is `exit code: 2` followed by the stderr text

#### Scenario: Output over the cut

- **WHEN** the command writes 20001 characters to stdout
- **THEN** the result carries 20000 of them and a note that the output was cut at 20000 characters per stream

#### Scenario: Blank command

- **WHEN** `command` is missing, blank or not a string
- **THEN** the model is told that a non-empty `command` is required and no sandbox is created or renewed

#### Scenario: Timeout at the bounds

- **WHEN** `timeout_seconds` is 1 or 600
- **THEN** the command runs with that timeout

#### Scenario: Timeout out of bounds

- **WHEN** `timeout_seconds` is 0, -1, null, 601, a fraction or a string
- **THEN** the model is told that it is a whole number from 1 to 600 and no sandbox is created or renewed

### Requirement: Reading and writing a file of the sandbox

`sandbox_read_file` SHALL take `path` without a `..` part, absolute or relative to the thread's directory,
and answer the file's text, or that it is empty, absent, or over 262144 bytes. `sandbox_write_file` SHALL
take such a `path` and `content`, a string of at most 1048576 characters, create missing directories and
replace the file. Other arguments SHALL answer an error before any sandbox is reached.

#### Scenario: Write and read back

- **WHEN** the model writes `/workspace/a/b.txt` and then reads it
- **THEN** the write answers the number of bytes written and the read answers the same text

#### Scenario: Absent file

- **WHEN** the model reads a path that does not exist
- **THEN** the result says that it does not exist

#### Scenario: File over the read limit

- **WHEN** the model reads a file of 262145 bytes
- **THEN** the result says that it is over 262144 bytes and to read a part of it with `sandbox_exec`

#### Scenario: File and content at their limits

- **WHEN** the model reads a file of 262144 bytes, or writes a `content` of 1048576 characters
- **THEN** the read answers the file's text and the write answers 1048576 bytes written

#### Scenario: Empty content

- **WHEN** `content` is an empty string
- **THEN** an empty file is written and the write answers 0 bytes written

#### Scenario: Content over the write limit

- **WHEN** `content` has 1048577 characters
- **THEN** the model is told that it is over 1048576 characters and no sandbox is created or renewed

#### Scenario: Relative path

- **WHEN** the model writes `out/a.txt` and then reads `out/a.txt`
- **THEN** the write names `/workspace/{threadId}/out/a.txt` and the read answers the same text

#### Scenario: Empty or climbing path

- **WHEN** `path` is empty, `../a.txt` or `/workspace/../etc/passwd`
- **THEN** the model is told that a path without `..` is required and no sandbox is created or renewed

### Requirement: Saving a file of the sandbox into the workspace

`sandbox_save_file` SHALL take `path` and an optional `title` of 1 to 165 characters, not `.` or `..` and
without `/`, `\`, control or text-direction characters, the last name of `path` by default. It SHALL save
the file, of 1 to 33554432 bytes, as a new file under the caller's session where a result of
`document_agent` goes, and answer its id, title, size and place. A refusal of the workspace SHALL be answered as "not saved"; any
other failed save as "may have been saved".

#### Scenario: Saved

- **WHEN** the model saves `/workspace/out/report.csv` in a thread outside an AI project, opened in a folder the caller may create in
- **THEN** a new file `report.csv` is created in that folder and the result names its id, its title, 4 bytes for a file of 4 bytes, and that folder

#### Scenario: Saved in a conversation of an AI project

- **WHEN** the model saves `/workspace/out/report.csv` in a thread of an AI project
- **THEN** the file is stored among the artifacts of the thread, no file is created in the folder, and the result says that it is among the artifacts of the thread

#### Scenario: Thread opened in no folder

- **WHEN** the model saves a file in a thread outside an AI project and the round names no folder
- **THEN** the file is created in the caller's My documents and the result names that folder

#### Scenario: Title at the bounds

- **WHEN** `title` has 1 character or 165 characters
- **THEN** the file is saved under that title

#### Scenario: File at the size limit

- **WHEN** the file has 33554432 bytes
- **THEN** it is saved and the result names 33554432 bytes

#### Scenario: Bad title

- **WHEN** `title` is blank, `.`, `..`, has 166 characters, or contains `/`, `\`, a line break, U+007F, U+0085, U+2028, U+202E or U+2066
- **THEN** the model is told what a title is, and neither a folder nor a sandbox is asked for

#### Scenario: No folder to save into

- **WHEN** the round names no folder and the caller has no My documents
- **THEN** the model is told that there is no folder the file could be saved into, and no sandbox is asked for

#### Scenario: Folder cannot be read

- **WHEN** the Files API answers 500 to the read of the round's folder
- **THEN** the model is told that the workspace answered 500, and no sandbox is asked for

#### Scenario: Empty file

- **WHEN** the file has 0 bytes
- **THEN** the result says that it is empty and that no file was saved, and nothing is uploaded

#### Scenario: File absent or too large

- **WHEN** the path does not exist in the sandbox, or the file is over 33554432 bytes
- **THEN** the result says so and that no file was saved

#### Scenario: Workspace refuses

- **WHEN** the workspace answers 403 to the upload
- **THEN** the result says that the workspace refused with 403 and that no file was saved

#### Scenario: Workspace does not answer

- **WHEN** the upload ends in a 5xx or without an answer
- **THEN** the result says that the file may or may not have been saved
