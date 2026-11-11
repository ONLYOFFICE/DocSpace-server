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
The release notes up to v4.0 do not mention the feature; no notes exist for v4.1.0, so the revision after its merge
rests on the git diff and the tests alone. Where the OpenAPI document and the server disagree this specification
follows the server; the evidence per scenario, the differences and the suspected defects are in `verification.md`
next to this file. HTTP status codes are stated where the tests assert them or the code produces them.

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

#### Scenario: Visible filter and creation order

- **WHEN** a visible and a hidden template exist and the list is read with `visible` true, false and without it
- **THEN** the first list holds the visible one only, the second the hidden one only, the third both, the earlier created template first

#### Scenario: Readable by every member

- **WHEN** a Room admin or a User calls `getTemplates` and `getTemplate`
- **THEN** both answer 200 with the template

### Requirement: Creating a metadata template

`createTemplate` (`POST api/2.0/files/metadata/templates`) SHALL be allowed to a DocSpace admin only (others,
a Guest included: 403). `name`: present, non-blank, at most 255 characters, unique on the portal without regard
to case, never the reserved `System` (400); `visible` defaults to true; `fields`: at least one (400 when missing
or empty), validated before anything is stored, so a refused request leaves no template behind. Concurrent
creations of one name SHALL yield one 200, the rest 400.

#### Scenario: Room admin creates a template

- **WHEN** a Room admin, a User or a Guest calls `createTemplate`
- **THEN** the system answers 403 and the name is not listed afterwards

#### Scenario: No fields

- **WHEN** the owner creates a template with `fields` empty or left out
- **THEN** the system answers 400 and `getTemplates` does not list the name

#### Scenario: Blank or missing name

- **WHEN** the `name` is empty, whitespace only or `null`
- **THEN** the system answers 400 and the number of templates is unchanged

#### Scenario: Names kept as sent

- **WHEN** a template is created with a Cyrillic, a CJK or an `A&B 'q' %20 ;` name, or with a name of exactly 255 characters
- **THEN** `getTemplate` returns the name unchanged

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

#### Scenario: Unknown identifier

- **WHEN** `getTemplate` is called with an identifier nobody has, with 0 or with -1
- **THEN** the system answers 404

### Requirement: Template routes and anonymous callers

Every template and field route — `getTemplates`, `getTemplate`, `createTemplate`, `updateTemplate`,
`deleteTemplate`, `createField`, `updateField`, `deleteField` — SHALL answer 401 to a caller without a session,
and SHALL change nothing.

#### Scenario: Anonymous on every route

- **WHEN** an anonymous caller tries each of the eight routes on an existing template and field
- **THEN** every call answers 401 and the template is unchanged

### Requirement: Updating a metadata template

`updateTemplate` (`PUT api/2.0/files/metadata/templates/{templateId}`) SHALL be allowed to a DocSpace admin only
(403, nothing changed) and SHALL change only what the body names: a non-empty `name` (rules of the creation, 400
when taken in any case or too long, old name kept; its own name accepted) and `visible`. It SHALL set `modifiedBy`
to the caller, keep `createBy` and the entries' assignments and values, and answer 200 with the whole template,
fields included. Unknown template: 404.

#### Scenario: Rename and hide

- **WHEN** the owner sends a new `name` and `visible` false
- **THEN** the answer and a later `getTemplate` carry the new name and `visible` false

#### Scenario: Fields survive the answer

- **WHEN** the owner renames a template holding a field `Code`
- **THEN** the answer lists the field `Code`

#### Scenario: Refused rename

- **WHEN** a Room admin, a User or a Guest renames or hides a template
- **THEN** the system answers 403 and `getTemplate` shows the old name and visibility

#### Scenario: Own name and a foreign name

- **WHEN** a template is renamed to its own name, then to another template's name in lower case, then to a 256-character name
- **THEN** the first answers 200, the others 400, and the name is unchanged after them

#### Scenario: Rename keeps the values on the entries

- **WHEN** a template assigned to a folder and a file with values on both is renamed, by another DocSpace admin
- **THEN** both entries list the template under the new name with their values, `createBy` is still the creator and `modifiedBy` the admin

### Requirement: Deleting a metadata template

`deleteTemplate` (`DELETE api/2.0/files/metadata/templates/{templateId}`) SHALL be allowed to a DocSpace admin
only (403, template kept) and SHALL delete the template with its fields, its assignments on every entry and
every value of its fields, answering 200 with no body. Afterwards `getTemplate` and a second deletion SHALL
answer 404, `getTemplates` SHALL not list it, the entries SHALL not report it, and a listing filter naming it
SHALL be refused with 400 like an unknown template.

