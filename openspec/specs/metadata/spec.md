# metadata Specification

## Purpose

Defines the structured metadata of files, folders and rooms: the portal-wide metadata templates and their typed
fields (string, date, number, single choice, multiple choice) that a DocSpace admin manages, the assignment of a
template to an entry and the values written for its fields, the free-form custom fields any editor of an entry
may set by name, the cascade of a template from a folder or a room over its content and the inheritance an entry
gets when it is created or moved, what happens to the metadata when an entry is copied, deleted, trashed or
restored, the metadata filter and the typed search of the listings, the part the string values take in the free
text search, the audit trail, the real-time events and the backup of it all. The AI proposal of fields and values
for a file (`POST api/2.0/ai/meta/generate`, `POST api/2.0/ai/meta/fill`) is specified in `ai-agents`; it writes
through the operations below and is not restated here. Who may read or edit a file or a folder at all is the
business of `room-role-permissions`, `file-sharing` and `shared-links`; this specification only says which of
those rights the metadata follows.

Sources: the integration and unit tests of `products/ASC.Files/Tests/Tests/10_Metadata`, the OpenAPI contract
`sdk/docspace-api-spec/docspace-backend.yaml` (operations named by `operationId`), and the product code of the
metadata service, the filter helper, the cascade worker, the DAOs and the security checks where the tests stop.
The release notes up to v4.0 do not mention the feature. Where the OpenAPI document and the server disagree this
specification follows the server; the evidence per scenario, the differences and the suspected defects are in
`verification.md` next to this file. HTTP status codes are stated where the tests assert them or the code
produces them.

## Requirements

### Requirement: Listing the metadata templates

`getTemplates` (`GET api/2.0/files/metadata/templates`) SHALL return to any signed-in member every template of
the portal in creation order, each with its fields in display order and the options of its choice fields, with
`createBy` and `modifiedBy` as user objects (not identifiers). The `visible` query SHALL narrow the list to the
shown or to the hidden templates. The template that holds the custom fields SHALL never be listed. Anonymous: 401.

#### Scenario: Authors come as users

- **WHEN** the owner creates a template and lists the templates
- **THEN** the entry carries `createBy` and `modifiedBy` with the owner's `id` and a non-empty `displayName`

#### Scenario: Custom fields are not a template

- **WHEN** a room holds a custom field and one template exists on the portal
- **THEN** `getTemplates` lists exactly that template, and no listed field carries the custom field's name

### Requirement: Creating a metadata template

`createTemplate` (`POST api/2.0/files/metadata/templates`) SHALL be allowed to a DocSpace admin only (others:
403). `name` SHALL be non-blank, at most 255 characters, unique on the portal without regard to case and never
the reserved `System` (400); `visible` defaults to true and is stored as sent; the optional `fields` SHALL be
validated before anything is stored, so an invalid field leaves no template behind. Concurrent creations of one
name SHALL yield one 200, the rest 400.

#### Scenario: Room admin creates a template

- **WHEN** a Room admin or a User calls `createTemplate`
- **THEN** the system answers 403

#### Scenario: Hidden template stays hidden

- **WHEN** the owner creates a template with `visible` false and reads it back with `getTemplate`
- **THEN** both answers carry `visible` false

#### Scenario: Reserved name

- **WHEN** the owner creates a template named `System` or `system`, whether or not a custom field exists yet
- **THEN** the system answers 400, and setting a custom field afterwards still works

#### Scenario: Name too long or fields repeated

- **WHEN** the `name` is 256 characters long, or two fields are named `Client` and `client`
- **THEN** the system answers 400

#### Scenario: Five parallel creations of one name

- **WHEN** five requests create the same name at once
- **THEN** exactly one answers 200, the others 400, and `getTemplates` lists the name once

### Requirement: Reading one metadata template

`getTemplate` (`GET api/2.0/files/metadata/templates/{templateId}`) SHALL return to any signed-in member the
template with its fields in display order and the options of its choice fields, and SHALL answer 404 for an
identifier that does not exist, was deleted, or belongs to the hidden template of the custom fields.

#### Scenario: Deleted template

- **WHEN** a template is deleted and read again by its identifier
- **THEN** the system answers 404

### Requirement: Updating a metadata template

`updateTemplate` (`PUT api/2.0/files/metadata/templates/{templateId}`) SHALL be allowed to a DocSpace admin only
(403, nothing changed) and SHALL change only what the body names: a non-empty `name` (same rules as on creation,
400 when taken or too long) and `visible`. It SHALL store the change, set `modifiedBy` to the caller, and answer
200 with the whole template, its fields included. Unknown template: 404.

#### Scenario: Rename and hide

- **WHEN** the owner sends a new `name` and `visible` false
- **THEN** the answer and a later `getTemplate` carry the new name and `visible` false

#### Scenario: Fields survive the answer

- **WHEN** the owner renames a template holding a field `Code`
- **THEN** the answer lists the field `Code`

#### Scenario: Refused rename

- **WHEN** a Room admin or a User renames a template
- **THEN** the system answers 403 and `getTemplate` shows the old name

### Requirement: Deleting a metadata template

