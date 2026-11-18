# Spec Delta

## ADDED Requirements

### Requirement: Files of a project thread

A thread whose entry is an AI project SHALL keep two sets of files inside that project: its artifacts, saved
by the chat tools, and its attachments, the files sent with its messages. Each thread SHALL have sets of its
own, never shared with another thread of the project, and the thread itself SHALL NOT report where they are
stored. A thread of any other entry has neither set.

#### Scenario: Two threads of one project

- **WHEN** the creator saves an artifact into each of two threads of one project
- **THEN** `getThreadArtifacts` of each thread lists its own file only, and the two files report different `folderId` values

#### Scenario: Artifacts saved at the same moment

- **WHEN** three artifacts are saved at once into a thread that has none yet
- **THEN** the three files report one `folderId` and the thread lists three artifacts

#### Scenario: The thread does not name its folders

- **WHEN** the creator reads a thread that has an artifact
- **THEN** the answer carries no `folderId`

### Requirement: Saving an artifact of a thread

The storage route `POST internal/ai/threads/{id}/artifacts` SHALL save the sent file among the artifacts of
the caller's project thread and answer with the stored file. It SHALL answer 400 for a thread that is not in
an AI project, for an empty file and for a file over 100 MB, 404 for an unknown thread and 403 for a thread
of another user, storing nothing. A title that an artifact of the thread already has SHALL NOT replace that
artifact: the new file is stored under another title.

#### Scenario: Saved artifact is listed

- **WHEN** the creator saves `report.txt` into a thread of a project
- **THEN** the answer is the file titled `report.txt`, and `getThreadArtifacts` lists it with total 1

#### Scenario: Same title twice

- **WHEN** the creator saves `report.txt` into one thread twice
- **THEN** the thread lists two artifacts with different ids and different titles

#### Scenario: Thread outside a project

- **WHEN** the creator saves a file into a thread that has no entry
- **THEN** the system answers 400

#### Scenario: Empty file

- **WHEN** the creator saves a file of 0 bytes into a thread of a project
- **THEN** the system answers 400 and the thread lists no artifacts

#### Scenario: File over the limit

- **WHEN** the creator saves a file of 100 MB and one byte into a thread of a project
- **THEN** the system answers 400 and the thread lists no artifacts

#### Scenario: Unknown thread

- **WHEN** a file is saved into a thread id that does not exist
- **THEN** the system answers 404

#### Scenario: Thread of another member

- **WHEN** a member of the project saves a file into a thread the project's owner created
- **THEN** the system answers 403 and the thread still lists no artifacts

### Requirement: Listing the files of a thread

`getThreadArtifacts` and `getThreadAttachments` SHALL answer to the creator of a thread of the given AI project
one page of its files with the total, an empty page when it has none. They SHALL answer 404 for a thread of
another user, the project's owner included, for a thread of another project and for a deleted thread, and
403 to a caller outside the project. `count` SHALL be 1 to 100, 100 when omitted, and `startIndex` 0 or more;
any other value answers 400.

#### Scenario: Thread without files

- **WHEN** the creator lists the artifacts of a thread nothing was saved into
- **THEN** the page is empty and the total is 0

#### Scenario: One page

- **WHEN** the creator lists two artifacts with `count` 1 and `startIndex` 1
- **THEN** the page holds one file and the total is 2

#### Scenario: Project owner asks for a member's thread

- **WHEN** a member has an artifact and a sent attachment in a thread, and the project's owner calls `getThreadArtifacts` and `getThreadAttachments` for it
- **THEN** the member's own calls list the files and both calls of the owner answer 404

#### Scenario: Thread of another project

- **WHEN** the creator names the thread together with the id of another project they own
- **THEN** the system answers 404

#### Scenario: Caller outside the project

- **WHEN** a User who is not a member of the project calls `getThreadArtifacts`
- **THEN** the system answers 403

#### Scenario: Attachment that was not sent

- **WHEN** the creator uploads a file into the project and attaches it without sending a message
- **THEN** `getThreadAttachments` answers an empty page with total 0

#### Scenario: Count of zero

- **WHEN** the creator calls either list with `count` 0
- **THEN** the system answers 400

#### Scenario: Count above the maximum

- **WHEN** the creator calls either list with `count` 101
- **THEN** the system answers 400

#### Scenario: Negative start index

- **WHEN** the creator calls either list with `startIndex` -1
- **THEN** the system answers 400

#### Scenario: Largest count

- **WHEN** the creator calls either list with `count` 100 and `startIndex` 0
- **THEN** the system answers 200

### Requirement: Uploading an attachment into a project

A caller who may chat in an AI project SHALL upload a file into the project itself through a chunked session
opened by `createUploadSessionInFolder` with the project's id. The session SHALL be refused with 403 for a
caller outside the project and for a `relativePath`, and with 400 for a file that is neither a picture nor a
document text can be read from. `uploadFile` into a project SHALL answer 403.

