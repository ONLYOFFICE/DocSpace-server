

# FilePayload

ASC.Files/Core/ApiModels/WebhookDto/FileWebhookDto.cs. A copy of the REST FileDto. Sent by every file.* trigger and by form.filled.out and form.stopped.  Not carried: thumbnailUrl, dimensions, viewAccessibility, formFillingStatus, hasDraft and the form-role fields. Each needs per-request work the REST helper does and a delivery must not - dimensions in particular opens the file stream to measure the image. 

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | [**EntryId**](EntryId.md) |  |  [optional] |
|**parentId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**rootFolderId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**title** | **String** |  |  [optional] |
|**fileEntryType** | **Integer** | 1 folder, 2 file. Present on every entry payload. |  [optional] |
|**created** | **OffsetDateTime** |  |  [optional] |
|**createdBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
|**updated** | **OffsetDateTime** |  |  [optional] |
|**updatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
|**rootFolderType** | **Integer** | enum FolderType |  [optional] |
|**parentRoomType** | **Integer** | enum FolderType |  [optional] |
|**originId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**originRoomId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**originTitle** | **String** |  |  [optional] |
|**originRoomTitle** | **String** |  |  [optional] |
|**providerItem** | **Boolean** |  |  [optional] |
|**providerKey** | **String** |  |  [optional] |
|**providerId** | **Integer** |  |  [optional] |
|**order** | **Integer** | Position within an indexed room. |  [optional] |
|**version** | **Integer** |  |  [optional] |
|**versionGroup** | **Integer** |  |  [optional] |
|**contentLength** | **Long** | Bytes, as a number. The REST DTO sends a formatted string. |  [optional] |
|**fileType** | **Integer** | enum FileType |  [optional] |
|**fileExst** | **String** | Includes the leading dot, e.g. \&quot;.docx\&quot;. |  [optional] |
|**comment** | **String** |  |  [optional] |
|**viewUrl** | **String** | Download URL. Carries no share token. |  [optional] |
|**webUrl** | **String** | Browser URL. Carries no share token. |  [optional] |
|**encrypted** | **Boolean** |  |  [optional] |
|**locked** | **Boolean** |  |  [optional] |
|**lockedBy** | **String** |  |  [optional] |
|**isForm** | **Boolean** |  |  [optional] |
|**customFilterEnabled** | **Boolean** |  |  [optional] |
|**customFilterEnabledBy** | **String** |  |  [optional] |
|**lastOpened** | **OffsetDateTime** |  |  [optional] |
|**vectorizationStatus** | **Integer** | enum VectorizationStatus |  [optional] |



