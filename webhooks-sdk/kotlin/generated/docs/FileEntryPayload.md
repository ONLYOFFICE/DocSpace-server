
# FileEntryPayload

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



