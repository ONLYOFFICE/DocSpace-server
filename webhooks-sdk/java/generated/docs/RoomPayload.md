

# RoomPayload

ASC.Files/Core/ApiModels/WebhookDto/RoomWebhookDto.cs. The room.* triggers and every agent.* trigger - an agent is an AI room and has the same shape today.  Not carried: logo, watermark, lifetime, tags, chatSettings. Each needs per-request URL building or an extra query; read them from GET api/2.0/files/rooms/{id}. 

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
|**roomType** | **Integer** | enum RoomType |  [optional] |
|**type** | **Integer** | enum FolderType |  [optional] |
|**filesCount** | **Integer** |  |  [optional] |
|**foldersCount** | **Integer** |  |  [optional] |
|**_private** | **Boolean** |  |  [optional] |
|**indexing** | **Boolean** |  |  [optional] |
|**denyDownload** | **Boolean** |  |  [optional] |
|**pinned** | **Boolean** |  |  [optional] |
|**quotaLimit** | **Long** |  |  [optional] |
|**usedSpace** | **Long** |  |  [optional] |
|**color** | **String** |  |  [optional] |
|**cover** | **String** |  |  [optional] |



