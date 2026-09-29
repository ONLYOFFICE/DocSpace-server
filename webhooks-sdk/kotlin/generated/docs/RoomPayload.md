
# RoomPayload

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
| **roomType** | **kotlin.Int** | enum RoomType |  [optional] |
| **type** | **kotlin.Int** | enum FolderType |  [optional] |
| **filesCount** | **kotlin.Int** |  |  [optional] |
| **foldersCount** | **kotlin.Int** |  |  [optional] |
| **&#x60;private&#x60;** | **kotlin.Boolean** |  |  [optional] |
| **indexing** | **kotlin.Boolean** |  |  [optional] |
| **denyDownload** | **kotlin.Boolean** |  |  [optional] |
| **pinned** | **kotlin.Boolean** |  |  [optional] |
| **quotaLimit** | **kotlin.Long** |  |  [optional] |
| **usedSpace** | **kotlin.Long** |  |  [optional] |
| **color** | **kotlin.String** |  |  [optional] |
| **cover** | **kotlin.String** |  |  [optional] |



