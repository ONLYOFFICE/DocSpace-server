# Spec Delta

## MODIFIED Requirements

### Requirement: Reading and deleting attachments

`aiAttachmentsGet`, `aiAttachmentsGetMany`, `aiAttachmentsDelete` and `aiAttachmentsDeleteMany` SHALL see
the caller's drafts only: another user's draft reads as null in place and is not deleted; an unknown id
reads as null and deletes with success without stopping a batch; a malformed id, an empty list or a missing
body 400. A draft is readable right after it is saved. A deleted draft SHALL stay readable until the next
background pass and be gone after it; a linked attachment SHALL stay.

#### Scenario: Another user's draft

- **WHEN** a User asks `aiAttachmentsGetMany` for the owner's draft id next to their own
- **THEN** the own draft is returned and the owner's slot is null

#### Scenario: Mixed delete

- **WHEN** a User deletes their own draft and the owner's in one `aiAttachmentsDeleteMany`, and the background pass has run
- **THEN** only the User's draft is gone

#### Scenario: Deleted draft

- **WHEN** the owner deletes a draft made from a file of their My documents, and the background pass has run
- **THEN** the draft reads as null and `getFileInfo` still answers for the file

#### Scenario: Linked attachment

- **WHEN** the owner links a draft to a message, deletes it, and the background pass has run
- **THEN** the attachment still reads back

#### Scenario: Draft reaches the model

- **WHEN** the owner sends a message carrying a draft by id only, and then one carrying the whole record inline
- **THEN** the round ends healthy but no streamed request to the model carries the draft's text, either time (bug 82773)

#### Scenario: Unpaid portal

- **WHEN** the AI Tools service is off on a hosted portal
- **THEN** the whole draft lifecycle still works

## ADDED Requirements

### Requirement: Drafts that are not sent are removed

A background pass, every 15 minutes by default, SHALL remove each draft its creator deleted and each draft
not linked to a message 2 hours, by default, after it was created; for a file stored in an AI project the
draft counts from the upload or the copy. With the draft it SHALL remove the file the draft was made from
when that file still lies in an AI project unsent, and return its size to the project's space. Any other
file and any linked attachment SHALL stay.

#### Scenario: Deleted draft of a file in a project

- **WHEN** a Content creator of a project uploads a file into the project, attaches it, deletes the draft, and the background pass has run
- **THEN** the draft reads as null, the file answers 404 and the project's used space equals what it was before the upload

#### Scenario: Abandoned draft of a file in a project

- **WHEN** a Content creator uploads a file into a project and attaches it, and the draft is not linked for 2 hours
- **THEN** after the next background pass the draft reads as null, the file answers 404 and the project's used space equals what it was before the upload

#### Scenario: Abandoned draft of a file outside a project

- **WHEN** the owner attaches a file of their My documents and the draft is not linked for 2 hours
- **THEN** after the next background pass the draft reads as null and `getFileInfo` still answers for the file

#### Scenario: Sent attachment older than the lifetime

- **WHEN** a Content creator uploads a file into a project, attaches it and links it to a message of a project thread, and 2 hours pass after the upload
- **THEN** after the next background pass `getThreadAttachments` still lists the file
