
# FilePayload

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | [**EntryId**](EntryId.md) |  |  [optional] |
| **parentId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **rootFolderId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **title** | **kotlin.String** |  |  [optional] |
| **fileEntryType** | **kotlin.Int** | 1 folder, 2 file. Present on every entry payload. |  [optional] |
| **created** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **createdBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
| **updated** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **updatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
| **rootFolderType** | **kotlin.Int** | enum FolderType |  [optional] |
| **parentRoomType** | **kotlin.Int** | enum FolderType |  [optional] |
| **originId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **originRoomId** | [**EntryId**](EntryId.md) |  |  [optional] |
| **originTitle** | **kotlin.String** |  |  [optional] |
| **originRoomTitle** | **kotlin.String** |  |  [optional] |
| **providerItem** | **kotlin.Boolean** |  |  [optional] |
| **providerKey** | **kotlin.String** |  |  [optional] |
| **providerId** | **kotlin.Int** |  |  [optional] |
| **order** | **kotlin.Int** | Position within an indexed room. |  [optional] |
| **version** | **kotlin.Int** |  |  [optional] |
| **versionGroup** | **kotlin.Int** |  |  [optional] |
| **contentLength** | **kotlin.Long** | Bytes, as a number. The REST DTO sends a formatted string. |  [optional] |
| **fileType** | **kotlin.Int** | enum FileType |  [optional] |
| **fileExst** | **kotlin.String** | Includes the leading dot, e.g. \&quot;.docx\&quot;. |  [optional] |
| **comment** | **kotlin.String** |  |  [optional] |
| **viewUrl** | **kotlin.String** | Download URL. Carries no share token. |  [optional] |
| **webUrl** | **kotlin.String** | Browser URL. Carries no share token. |  [optional] |
| **encrypted** | **kotlin.Boolean** |  |  [optional] |
| **locked** | **kotlin.Boolean** |  |  [optional] |
| **lockedBy** | **kotlin.String** |  |  [optional] |
| **isForm** | **kotlin.Boolean** |  |  [optional] |
| **customFilterEnabled** | **kotlin.Boolean** |  |  [optional] |
| **customFilterEnabledBy** | **kotlin.String** |  |  [optional] |
| **lastOpened** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **vectorizationStatus** | **kotlin.Int** | enum VectorizationStatus |  [optional] |