`deleteTemplate` (`DELETE api/2.0/files/metadata/templates/{templateId}`) SHALL be allowed to a DocSpace admin
only (403, template kept) and SHALL delete the template with its fields, its assignments on every entry and
every value written for its fields, answering 200 with no body. Afterwards `getTemplate` SHALL answer 404,
`getTemplates` SHALL not list it, the entries SHALL not report it, and a listing filter naming it SHALL be
refused with 400 like an unknown template.

#### Scenario: Template deleted while assigned

- **WHEN** a room carries the template with a value and the owner deletes the template
- **THEN** `getFolderMetadata` of the room no longer lists the template and `searchFolder` with its `metadataTemplateId` answers 400

### Requirement: Field types and their options

A field SHALL have one of five types: string, date, number, single choice, multiple choice; a type outside that
set SHALL be refused with 400. A choice field SHALL carry at least one option, the option values non-blank and
unique without regard to case, each option getting a generated identifier the values refer to; a field of any
other type SHALL carry no options (400 otherwise).

#### Scenario: Type outside the set

- **WHEN** the owner creates a template with a field whose `type` is a number not in the set
- **THEN** the system answers 400

#### Scenario: Options generated

- **WHEN** the owner creates a single choice field with the options `Draft` and `Signed`
- **THEN** the answer carries both options with distinct identifiers

### Requirement: Adding a field to a template

`createField` (`POST api/2.0/files/metadata/templates/{templateId}/fields`) SHALL be allowed to a DocSpace admin
only (403) and SHALL add one field with `name` (non-blank, at most 255 characters, unique within the template
without regard to case: 400), `type`, `options` and an optional `order`, answering 200 with the field and its
generated option identifiers. Unknown template: 404.

#### Scenario: Field added

- **WHEN** the owner adds `Department` to a template holding `Client`
- **THEN** `getTemplate` lists `Client` and `Department`

#### Scenario: Name taken in another case

- **WHEN** the owner adds `CLIENT` to a template holding `Client`
- **THEN** the system answers 400

### Requirement: Display order of the fields

The fields of a template SHALL be returned by `order` ascending, equal positions in creation order, both in the
template and in the metadata of an entry. A field created without `order` SHALL be placed after the highest
position the template has, the unpositioned fields of one request following each other in request order. An
update with `order` SHALL move the field; one without SHALL leave it where it is.

#### Scenario: Mixed request

- **WHEN** a template is created with `Third`, `First` (order 0), `Fourth`, `Second` (order 1)
- **THEN** the fields come back as `First`, `Second`, `Third`, `Fourth`

#### Scenario: Appended after the highest position

- **WHEN** a template holds `A` (order 5) and `B` (order 2) and `C` is added without `order`
- **THEN** `C` gets order 6 and the template lists `B`, `A`, `C`

#### Scenario: Entry metadata keeps the order

- **WHEN** a file carries a template whose fields are `First` (0), `Second` (1), `Third` (none) and only `Third` has a value
- **THEN** `getFileMetadata` lists every field as `First`, `Second`, `Third`

### Requirement: Updating a field

`updateField` (`PUT api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}`) SHALL be allowed to a
DocSpace admin only (403) and SHALL change only the properties the body names: `name` (400 when another field of
the template has it), `type` (only while the field holds no values: else 400), `options` (new ones get
identifiers; an option selected on any entry cannot be removed: 400) and `order`. A field reached through a
template it does not belong to: 404.

#### Scenario: Rename keeps the type and options

- **WHEN** the owner sends only `name` for a number field, and only `name` for a choice field with `Draft` and `Signed`
- **THEN** the number field keeps its type, and the choice field keeps its type and both options, also when read back

#### Scenario: Retype an empty field

- **WHEN** the owner sends `type` number for a string field that holds no values
- **THEN** the answer carries the number type and the unchanged name

#### Scenario: Remove an option

- **WHEN** `Draft` is selected on a room and the owner sends `options` with `Draft` only
- **THEN** the system answers 200 and the field keeps `Draft` alone
- **AND** sending `options` with `Signed` only answers 400 and the field keeps both

#### Scenario: Field of another template

- **WHEN** the owner updates a field through the route of a template it does not belong to
- **THEN** the system answers 404 and the field is unchanged

### Requirement: Deleting a field

`deleteField` (`DELETE api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}`) SHALL be allowed to a
DocSpace admin only (403) and SHALL remove the field from its template and every value written for it from
every entry, answering 200 with no body; the other fields keep their values. A filter condition naming the
field SHALL then be refused with 400. A field reached through another template: 404.

#### Scenario: Field deleted while valued

- **WHEN** a room holds `Client` = `ACME` and `Department` = `Legal` and the owner deletes `Department`
- **THEN** `getTemplate` lists `Client` alone and `getFolderMetadata` of the room lists one field holding `ACME`
- **AND** `searchFolder` with a condition on the deleted field answers 400

### Requirement: Last-modified stamp of a template

`modifiedOn` and `modifiedBy` of a template SHALL move when the template itself is renamed or re-hidden and when
one of its fields is added, changed or deleted, and SHALL NOT move when values are written for its fields on an
entry. The stamp has a precision of one second.

#### Scenario: Field change moves the stamp

