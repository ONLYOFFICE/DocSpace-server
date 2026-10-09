
# FolderPayload

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **parentId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **rootFolderId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **title** | **kotlin.String** |  |  [optional] |
| **fileEntryType** | **kotlin.Int** | 1 folder, 2 file. Present on every entry payload. |  [optional] |
| **created** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **createdBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
| **updated** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) |  |  [optional] |
| **updatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
| **rootFolderType** | **kotlin.Int** | enum FolderType |  [optional] |
| **parentRoomType** | **kotlin.Int** | enum FolderType |  [optional] |
| **originId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **originRoomId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **originTitle** | **kotlin.String** |  |  [optional] |
| **originRoomTitle** | **kotlin.String** |  |  [optional] |
| **providerItem** | **kotlin.Boolean** |  |  [optional] |
| **providerKey** | **kotlin.String** |  |  [optional] |
| **providerId** | **kotlin.Int** |  |  [optional] |
| **order** | **kotlin.Int** | Position within an indexed room. |  [optional] |
| **filesCount** | **kotlin.Int** |  |  [optional] |
| **foldersCount** | **kotlin.Int** |  |  [optional] |
| **type** | **kotlin.Int** | enum FolderType |  [optional] |
| **isShareable** | **kotlin.Boolean** |  |  [optional] |



