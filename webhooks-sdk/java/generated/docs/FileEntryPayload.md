

# FileEntryPayload

ASC.Files/Core/ApiModels/WebhookDto/FileEntryWebhookDto.cs. What FilePayload, FolderPayload and RoomPayload have in common.  This schema is never sent on its own -- unlike the previous contract, where every file, folder, room, agent and form trigger sent exactly this and nothing else. The old payload was typed as the abstract FileEntry<T> at the publish site, so System.Text.Json serialized by DECLARED type and every File<T> and Folder<T> member was silently dropped: version, contentLength, fileType, folderType, filesCount, roomType never reached a receiver. That is fixed; the subtypes below carry their own fields.  Not carried, deliberately: access, security, securityByUsers, availableShareRights, shareSettings, canShare, shared, sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. Those answer \"what may the caller see\", and a delivery has no caller - who receives it is decided by WebhookFileEntryAccessChecker against the subscription owner. Putting one user's permission matrix on the wire was both meaningless to the receiver and a disclosure. 

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