- **WHEN** a field is added, then renamed, then deleted, a second apart
- **THEN** `getTemplate` reports a later `modifiedOn` after each step, with `modifiedBy` the caller

#### Scenario: A value does not

- **WHEN** a value is written on a room carrying the template
- **THEN** `modifiedOn` of the template is unchanged

### Requirement: Reading the metadata of an entry

`getFileMetadata` (`GET api/2.0/files/metadata/file/{fileId}`) and `getFolderMetadata` (`GET
api/2.0/files/metadata/folder/{folderId}`) SHALL require the right to read the entry (403; unknown 404) and
answer `templates` — each assigned template with all its fields and their `value` or null, plus `cascade` and
`conflictResolveType` (null unless the folder cascades it; false and null on a file) — and `customFields` as
name/value pairs. The template of the custom fields SHALL never appear there.

#### Scenario: Viewer reads a file card

- **WHEN** a room member with the Viewer role reads `getFileMetadata` of a file holding `Client` = `ACME`
- **THEN** the answer lists the template with the field `Client` whose `value.stringValue` is `ACME`

#### Scenario: Cascade state reported

- **WHEN** a room cascades a template in overwrite mode, a sub-folder carries it without cascade and a file inherited it
- **THEN** the room reports `cascade` true with `conflictResolveType` overwrite, the sub-folder `cascade` false with a null mode, the file `cascade` false with a null mode

#### Scenario: Custom field named like a template field

- **WHEN** a room holds the template field `Client` = `ACME` and a custom field `Client` = `Globex`
- **THEN** `templates` holds one template whose `Client` is `ACME` and `customFields` holds `Client` = `Globex`

### Requirement: The right to write the metadata of an entry

Assigning or unassigning a template, writing values and setting custom fields SHALL follow one rule: on a file,
the right to edit the file; on a folder, the right to create in it (a Room manager or a Content creator, not an
Editor or a Viewer); on a room, the right to edit the room (its manager). The section roots and an agent's
knowledge folder SHALL refuse every write (403) although their owner may create in them. A refused write SHALL
change nothing.

#### Scenario: Viewer

- **WHEN** a Viewer of a room sets values on the file or the room, assigns a template to the file or unassigns one
- **THEN** every call answers 403 and the metadata is unchanged

#### Scenario: Editor

- **WHEN** an Editor of a room assigns a template to a file and writes its values, then writes values or custom fields on a sub-folder or on the room, or switches a cascade on the sub-folder
- **THEN** the file calls answer 200 and every folder and room call 403

#### Scenario: Content creator

- **WHEN** a Content creator assigns a template to a sub-folder, writes its values and switches its cascade on, then writes values on the room
- **THEN** the folder calls answer 200 (the cascade answer carries an operation `id`) and the room call 403

#### Scenario: Section roots

- **WHEN** the owner assigns a template with cascade to My documents, or a Room admin to the rooms root, or either sets a custom field there
- **THEN** the system answers 403

### Requirement: Assigning templates to a file

`assignFileTemplates` (`PUT api/2.0/files/metadata/file/{fileId}/templates`) SHALL take `templateIds` (400 when
the body cannot be read or has none) and link each listed template to the file, skipping the ones it already
carries, answering 200 with no body. A template that does not exist, or the hidden template of the custom fields:
404. The right required is the right to edit the file (403); a file that does not exist: 404.

#### Scenario: Editor assigns

- **WHEN** an Editor of the room calls `assignFileTemplates` for a file of the room
- **THEN** the system answers 200 and `getFileMetadata` lists the template with every field empty

### Requirement: Assigning templates to a folder or a room

`assignFolderTemplates` (`PUT api/2.0/files/metadata/folder/{folderId}/templates`) SHALL take `templateIds`,
`cascade` and `conflictResolveType`, link the templates to the folder (403 without the folder write right; 404
for a missing folder or template) and answer an operation: completed, with no `id`, progress 100 and no error
when `cascade` is off or the list is empty (nothing else done); the queued pass with its `id` and `templateIds`
when `cascade` is on and a template is given.

#### Scenario: No cascade

- **WHEN** the owner assigns a template to a room with `cascade` false
- **THEN** the answer is `{id: null, progress: 100, isCompleted: true, error: null}` and `getCascadeProgress` of the room reports the same

#### Scenario: Empty list with cascade

- **WHEN** the owner sends an empty `templateIds` with `cascade` true
- **THEN** the answer is a completed operation without error and the room carries no template

#### Scenario: Cascade queued

- **WHEN** the owner assigns a template with `cascade` true
- **THEN** the answer carries `templateIds` equal to the assigned template

### Requirement: Unassigning a template from an entry

`unassignFileTemplate` (`DELETE api/2.0/files/metadata/file/{fileId}/templates/{templateId}`) and
`unassignFolderTemplate` (`DELETE api/2.0/files/metadata/folder/{folderId}/templates/{templateId}`) SHALL remove
the template and the values of its fields from the entry in one step, answering 200 with no body (403 without
the write right; 404 for a missing entry or template). A cascaded template's inherited links in the content
SHALL become direct assignments keeping their values.

#### Scenario: Room un-cascaded by unassignment