#### Scenario: Member uploads

- **WHEN** a Content creator of the project uploads `notes.txt` through a chunked session opened in the project
- **THEN** the stored file reports the project's id as `folderId`

#### Scenario: Caller outside the project

- **WHEN** a User who is not a member opens a session in the project
- **THEN** the system answers 403

#### Scenario: Relative path

- **WHEN** the owner opens a session in the project with `relativePath` `nested`
- **THEN** the system answers 403

#### Scenario: Unsupported format

- **WHEN** the owner opens a session in the project for `data.bin`
- **THEN** the system answers 400

#### Scenario: Upload in one request

- **WHEN** the owner sends `notes.txt` to the project with `uploadFile`
- **THEN** the system answers 403

### Requirement: Copying a portal file into a project

`copyBatchItems` with an AI project as the destination SHALL copy files for a caller who may chat in the
project, storing each copy in the project as a new file whatever `conflictResolveType` is sent. It SHALL answer
403 to a caller outside the project and when the items include a folder, and 415 for a file that is neither a
picture nor a document text can be read from. `moveBatchItems` into a project SHALL answer 403.

#### Scenario: Member copies

- **WHEN** a Content creator of the project copies a file of their My documents into the project
- **THEN** the copy reports the project's id as `folderId` and an id of its own

#### Scenario: Same file twice

- **WHEN** the owner copies one file into the project twice, both times with the overwrite conflict type
- **THEN** the project holds two copies with different ids and different titles

#### Scenario: Caller outside the project

- **WHEN** a User who is not a member copies a file of their My documents into the project
- **THEN** the system answers 403

#### Scenario: Folder among the items

- **WHEN** the owner copies a folder of their My documents into the project
- **THEN** the system answers 403

#### Scenario: Unsupported format

- **WHEN** the owner copies `data.bin` into the project
- **THEN** the system answers 415

#### Scenario: Move

- **WHEN** the owner moves a file of their My documents into the project
- **THEN** the system answers 403

### Requirement: Attachment of a file stored in a project

`aiAttachmentsSaveFilesMany` for a file the caller stored in an AI project SHALL answer one attachment per
file: a repeated call for the same file SHALL return the same attachment, with the extracted text for a
document and as an image attachment for a picture.

#### Scenario: Document attached twice

- **WHEN** the owner uploads `notes.txt` into the project and attaches it twice
- **THEN** both answers carry one attachment id, the kind `file`, the file's id as `entryId` and the text of the file

#### Scenario: Picture attached twice

- **WHEN** the owner uploads a PNG picture into the project and attaches it twice
- **THEN** both answers carry one attachment id and the kind `image`

### Requirement: A sent attachment is kept with its thread

`aiAttachmentsLinkToMessage` for a message of a project thread SHALL move each linked file that the caller
stored in the project into the attachments of that thread, where `getThreadAttachments` lists it. A copy
linked this way SHALL stay after the file it was copied from is deleted. Linking in a thread outside a
project SHALL leave the file where it is.

#### Scenario: Linked file is listed

- **WHEN** the creator uploads a file into the project, attaches it and links the attachment to a message of a project thread
- **THEN** the link answers 204, `getThreadAttachments` lists that file with total 1, and its `folderId` is no longer the project's id

#### Scenario: Copy outlives the original

- **WHEN** the creator copies a file of their My documents into the project, links the copy to a message and deletes the original
- **THEN** `getThreadAttachments` still lists the copy

#### Scenario: Two messages of one thread

- **WHEN** the creator sends one file with each of two messages of a thread
- **THEN** the thread lists both attachments and they report one `folderId`

#### Scenario: Threads of one project

- **WHEN** the creator sends a file in each of two threads of one project
- **THEN** each thread lists its own attachment only, under different `folderId` values

#### Scenario: Thread outside a project

- **WHEN** the owner links an attachment of a file in their My documents to a message of a thread that has no entry
- **THEN** the link answers 204 and the file still reports My documents as `folderId`

### Requirement: Privacy of the contents of a project

A file or folder inside an AI project (an artifact, an attachment, a file not sent yet) SHALL be readable by
its creator only: `getFileInfo`, `getPresignedFileUri`, `copyBatchItems`, `bulkDownload` and `getFolderInfo`
SHALL answer 403 to another member, to the project's owner and to a DocSpace admin. The creator SHALL read,
download and copy it and nothing else: `updateFile`, `deleteFile`, `moveBatchItems`, an external link and
`insertFile` SHALL answer 403.

#### Scenario: Artifact of another member

- **WHEN** another member of the project, the project's owner and a DocSpace admin each read a member's artifact, ask for its download link, copy it into their My documents and request it as an archive
- **THEN** every call answers 403