#### Scenario: Template deleted while assigned

- **WHEN** a room carries the template with a value and the owner deletes the template
- **THEN** `getFolderMetadata` of the room no longer lists the template and `searchFolder` with its `metadataTemplateId` answers 400

#### Scenario: Other templates untouched

- **WHEN** a file and a folder carry two templates with values and one of them is deleted
- **THEN** both entries list the other template alone, its value kept

#### Scenario: Deleted twice

- **WHEN** a deleted template is deleted again, or an unknown identifier is deleted
- **THEN** the system answers 404

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

#### Scenario: Invalid field adds nothing

- **WHEN** `createField` is sent an empty or whitespace name, a 256-character name, a choice field without options or with `a` twice, options on a number field, or an unknown type
- **THEN** the system answers 400 and the template still holds its one field

### Requirement: Adding a field to a template

`createField` (`POST api/2.0/files/metadata/templates/{templateId}/fields`) SHALL be allowed to a DocSpace admin
only (403) and SHALL add one field with `name` (non-blank, at most 255 characters, unique within the template
without regard to case: 400), `type`, `options` and an optional `order`, answering 200 with the field, its
`templateId` and its generated option identifiers. Unknown template: 404.

#### Scenario: Field added

- **WHEN** the owner adds `Department` to a template holding `Client`
- **THEN** `getTemplate` lists `Client` and `Department`

#### Scenario: Name taken in another case

- **WHEN** the owner adds `CLIENT` to a template holding `Client`
- **THEN** the system answers 400

#### Scenario: One field of each type

- **WHEN** a string, a date, a number, a single choice and a multiple choice field are added one by one
- **THEN** each answer carries the requested `type` and the template's identifier, and `getTemplate` lists them after the seed field in the order added

#### Scenario: Unknown template

- **WHEN** a field is added to a template identifier nobody has
- **THEN** the system answers 404

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

#### Scenario: Negative order goes first

- **WHEN** `last` is added without `order` and `top` with `order` -10 to a template holding `first`
- **THEN** the template lists `top`, `first`, `last`
- **AND** an update of a field with `order` -5 and a new name moves it to the front with its options intact

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

- **WHEN** the owner updates a field through the route of a template it does not belong to, or an unknown field through its own
- **THEN** the system answers 404 and the field is unchanged

#### Scenario: Options updated in place

- **WHEN** a choice field with `a` and `b` is sent `options` naming `a`'s identifier with the value `a renamed`, `b` unchanged and a new `c`
- **THEN** the answer and `getTemplate` list `a renamed`, `b`, `c`, the first two under their old identifiers and `c` under a new one

#### Scenario: Retype refused once a file holds a value

- **WHEN** a string field valued on a file is sent `type` number
- **THEN** the system answers 400 and the field keeps the string type

#### Scenario: Sibling name

- **WHEN** field `B` is renamed to `a` while the template holds `A`
- **THEN** the system answers 400 and both names stay

### Requirement: Deleting a field

`deleteField` (`DELETE api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}`) SHALL be allowed to a
DocSpace admin only (403) and SHALL remove the field and every value written for it from every entry, 200 with
no body; the other fields keep their values. The last field of a template SHALL NOT be deleted on its own (400,
field kept): the template goes as a whole. A filter condition naming a deleted field: 400. A field reached
through another template, or deleted twice: 404.

#### Scenario: Field deleted while valued

- **WHEN** a room holds `Client` = `ACME` and `Department` = `Legal` and the owner deletes `Department`
- **THEN** `getTemplate` lists `Client` alone and `getFolderMetadata` of the room lists one field holding `ACME`
- **AND** `searchFolder` with a condition on the deleted field answers 400
- **AND** a file holding values in `a` and `b` keeps `b` = `2` alone after `a` is deleted

#### Scenario: Last field stays

- **WHEN** a template holds `Client` and `Amount`, `Amount` is deleted and then `Client`
- **THEN** the first deletion answers 200 and the second 400, the template still listing `Client`

#### Scenario: Deleted twice or through another template