- **WHEN** a room cascades a template whose value reached a file and the owner calls `unassignFolderTemplate` on the room
- **THEN** the room no longer lists the template, the file still lists it with the inherited value
- **AND** `unassignFileTemplate` on the file then removes the template and its value from the file

#### Scenario: Only the links of this folder are converted

- **WHEN** a room and a nested folder both cascade one template and the room is unassigned
- **THEN** a file under the nested folder keeps the nested folder's value and a copy of it carries no template, because its link is still inherited

### Requirement: Setting the values of template fields

`setFileValues` (`PUT api/2.0/files/metadata/file/{fileId}/values`) and `setFolderValues` (`PUT
api/2.0/files/metadata/folder/{folderId}/values`) SHALL take `values`, one per `fieldId` (null list or a field
listed twice: 400), each field of a template assigned to the entry and not a custom field (400); an unknown
field: 404. An empty value SHALL clear the field. The answer SHALL be the whole metadata of the entry as the
read returns it, every value the entry holds included.

#### Scenario: Answer is the entry, not the request

- **WHEN** a room holds `Client` = `ACME` and a custom field, and the owner writes `Amount` = 7
- **THEN** the answer lists the template with `Client` = `ACME` and `Amount` = 7, and the custom field

#### Scenario: Field listed twice

- **WHEN** `values` names one field twice with two strings
- **THEN** the system answers 400

#### Scenario: Null list

- **WHEN** the body is `{"values": null}`
- **THEN** the system answers 400

#### Scenario: Value cleared

- **WHEN** the owner sends a value with `fieldId` and nothing else for a field holding `ACME`
- **THEN** the room is no longer found by `Client` = `ACME` while its other values still match

### Requirement: Validation of a value by its field type

A value SHALL carry exactly the member of its field's type: `stringValue` for a string field (at most 8000
characters), `dateValue` for a date field, `numberValue` for a number field (a whole number within ±(2^53−1)),
`optionIds` for a choice field — every option known to the field, a single choice field taking one distinct
option. Anything else SHALL be refused with 400 and nothing of the request written.

#### Scenario: String too long

- **WHEN** a string field or a custom field receives 8001 characters
- **THEN** the system answers 400

#### Scenario: Longest string stored and found

- **WHEN** a string field receives exactly 8000 characters
- **THEN** `getFileMetadata` returns it whole and a condition with that value finds the file

#### Scenario: Number limits

- **WHEN** a number field receives 9007199254740991 or its negative
- **THEN** it is accepted; one beyond that magnitude is refused with 400

#### Scenario: Two options on a single choice

- **WHEN** a single choice field receives two different options, or any choice field an option it does not have
- **THEN** the system answers 400

### Requirement: Date values and their time zone

A `dateValue` carrying an offset SHALL be stored as that instant in UTC; one without an offset SHALL be read as
UTC, never shifted by the server time zone, so a day written as a date is found by a filter on the same day.
The stored date SHALL come back in `value.dateValue` with its UTC time.

#### Scenario: Midnight without offset

- **WHEN** `2026-01-15T00:00:00` is written and the room is filtered with `from` and `to` both `2026-01-15`
- **THEN** the stored UTC time is 2026-01-15 00:00 and the room is found

### Requirement: Custom fields of an entry

`setFileCustomFields` (`PUT api/2.0/files/metadata/file/{fileId}/customFields`) and `setFolderCustomFields`
(`PUT api/2.0/files/metadata/folder/{folderId}/customFields`) SHALL take `fields` of `name`/`value` pairs (null
or empty list, blank name, name over 255 or value over 8000 characters, a name twice: 400). Names match without
regard to case; a new name creates the field; a null or empty `value` removes it from this entry only; names not
listed stay. The answer is the entry's custom fields.

#### Scenario: Several at once, partial update

- **WHEN** the owner sets `Project code` = `A-42` and `Client` = `ACME`, then `Client` = `Globex` alone
- **THEN** the second answer is `Project code` = `A-42`, `Client` = `Globex`, and `getFolderMetadata` agrees

#### Scenario: Case-insensitive name

- **WHEN** a room holds `Reference` and a file receives `reference` = `R-2`
- **THEN** the file's answer names the field `Reference`

#### Scenario: Removal is per entry

- **WHEN** a room and a file both hold `Project code` and the file receives `Project code` = null
- **THEN** the file's answer is empty and the room keeps its field
- **AND** removing a name no entry holds answers 200 with the entry's fields, as a no-op

#### Scenario: Rename on one entry

- **WHEN** a file receives `Client` = null and `Customer` = `Globex` in one request
- **THEN** the file holds `Customer` only while the room still holds `Client`

### Requirement: Limits and dictionary of the custom fields

An entry SHALL hold at most 50 custom fields (a request pushing it over: 400, nothing written). The portal-wide
set of custom field names SHALL follow the values: a name no entry holds a value for any more SHALL be dropped,
and a filter naming it SHALL answer 400 as for an unknown field. The custom fields of an entry SHALL be returned
in the order the names were created, a re-created name taking the last place.

#### Scenario: Fifty-one fields

- **WHEN** a request sets 51 custom fields on an empty room
- **THEN** the system answers 400 and the room holds none

