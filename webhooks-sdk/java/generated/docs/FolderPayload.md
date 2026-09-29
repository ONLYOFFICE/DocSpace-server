

# FolderPayload

ASC.Files/Core/ApiModels/WebhookDto/FolderWebhookDto.cs. The folder.* triggers. Rooms are RoomPayload, never this, even though both come from Folder<T>.  Not carried: inRoom, mute, new - all per-viewer state. 

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
|**filesCount** | **Integer** |  |  [optional] |
|**foldersCount** | **Integer** |  |  [optional] |
|**type** | **Integer** | enum FolderType |  [optional] |
|**isShareable** | **Boolean** |  |  [optional] |



