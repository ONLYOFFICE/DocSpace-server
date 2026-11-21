## MODIFIED Requirements

### Requirement: Attachment of a file stored in a project

`aiAttachmentsSaveFilesMany` for a file the caller stored in an AI project SHALL answer one attachment per
file: a repeated call for the same file SHALL return the same attachment, with the extracted text for a
document and as an image attachment for a picture. Once the caller deleted that attachment, a call for the
file SHALL answer 404 and store nothing of the batch, before the background pass and after it.

#### Scenario: Document attached twice

- **WHEN** the owner uploads `notes.txt` into the project and attaches it twice
- **THEN** both answers carry one attachment id, the kind `file`, the file's id as `entryId` and the text of the file

#### Scenario: Picture attached twice

- **WHEN** the owner uploads a PNG picture into the project and attaches it twice
- **THEN** both answers carry one attachment id and the kind `image`

#### Scenario: Deleted attachment attached again

- **WHEN** a Content creator of a project uploads a file into the project, attaches it, deletes the attachment and attaches the file again in a batch with a file of their My documents
- **THEN** the system answers 404, no attachment of the My documents file is stored, and after the next background pass the project file answers 404 and the deleted attachment reads as null