#### Scenario: Order across removals

- **WHEN** `A`, `B`, `C` are set, `B` removed, `D` added, `B` set again, then `A` and `C` removed and `E` added
- **THEN** the orders are `A, C, D`, then `A, C, D, B`, then `D, B, E`

#### Scenario: Dropped name in a filter

- **WHEN** the only value of custom field `Ephemeral` is removed and the rooms are filtered by `name` `Ephemeral`
- **THEN** the system answers 400, while the same filter was answered 200 before the removal

### Requirement: Cascade of a template over the content of a folder

A folder or room assigned a template with `cascade` SHALL have a pass link the template to every entry below it
and copy the folder's filled fields: in skip mode into empty fields only, in overwrite mode over the entries'
values; an empty folder field never touches an entry. A nested folder cascading the same template is
left out with its subtree (the nearest cascading ancestor wins). The pass is a snapshot: a later edit of the
folder changes the folder only until a cascade is requested again.

#### Scenario: Nearest cascading folder wins

- **WHEN** a nested folder cascades `Client` = `Nested` and the room then cascades `Client` = `Room` in overwrite mode
- **THEN** a plain sub-folder and its file take `Room`, the nested folder and its file keep `Nested`

#### Scenario: Only filled fields travel

- **WHEN** the room cascades `Department` = `Legal` with `Client` empty in overwrite mode over a file holding `Client` = `Own`
- **THEN** the file gets `Department` = `Legal` and keeps `Client` = `Own`; a bare file is linked with `Client` empty

#### Scenario: Edit after the pass

- **WHEN** the room's `Client` changes from `v1` to `v2` after the pass and a file is created in the room
- **THEN** the old file keeps `v1`, the new file inherits `v2`; a skip cascade leaves `v1`, an overwrite cascade makes it `v2`

#### Scenario: Back-to-back requests both land

- **WHEN** a cascade of template A is requested and right after it one of A and B on the same room
- **THEN** a file and a sub-folder of the room end up carrying both, and the progress reports no error
- **AND** a second overwrite request sent right after a value edit pushes the edited value to every file

### Requirement: Progress of a cascade

`getCascadeProgress` (`GET api/2.0/files/metadata/folder/{folderId}/templates/progress`) SHALL require the right
to read the folder (404 for a missing one) and report the running cascade of the folder, else the most recent
one: `id`, `progress`, `isCompleted`, `error` and the `templateIds` it propagates. A folder that never cascaded
SHALL get a completed operation with no `id`, progress 100 and empty `templateIds`. A pass that fails SHALL
complete with its message in `error`.

#### Scenario: Idle folder

- **WHEN** `getCascadeProgress` is called on a room that never cascaded
- **THEN** `isCompleted` is true and `templateIds` is empty

#### Scenario: Pass identified by its templates

- **WHEN** a cascade of one template was queued on a room
- **THEN** the assignment answer and the progress both carry that template in `templateIds`

### Requirement: Switching the cascade of an assigned template

`updateFolderTemplate` (`PUT api/2.0/files/metadata/folder/{folderId}/templates/{templateId}`) SHALL take
`cascade` and `conflictResolveType` for a template the folder already carries (otherwise, or unknown: 404; 403
without the folder write right). On: mark the link cascading with that mode, queue a pass and answer it with its
`id`. Off: keep the template and the folder's values, make the inherited links of the content direct, answer a
completed operation without `id`; off again is a no-op.

#### Scenario: Cascade off

- **WHEN** a room cascading `Client` = `ACME` is switched off
- **THEN** the room still lists the template with `cascade` false, a null mode and `ACME`; an earlier file keeps `ACME`; a file created afterwards carries nothing
- **AND** switching off again answers a completed operation

#### Scenario: Cascade on

- **WHEN** a room carrying a template without cascade is switched on in overwrite mode
- **THEN** the answer carries an `id` and the template; the file below receives the room's value; the room reports `cascade` true with the overwrite mode

#### Scenario: Template not on the folder

- **WHEN** the switch is called for a template the folder does not carry
- **THEN** the system answers 404

### Requirement: Inheritance when an entry is created or moved

An entry created under a cascading folder SHALL carry the cascaded templates and values before the create call
returns. A move into a cascading folder SHALL stamp the moved entry under that folder's mode (skip fills empty
fields, overwrite replaces); a moved folder's content is stamped by a background pass, a moved folder cascading
the template itself keeps its own for its subtree. Leaving a cascading tree SHALL make the inherited metadata the
entry's own. A move to Trash stamps nothing.

#### Scenario: Created under a cascade

- **WHEN** a file is created in a cascading folder holding `Client` = `Folder client`
- **THEN** `getFileMetadata` right after the creation carries `Folder client`

#### Scenario: Moved in, skip and overwrite

- **WHEN** a file holding `Client` = `Own client` and an empty `Department` is moved into a cascading folder, from the same room or from My documents
- **THEN** in skip mode it gets `Department` from the folder and keeps `Own client`; in overwrite mode `Client` becomes the folder's too

#### Scenario: Moved out

- **WHEN** a file that inherited `Client` leaves the cascading folder and is copied elsewhere
- **THEN** the file and its copy both carry `Client`, the inherited value having become the file's own
- **AND** the same holds for a file inside a moved-out folder