#### Scenario: Folder of another member's thread

- **WHEN** another member, the project's owner and a DocSpace admin each call `getFolderInfo` with the `folderId` of a member's artifact
- **THEN** the system answers 403

#### Scenario: File that was not sent

- **WHEN** a member uploads a file into the project, and another member, the project's owner and a DocSpace admin each read it
- **THEN** the system answers 403

#### Scenario: Creator reads, downloads and copies

- **WHEN** the creator reads an own artifact, asks for its download link and copies it into their My documents
- **THEN** every call succeeds and the copy reports My documents as `folderId`

#### Scenario: Creator cannot change the file

- **WHEN** the creator renames an own artifact, deletes it, moves it into their My documents, creates an external link to it or inserts a file into its folder
- **THEN** every call answers 403

#### Scenario: Creator opens the editor

- **WHEN** the creator calls `openEditFile` for an own artifact
- **THEN** the document opens with the edit permission false

### Requirement: The contents of a project are not listed

`getFolderByFolderId` of an AI project SHALL answer no files, no folders and total 0, `getFolders` an empty
list, and a search inside the project nothing, to the creator of the files, the project's owner and a DocSpace
admin alike. `getRoomInfo` of a project SHALL report `filesCount` and `foldersCount` 0. `getProjectSources`
and `getRoomSkills` of the project SHALL answer as they do for a project without files.

#### Scenario: Listing a project that holds thread files

- **WHEN** the creator of the files, the project's owner and a DocSpace admin each call `getFolderByFolderId` and `getFolders` for a project with an artifact and a file not sent yet
- **THEN** every answer holds no files and no folders, with total 0

#### Scenario: Search inside the project

- **WHEN** the creator of an artifact `report.txt` calls `getFolderByFolderId` of the project with `filterValue` `report` and `withSubFolders` true
- **THEN** the answer holds no files and total 0

#### Scenario: Counters of the project

- **WHEN** the owner reads `getRoomInfo` of a project that holds thread files
- **THEN** `filesCount` and `foldersCount` are 0

#### Scenario: Sources and skills

- **WHEN** the owner links an Instructions room to a project that holds thread files
- **THEN** `getProjectSources` lists that room and `getRoomSkills` of the project answers

### Requirement: Recipients of an upload into a project

The socket event that announces a file uploaded into an AI project SHALL reach the uploader only: the
project's owner, a DocSpace admin and another member subscribed to the project SHALL NOT receive it. The
upload SHALL NOT mark the file as new for the project's owner, and the members of the project SHALL NOT be
sent the notification about a file uploaded to a room. `getSharedUsers` of a file inside a project SHALL
answer an empty list.

#### Scenario: Uploader is told

- **WHEN** a member subscribed to the project uploads a file into it
- **THEN** the member receives the event that the file was created

#### Scenario: Others are not told

- **WHEN** a member uploads a file into the project while the project's owner, a DocSpace admin or another member is subscribed to it
- **THEN** the uploader receives the event and the other subscriber receives none within 2 seconds

#### Scenario: Owner's new items

- **WHEN** a member uploads one file into a project and one into an ordinary room of the same owner
- **THEN** `getNewRoomItems` of the room lists the room's file for the owner, and `getNewRoomItems` of the project lists nothing

#### Scenario: Upload notification

- **WHEN** a member uploads a file into a project
- **THEN** no notification about a file uploaded to the room is prepared for the other members and the owner

#### Scenario: Shared users of a file

- **WHEN** a member uploads a file into a project on a portal that has a DocSpace admin, and calls `getSharedUsers` for it
- **THEN** the list is empty

### Requirement: Deleting a thread of a project

`aiThreadsDelete` of a thread in an AI project SHALL answer at once and close the thread everywhere: it
leaves the list and the search, and reading or renaming it, reading or adding a message, saving an artifact
and both file lists answer 404. Its files SHALL be removed afterwards by a background pass, every 5 minutes
by default, which returns their size to the project's used space. Other threads of the project SHALL keep
their files.

#### Scenario: Gone at once

- **WHEN** the creator deletes a project thread that has a message and an artifact
- **THEN** the delete answers 204, the thread is absent from the list of the project and from a search by its title, and reading it, renaming it, reading its message, adding a message, saving an artifact, `getThreadArtifacts` and `getThreadAttachments` answer 404

#### Scenario: Files removed by the background pass

- **WHEN** the creator deletes a project thread with an artifact and a sent attachment, and the background pass has run
- **THEN** reading either file answers 404 and the project's used space equals what it was before the thread

#### Scenario: Other thread of the project

- **WHEN** the creator deletes one of two threads of a project, each with an artifact, and the background pass has run
- **THEN** the other thread is still read and still lists its artifact, and the used space equals what it was with that thread alone