- **WHEN** a field is deleted through the route of another template, then through its own, then once more
- **THEN** the answers are 404, 200 and 404, and the foreign attempt left both templates unchanged

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
api/2.0/files/metadata/folder/{folderId}`) SHALL need the read right (403; unknown 404) and
answer `templates` — each assigned template with its fields and their `value` or null, `cascade` and
`conflictResolveType` (null unless the folder cascades it; false and null on a file) — `customFields` as
name/value pairs, and `hasMetadataFields`. No metadata: empty lists. The custom-field template stays out.

#### Scenario: Viewer reads a file card

- **WHEN** a room member with the Viewer role reads `getFileMetadata` of a file holding `Client` = `ACME`
- **THEN** the answer lists the template with the field `Client` whose `value.stringValue` is `ACME`

#### Scenario: Entry without metadata

- **WHEN** a fresh folder or file is read, and then an identifier nobody has
- **THEN** the first answers empty `templates` and `customFields`, the second 404

#### Scenario: Stranger

- **WHEN** a User who is not a member of the room reads the file, the folder and `getCascadeProgress` of the folder
- **THEN** every call answers 403

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

#### Scenario: Read member

- **WHEN** a member with the Read role reads the file and the folder, then tries every write on both: assign, unassign, values, custom fields and the cascade switch
- **THEN** the reads answer 200 and every write 403, the entries keeping their template, no value and no custom field

#### Scenario: Room manager

- **WHEN** a Room manager writes values and custom fields on a file and a folder of the room
- **THEN** every call answers 200 and the owner reads the written value

### Requirement: Assigning templates to a file

`assignFileTemplates` (`PUT api/2.0/files/metadata/file/{fileId}/templates`) SHALL take `templateIds` (400 when
the body cannot be read or lacks the member; an empty list changes nothing) and link each listed template to the
file, keeping the ones it carries: additive and idempotent, 200 with no body. A template that does not exist, or
the custom-field template: 404 and nothing of the list linked. Needs the right to edit the file (403); an unknown
file: 404.

#### Scenario: Editor assigns

- **WHEN** an Editor of the room calls `assignFileTemplates` for a file of the room
- **THEN** the system answers 200 and `getFileMetadata` lists the template with every field empty, `cascade` false

#### Scenario: Idempotent and additive

- **WHEN** template A is assigned twice, then B, then an empty list
- **THEN** every call answers 200 and the file carries A and B

#### Scenario: Unknown template in the list

- **WHEN** a known and an unknown template are sent together, or any template to an unknown file
- **THEN** the system answers 404 and the file carries nothing

#### Scenario: Re-assigned after unassignment

- **WHEN** a template unassigned from a file (its value gone with it) is assigned again
- **THEN** the file lists the template with the field empty

### Requirement: Assigning templates to a folder or a room

`assignFolderTemplates` (`PUT api/2.0/files/metadata/folder/{folderId}/templates`) SHALL take `templateIds`,
`cascade` and `conflictResolveType`, link the templates to the folder additively (403 without the folder write
right; 404 for a missing folder or template, nothing linked) and answer an operation: completed, no `id`,
progress 100, no error when `cascade` is off or the list is empty (content untouched); the queued pass with
`id` and `templateIds` when `cascade` is on.

#### Scenario: No cascade

- **WHEN** the owner assigns a template to a room with `cascade` false
- **THEN** the answer is `{id: null, progress: 100, isCompleted: true, error: null}` and `getCascadeProgress` of the room reports the same
- **AND** a file inside the folder carries nothing, while the folder lists the template with `cascade` false and every field

#### Scenario: Additive and atomic

- **WHEN** template A is assigned twice and then B, and another folder is sent A together with an unknown template
- **THEN** the first folder carries A and B; the second call answers 404 and that folder carries nothing; an unknown folder answers 404

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

#### Scenario: Template the file does not carry

- **WHEN** `unassignFileTemplate` names a template the file does not carry, then one that does not exist
- **THEN** the first answers 200 and changes nothing, the second 404; a missing folder or template on `unassignFolderTemplate` answers 404 too

#### Scenario: Other template kept

- **WHEN** a file carries two templates with a value each and one is unassigned
- **THEN** the file lists the other template with its value

#### Scenario: Inherited template unassigned, then cascaded again

- **WHEN** a file that inherited a template from a cascading folder has it unassigned, and the folder's cascade is switched on again
- **THEN** the file carries nothing in between and the template again after the pass

### Requirement: Setting the values of template fields

`setFileValues` (`PUT api/2.0/files/metadata/file/{fileId}/values`) and `setFolderValues` (`PUT
api/2.0/files/metadata/folder/{folderId}/values`) SHALL take `values`, one per `fieldId` (null list or a field
listed twice: 400), each of a template assigned to the entry and not a custom field (400); unknown field: 404.
A refused request writes nothing. A value whose member is empty, null or left out clears the field; fields not
listed stay. The answer is the whole metadata of the entry.

#### Scenario: Answer is the entry, not the request

- **WHEN** a room holds `Client` = `ACME` and a custom field, and the owner writes `Amount` = 7
- **THEN** the answer lists the template with `Client` = `ACME` and `Amount` = 7, and the custom field

#### Scenario: Every type in one request

- **WHEN** a string, a number, a date, a single and a multiple choice value are written on a file or a folder in one request
- **THEN** the answer and the later read carry all five, the string with its Cyrillic and CJK characters intact

#### Scenario: One invalid value spoils the batch

- **WHEN** a valid string and a number one beyond the limit, a string on a number field, a field of a template the entry does not carry, a field listed twice or an unknown field are sent together
- **THEN** the system answers 400 (404 for the unknown field) and the string field keeps its old value

#### Scenario: Member left out clears

- **WHEN** a value carries `fieldId` and no other member, for a field of any type on a file or a folder
- **THEN** the system answers 200, the field is empty and the sibling field keeps its value
- **AND** an empty string on a string field, null on a number or date field, and an empty or null `optionIds` on a choice field clear the same way

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

- **WHEN** a single choice field receives two different options, or any choice field an option it does not have, an option of another field, or a text that is no identifier
- **THEN** the system answers 400 and the selected option stays

#### Scenario: Member of another type

- **WHEN** a string field receives `numberValue` or `optionIds`, a number field `stringValue` or `optionIds`, a date field `stringValue` or `numberValue`, a choice field `stringValue`
- **THEN** the system answers 400 and the field keeps its value, on a file and on a folder alike

#### Scenario: Fraction and overflow

- **WHEN** a number field receives `42.5` or `1e30`
- **THEN** the system answers 400 and the field keeps its value

#### Scenario: Zero, negative and blank

- **WHEN** a number field receives 0 or -7, and a string field three spaces
- **THEN** each is stored and read back as sent

### Requirement: Date values and their time zone

A `dateValue` carrying an offset SHALL be stored as that instant in UTC; one without an offset SHALL be read as
UTC, never shifted by the server time zone, so a day written as a date is found by a filter on the same day; a
date without a time is its midnight UTC. Any instant of the years 1 to 9999 SHALL be accepted; a year outside,
a month that does not exist or text that is no date: 400. The stored date SHALL come back in `value.dateValue`
with its UTC time.

#### Scenario: Midnight without offset

- **WHEN** `2026-01-15T00:00:00` is written and the room is filtered with `from` and `to` both `2026-01-15`
- **THEN** the stored UTC time is 2026-01-15 00:00 and the room is found

#### Scenario: Written forms

- **WHEN** `2026-03-04`, `2026-03-04T05:06:07` and `2026-03-04T05:06:07+03:00` are written on a file or a folder
- **THEN** the stored UTC times are 00:00:00, 05:06:07 and 02:06:07 of that day

#### Scenario: Extremes

- **WHEN** `0001-01-01T00:00:00Z`, `9999-12-31T23:59:59Z` or `1970-01-01T00:00:00Z` is written
- **THEN** each is stored and read back as that instant

#### Scenario: Not a date

- **WHEN** `0000-01-01`, `10000-01-01`, `2026-13-01` or `not a date` is written
- **THEN** the system answers 400 and the field keeps its value

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

#### Scenario: Never cascaded, never mixed with the template values

- **WHEN** a folder holds custom fields and cascades a template over a file inside, and a file holding a template value receives a custom field
- **THEN** the file inside the folder has no custom field, and the template value of the other file is unchanged next to its new custom field

#### Scenario: Refused writes change nothing

- **WHEN** an empty list, a blank name, `a` and `A` together, a 256-character name or an 8001-character value is sent to a file or a folder holding `keep` = `me`
- **THEN** the system answers 400 and the entry still holds `keep` = `me` alone; an unknown file answers 404

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

#### Scenario: Every depth

- **WHEN** a folder holding `from root` cascades in overwrite mode over a sub-folder, a folder two levels down and files in both
- **THEN** every folder and file below carries the template with `from root`, and the folder reports `cascade` true with the overwrite mode

#### Scenario: Skip then overwrite

- **WHEN** a folder holding `folder` cascades in skip mode over a file holding `file`, an empty file, a sub-folder holding `sub` and an empty sub-folder, and is then switched to overwrite mode
- **THEN** after the skip pass the four read `file`, `folder`, `sub`, `folder`; after the overwrite pass all four read `folder`

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

#### Scenario: Finished pass stays reported

- **WHEN** the pass has completed and the progress is read again
- **THEN** the same `id` and `templateIds` come back with `isCompleted` true, progress 100 and no error

#### Scenario: Unknown folder

- **WHEN** the progress of a folder identifier nobody has is read
- **THEN** the system answers 404

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

- **WHEN** the switch is called for a template the folder does not carry, or for one that does not exist
- **THEN** the system answers 404

#### Scenario: Switched on from an inherited sub-folder

- **WHEN** a sub-folder that inherited a template from its cascading parent (reporting `cascade` false) is switched on
- **THEN** the sub-folder reports `cascade` true and the parent still does

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

### Requirement: Whether an entry has fields to fill

Every file and folder of a folder listing, `getFileInfo`, `getFolderInfo`, `getFileMetadata` and
`getFolderMetadata` SHALL carry `hasMetadataFields`: true when the entry carries a visible template with at
least one field or a custom field, false otherwise — always present there, false rather than absent on a portal
without templates. It is a fact about the data, not about the caller's rights, and it says nothing about the
values. It SHALL follow the visibility of the templates.

#### Scenario: False, not absent

- **WHEN** a portal has no template and a file and a folder of a room are listed and read
- **THEN** the listing, the info endpoints and the metadata reads all carry `hasMetadataFields` false

#### Scenario: Visible template

- **WHEN** a template with a field is assigned to a file and a folder, a third file untouched
- **THEN** the two carry true and the third false everywhere

#### Scenario: Hidden template only

- **WHEN** a hidden template is the only one assigned to a file
- **THEN** the listing reports `hasMetadataFields` false while `assignedMetadataTemplates` still names the template

#### Scenario: Custom field only

- **WHEN** a file and a folder hold a custom field and no template
- **THEN** both carry true, and the file's listing entry carries no `assignedMetadataTemplates`

#### Scenario: Follows the visibility

- **WHEN** the assigned template is hidden with `updateTemplate` and shown again
- **THEN** the flag turns false and then true again on the listing, the info and the metadata read

#### Scenario: Filled and read-only

- **WHEN** every field of the file is filled, and a member with the Read role lists the room
- **THEN** the flag stays true; the Read member sees true next to `security.edit` false

### Requirement: Text of names, options and values

A template name, a field name, an option value, a string value and a custom field name or value SHALL store
every character of the Basic Multilingual Plane as sent, Cyrillic, CJK and symbols included, and return it
unchanged from `getTemplate` and the metadata reads.

#### Scenario: Round trip

- **WHEN** `Привет 模板 ™ ✓ é ☃ ❤ ☺` is used in a template name, a field name, an option, a string value and a custom field
- **THEN** every read returns the text unchanged

#### Scenario: Character outside the Basic Multilingual Plane

- **WHEN** a template, a field or a file value is written with an emoji in its text
- **THEN** the write fails with a 500 from saving; the text is not stored as sent

### Requirement: Assigned templates in the listings

Every file and folder returned by a listing, by `getFileInfo` and by `getFolderInfo` SHALL carry
`assignedMetadataTemplates`, the identifiers of the templates it carries, empty or absent when it carries none;
the info endpoints and the listing SHALL agree. A portal that gets its first template after a listing SHALL
report the assignments made afterwards. The entries of a listing SHALL name their concrete kind in `entryKind`
(`file`, `folder`, `thirdPartyFile`, `thirdPartyFolder`).

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

The filter SHALL apply on the rooms (active, archive, Forms, private), on a folder or room, on My documents, Trash,
Recent, Favorites and Shared with me, by numeric identifier and by alias (`@my`, `@trash`, `@recent`,
`@favorites`, `@share`, `@forms`), validated the same way. The Templates sections and the legacy Privacy folder
SHALL refuse a non-empty filter with 400, by identifier, alias or typed search. Third-party entries carry no
metadata.

#### Scenario: Private rooms section

- **WHEN** the private rooms section holds a private room with the template and a bare one, and is listed by its identifier, searched by `searchFolder` or listed as the rooms with `searchArea` `PrivateRooms`, with the template
- **THEN** only the room carrying the template comes back

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