#### Scenario: Moved cascading folder

- **WHEN** a folder cascading `Client` = `Own client` is moved into a folder cascading the same template in overwrite mode
- **THEN** the moved folder and its file keep `Own client`, and the file's `Department` stays empty

### Requirement: Metadata of a copy

A copy of a file or a folder SHALL carry the direct assignments and values of the source (the ones it inherited
and still points to a cascading folder for are not copied); a folder copy SHALL carry the metadata of its
content as well, and the source SHALL be left as it was. What the destination cascades SHALL be applied to the
copy and SHALL survive next to the copied values.

#### Scenario: Copy into a cascading room

- **WHEN** a file holding `Client` = `ACME` is copied into a room cascading `Department` = `Legal`
- **THEN** the copy holds both `ACME` and `Legal`

#### Scenario: Folder copied

- **WHEN** a folder holding `Client` = `Folder` with a file holding `Client` = `File` is copied to another room
- **THEN** the folder copy holds `Folder`, the file copy `File`, and the source still holds `Folder`

### Requirement: Metadata through deletion, Trash and restore

An entry deleted for good, dropped when Trash is emptied, or deleted with its room SHALL lose its metadata:
reading it answers 404 and no filter finds it. An entry in Trash SHALL keep its metadata and SHALL be found by
the filter in Trash; a restored entry SHALL keep it and be found where it came back to.

#### Scenario: Trash round trip

- **WHEN** a folder with a file is moved to Trash and the file is moved back to My documents
- **THEN** in Trash the folder still lists its template; after the restore the file holds its value and is found in My documents

#### Scenario: Trash emptied

- **WHEN** Trash holding that folder is emptied
- **THEN** `getFolderMetadata` and `getFileMetadata` answer 404 and the filter on My documents returns neither

#### Scenario: Room deleted

- **WHEN** a room carrying a template with a file carrying it is deleted
- **THEN** both reads answer 404 and the rooms filter no longer returns the room

### Requirement: Assigned templates in the listings

Every file and folder returned by a listing, by `getFileInfo` and by `getFolderInfo` SHALL carry
`assignedMetadataTemplates`, the identifiers of the templates it carries, empty or absent when it carries none;
the info endpoints and the listing SHALL agree. A portal that gets its first template after a listing SHALL
report the assignments made afterwards.

#### Scenario: Info agrees with the listing

- **WHEN** a template is assigned to a file and to a sub-folder of a room
- **THEN** `getFileInfo`, `getFolderInfo` and the room listing all report that template on them

#### Scenario: First template of the portal

- **WHEN** a room is listed before the portal has a template, then a template is created and assigned to its sub-folder
- **THEN** the next listing reports the template on the sub-folder

### Requirement: Filtering a listing by metadata

`getFolderByFolderId`, `getRoomsFolder` and the section listings SHALL take `metadataTemplateId` and
`metadataFilters` (a URL-encoded JSON array of conditions). The template alone keeps the entries carrying it;
the conditions all hold at once, their fields belonging to the named template when given. `total` counts the
filtered result; a folder listing searches the subtree unless `withSubFolders` is false. Unknown template or
field, field of another template, bad JSON, over 20 conditions: 400.

#### Scenario: Template alone

- **WHEN** a room is listed with `metadataTemplateId` and no conditions
- **THEN** every folder and file carrying the template is returned, the bare ones are not, and `total` counts them

#### Scenario: Folder and file together

- **WHEN** a room is listed with the condition `Client` = `ACME` held by one sub-folder and one file
- **THEN** `folders` holds that sub-folder, `files` that file, `total` and `count` are 2; a value nobody holds gives empty lists and `total` 0

#### Scenario: Subtree

- **WHEN** the condition matches an entry two levels below the room
- **THEN** it is returned by default and not with `withSubFolders` false

#### Scenario: Invalid filter

- **WHEN** `metadataFilters` is not JSON, names an unknown field, names a field of another template than `metadataTemplateId`, or `metadataTemplateId` is unknown
- **THEN** the system answers 400

#### Scenario: Rooms filtered with the other filters

- **WHEN** the rooms are listed with a metadata condition together with a room `type`, a `searchArea` or a `filterValue`
- **THEN** every filter narrows the result; an archived room is found in the archive area and no longer in the active one; a member sees only the matching rooms they may see

### Requirement: Access before the filter is validated

On the anonymous folder listing the access to the folder SHALL be checked before the metadata filter is parsed,
so a caller without access learns nothing about the portal's templates from the validation.

#### Scenario: Stranger with a bogus template

- **WHEN** an anonymous caller lists a room with an unknown `metadataTemplateId`
- **THEN** the system answers 401, not 400

### Requirement: Conditions and operators of a filter

A condition SHALL name a template field by `fieldId` or a custom field by `name` (neither: 400) and MAY name
`op`: `eq`, `contains` for a string; `eq`, `range`, `gt`, `gte`, `lt`, `lte` for a number; `range`, `gt`, `gte`,
`lt`, `lte` for a date; `in` for a choice (an operator the type does not take: 400). Omitted, it is `eq` for a
string and a number with `value`, `range` for a date and a number with bounds, `in` for a choice. `value`, `from`
and `to` MAY be JSON strings or numbers.