#### Scenario: Project deleted afterwards

- **WHEN** the owner deletes a project one of whose threads was deleted before
- **THEN** the deletion of the project finishes without an error

### Requirement: Losing access to a project deletes the member's threads

When a member's own access to an AI project is removed (by `setRoomSecurity` with no access, sent by a manager
or by the member, or by `removeSecurityInfo` sent by the project's owner), the threads that member created in
the project SHALL be deleted as by `aiThreadsDelete`, with their files. Threads of other members SHALL stay, a
later invitation SHALL NOT bring the threads back, and a change of the project's owner SHALL delete nothing.

#### Scenario: Member removed by the owner

- **WHEN** the owner removes a member who has a thread with an artifact and a sent attachment, and the background pass has run
- **THEN** the member's read of the thread, 200 before, answers 404, both files answer 404 and the project's used space equals what it was before the member's thread

#### Scenario: Member leaves

- **WHEN** a member removes their own access to the project
- **THEN** the thread answers 404 and its files are removed as above

#### Scenario: Deactivated member removed

- **WHEN** the owner removes a member whose account was disabled
- **THEN** the files of the member's thread are removed and the used space returns

#### Scenario: Other members

- **WHEN** one of two members with a thread each is removed
- **THEN** the other member still reads their thread and lists its artifact

#### Scenario: Invited again

- **WHEN** a removed member is invited to the project again as Content creator
- **THEN** the old thread and its artifact list answer 404

#### Scenario: Owner removes the project from the list

- **WHEN** the project's owner calls `removeSecurityInfo` for a project where two members have a thread each
- **THEN** both threads answer 404 and their files are removed

#### Scenario: Member removes the project from the list

- **WHEN** a member calls `removeSecurityInfo` for the project
- **THEN** the member is still in the project, still reads the thread and still lists its artifact

#### Scenario: Owner changed

- **WHEN** the project passes to a new owner who had a thread in it, the former owner having one too
- **THEN** both still read their threads and the new owner still lists their artifact

### Requirement: Thread list of an AI project

`aiThreadsList` with the id of an AI project as `entityId` SHALL list the threads the caller started in that
project and no other thread. A thread started in a project SHALL NOT be listed for another project, for any
other `entityId` or without one.

#### Scenario: Thread started in a project

- **WHEN** the owner of two projects starts a thread in one of them and lists threads for that project, for the other project, for a room and without an entity
- **THEN** the list of that project holds exactly this thread and the other three lists do not hold it

#### Scenario: Thread started in a room

- **WHEN** the owner starts one thread in a room and one in a project, and lists threads for the project and without an entity
- **THEN** the list of the project holds the project's thread only, and the list without an entity holds the room's thread

## MODIFIED Requirements

### Requirement: One thread list per user

`aiThreadsList` SHALL show the caller's threads as one list whatever `entityId` it carries, an AI project
excepted: a thread started in room A is listed for room B, a folder, a third-party room, a string that is no
id and no entity at all; the id is not checked, so a room the caller cannot open lists
the same threads and never another user's. A thread SHALL survive the archiving or deletion of the room it
was started in and stays readable and answerable.

#### Scenario: Rooms share one list

- **WHEN** a member starts a thread in room A and lists threads for room B, for a folder and without an entity
- **THEN** the thread appears in all three lists

#### Scenario: Room of the thread deleted

- **WHEN** the room a thread was started in is archived, unarchived and then deleted
- **THEN** the thread stays listed, readable, answerable and deletable

### Requirement: Thread privacy

A thread SHALL be visible to its creator only: `aiThreadsGetById`, `aiThreadsReadMessages`,
`aiThreadsRename`, `aiThreadsDelete`, `aiThreadsClearMessages`, `aiThreadsTouch`,
`aiThreadsOpenOrCreate`, every message route, a tool-call decision and a send or regeneration into it
SHALL answer 403 to any other member, a DocSpace admin or a member of the same room included. A member
removed from a non-project room SHALL still manage the thread they wrote there but is refused a send into it
(403).

#### Scenario: Two members of one room

- **WHEN** two members each start a thread in the same room
- **THEN** each lists only their own thread and reading the other's answers 403

#### Scenario: Removed member keeps their thread

- **WHEN** a member who wrote in a room that is not an AI project is removed from it
- **THEN** creating for that room and sending into the thread answer 403, while the thread is still read and renamed by its creator

#### Scenario: Member removed from an AI project

- **WHEN** a member who wrote a thread in an AI project is removed from the project
- **THEN** reading that thread answers 404

#### Scenario: Regeneration by a fellow member

- **WHEN** a member of the same room regenerates the owner's thread
- **THEN** the system answers 403, the reply is kept and the model is not reached