#### Scenario: String operators

- **WHEN** folders hold `ACME Corp`, `Globex` and `acme`
- **THEN** `contains` `acme` returns the first and the third, `eq` `acme` the third only, both without regard to case

#### Scenario: Number bounds

- **WHEN** folders hold 100, 200 and 300
- **THEN** `gt` 200 returns 300; `gte` 200 returns 200 and 300; `lt` 200 returns 100; `lte` 200 returns 100 and 200; `range` `from` 100 `to` 200 includes both bounds; `from` 100 alone is open above

#### Scenario: Choice conditions

- **WHEN** `in` names `Signed` and `Draft` for a single choice, or `Legal` and `Finance` for a multiple choice a room holds both of
- **THEN** the options are combined with OR and the room is returned once

#### Scenario: Form of a condition refused

- **WHEN** `contains` or `gt` is sent for a number or a string field respectively, `gt` carries `from` instead of `value`, `gt` has no value, `eq` carries a bound, `range` carries a `value`, a number bound is `9007199254740992`, a date bound is `yesterday`, or more than 20 conditions are sent
- **THEN** the system answers 400

#### Scenario: Custom field by name

- **WHEN** a condition carries `name` `Client` with `eq` `ACME` and two rooms hold the custom field with `ACME` and `Globex`
- **THEN** the first room is returned and the second is not

#### Scenario: JSON numbers

- **WHEN** a condition is sent as `{"fieldId": …, "op": "eq", "value": 5}` or `{"op": "range", "from": 1, "to": 6}` in the body or in the query string
- **THEN** the folder holding 5 is returned

### Requirement: Date bounds of a condition

A date bound given as a date only (`2026-06-30`) SHALL name the whole UTC day: as `to` or `lte` it covers the
day to its last second, as `from` or `gte` it starts at its midnight, `gt` starts after the day and `lt` ends
before it. A bound carrying a time SHALL be an instant taken as is.

#### Scenario: Day-only upper bound

- **WHEN** a value is 30 June 14:00 UTC and the filter is `from` `2026-06-01` `to` `2026-06-30`
- **THEN** the room is found; with `to` `2026-06-30T12:00:00Z` it is not

#### Scenario: Strict bounds step over the day

- **WHEN** values are 30 June 14:00, 1 July 09:00 and 15 July 00:00
- **THEN** `gt` `2026-06-30` returns the last two, `gt` `2026-06-30T12:00:00Z` all three, `gte` `2026-06-30` all three, `lt` `2026-07-01` the first only, `lte` `2026-07-01` the first two

### Requirement: Sections that take or refuse the metadata filter

The filter SHALL apply on the rooms (active, archive, Forms), on a folder or room, on My documents, Trash,
Recent, Favorites and Shared with me, by the section's numeric identifier and by its alias (`@my`, `@trash`,
`@recent`, `@favorites`, `@share`, `@forms`), validated the same way. The Templates and the Private room
sections SHALL refuse a non-empty filter with 400 rather than answer unfiltered, by identifier, alias or typed
search. Third-party storage entries carry no metadata.

#### Scenario: Recent, Favorites and Shared with me

- **WHEN** Recent, Favorites or Shared with me holds entries with the template and a bare one, and is listed with the template or with a condition
- **THEN** only the entries carrying the template, or matching the condition, come back and `total` counts them

#### Scenario: Aliases

- **WHEN** `@my`, `@trash`, `@share`, `@recent`, `@favorites` or `@forms` is listed with the template or a condition
- **THEN** the result is narrowed like the numeric section; an unknown template answers 400

#### Scenario: Templates section

- **WHEN** the Templates section is listed by identifier, by `@templates` or searched by `searchFolder` with a `metadataTemplateId`
- **THEN** the system answers 400

### Requirement: Typed metadata search

`searchFolder` (`POST api/2.0/files/{folderId}/search`) and `searchRooms` (`POST api/2.0/files/rooms/search`)
SHALL take the same filter as a body — `metadataTemplateId`, `metadataFilters` as objects, `filterValue`,
`includeHiddenMetadata`, paging, sorting — and answer the shape of the listing, validating and matching the
conditions identically. `count` SHALL be 1 to 100 and `startIndex` non-negative (else 400, a missing `count`
too). `searchFolder` searches the subtree unless told not to.

#### Scenario: Agrees with the listing

- **WHEN** one condition is sent through `searchFolder` and through the folder listing
- **THEN** both return the same folders, files and `total`; the same holds for `contains` and `lt`

#### Scenario: Paging refused

- **WHEN** `count` is 101, 0, -1 or absent, or `startIndex` is -1, on `searchFolder`; or `count` is 101 or -1 on `searchRooms`
- **THEN** the system answers 400

#### Scenario: Sections through the body

- **WHEN** `searchFolder` is called on Recent with a condition
- **THEN** the matching recent file is returned; on the Templates section the call answers 400

### Requirement: String metadata values in the free text search

A listing or typed search with `filterValue` SHALL also find the entries whose string values — of template
fields and custom fields — contain the text, and say where in `metadataMatches`: one item per matching value
with the whole stored `value`, `fieldName`, `templateId` and `fieldId` (both null for a custom field). Entries
found by title alone and listings without text SHALL carry no `metadataMatches`. The extension filter SHALL
still apply to entries matched by metadata.

#### Scenario: Found by a custom field

- **WHEN** a sub-folder or a room holds a custom field `Reference` with a unique marker and the listing is searched for it
- **THEN** the entry is returned with one match: `fieldName` `Reference`, the marker as `value`, null `templateId` and `fieldId`

#### Scenario: Found by a template field

- **WHEN** `Client` holds `<marker> Corp` and the text is the marker
- **THEN** the match carries the template and field identifiers, `Client`, and `<marker> Corp` whole

#### Scenario: Several matches

- **WHEN** a file holds the marker in `Client` and in the custom field `Reference`
- **THEN** `metadataMatches` holds two items, one per field

#### Scenario: Extension still applies

- **WHEN** a spreadsheet is found by a custom field and the listing asks for `extension` `docx`
- **THEN** the spreadsheet is not returned

### Requirement: Hidden templates and the text search

The values of a template created with `visible` false SHALL be left out of the text search and of
`metadataMatches` unless the request sends `includeHiddenMetadata` true (query on the listings, body on the
typed searches). A `metadataTemplateId` or a condition naming a hidden template or its field SHALL apply
without the flag.

#### Scenario: Hidden value not searched

- **WHEN** a folder or a room holds a marker in a hidden template's field and the text search asks for it
- **THEN** the entry is returned with `includeHiddenMetadata` true and not without, on the listing and on the typed search alike

#### Scenario: Found by title, hidden match reported on request

- **WHEN** a folder titled with the marker also holds it in a hidden field
- **THEN** it is found either way, `metadataMatches` being empty without the flag and naming the hidden field with it

#### Scenario: Condition on a hidden field

- **WHEN** a condition names the hidden field with the marker and `metadataTemplateId` the hidden template
- **THEN** the folder is returned without the flag

### Requirement: The stored values are the truth of a filter

A filter or text search SHALL never return an entry by a value it no longer holds: after a template is
unassigned or a value cleared, the very next listing SHALL leave the entry out, even if a search index still
remembers it. A newly written value MAY take a moment to become searchable.

#### Scenario: Unassigned right away

- **WHEN** a template is unassigned from a folder found by `Client` = `ACME` and the room is listed once, at once
- **THEN** the folder is absent and a file holding its own `ACME` is still returned

### Requirement: Metadata reached through an external link

An anonymous caller holding the external link key of a room SHALL read `getFileMetadata` and
`getFolderMetadata` of the room's entries as a member would, and SHALL write `setFileValues` through a link
granting editing but not through one granting viewing (403). A key of another room grants nothing (403); no
session and no key: 401. `setFolderValues`, the assignments and the custom fields SHALL stay closed to anonymous
callers (401).

#### Scenario: Read through the room link

- **WHEN** an anonymous caller with the room's link reads a file and a folder of the room
- **THEN** both answers list the template with the stored values

#### Scenario: Write through the link

- **WHEN** the link grants editing and the caller writes a file value, then the link grants viewing and the caller tries again
- **THEN** the first write is stored and visible to the owner, the second answers 403 and changes nothing

#### Scenario: Closed writes

- **WHEN** an anonymous caller with an editing link sets folder values, unassigns a file template or sets custom fields
- **THEN** every call answers 401

### Requirement: Clients are told about metadata changes

Every assignment, unassignment, value write, custom field write and cascade switch SHALL send the connected
clients the ordinary update event of the entry, so an open listing re-reads its row. A cascade pass, the
deletion of a template and the deletion of a field SHALL send the same event for every entry they changed. A
failure to send SHALL NOT fail the call.

#### Scenario: Cascade touches the subtree

- **WHEN** a cascade pass writes the links of a room's files
- **THEN** the clients viewing the room receive an update event per changed file and one for the room when the pass ends

### Requirement: Audit trail of the metadata actions

The creation, update and deletion of a template and of a field, the assignment and unassignment of a template,
a write of values or custom fields and the start of a cascade SHALL each write one audit event whose text names
the template, the field or the entry: "Metadata template created: <name>", "Metadata field deleted: <name>",
"Metadata values updated: <entry title>" and so on. Reads and the progress poll SHALL write none.

#### Scenario: One session audited

- **WHEN** the owner creates a template and a field, renames the template, assigns it to a room, writes a value, unassigns it, deletes the field and the template
- **THEN** the audit trail shows "Metadata template created", "Metadata field created", "Metadata template updated", "Metadata template assigned", "Metadata values updated", "Metadata template unassigned", "Metadata field deleted" and "Metadata template deleted", each with its object's name

### Requirement: Metadata in backup and restore

A portal backup SHALL include the templates, the fields, the assignments (their cascade source included) and
the values, so a restore brings the metadata back on the restored files and folders with the identifiers
remapped together.

#### Scenario: Restore on another installation

- **WHEN** a portal with templates assigned to a room is backed up and restored
- **THEN** `getFolderMetadata` of the restored room lists the templates with their values
