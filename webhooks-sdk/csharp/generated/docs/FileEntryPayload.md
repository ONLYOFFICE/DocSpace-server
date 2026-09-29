# DocSpace.Webhooks.SDK.Model.FileEntryPayload
ASC.Files/Core/ApiModels/WebhookDto/FileEntryWebhookDto.cs. What FilePayload, FolderPayload and RoomPayload have in common.  This schema is never sent on its own - - unlike the previous contract, where every file, folder, room, agent and form trigger sent exactly this and nothing else. The old payload was typed as the abstract FileEntry<T> at the publish site, so System.Text.Json serialized by DECLARED type and every File<T> and Folder<T> member was silently dropped: version, contentLength, fileType, folderType, filesCount, roomType never reached a receiver. That is fixed; the subtypes below carry their own fields.  Not carried, deliberately: access, security, securityByUsers, availableShareRights, shareSettings, canShare, shared, sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. Those answer \"what may the caller see\", and a delivery has no caller - who receives it is decided by WebhookFileEntryAccessChecker against the subscription owner. Putting one user's permission matrix on the wire was both meaningless to the receiver and a disclosure. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | [**EntryId**](EntryId.md) |  | [optional] 
**ParentId** | [**EntryId**](EntryId.md) |  | [optional] 
**RootFolderId** | [**EntryId**](EntryId.md) |  | [optional] 
**Title** | **string** |  | [optional] 
**FileEntryType** | **int** | 1 folder, 2 file. Present on every entry payload. | [optional] 
**Created** | **DateTime** |  | [optional] 
**CreatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**Updated** | **DateTime** |  | [optional] 
**UpdatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**RootFolderType** | **int** | enum FolderType | [optional] 
**ParentRoomType** | **int** | enum FolderType | [optional] 
**OriginId** | [**EntryId**](EntryId.md) |  | [optional] 
**OriginRoomId** | [**EntryId**](EntryId.md) |  | [optional] 
**OriginTitle** | **string** |  | [optional] 
**OriginRoomTitle** | **string** |  | [optional] 
**ProviderItem** | **bool** |  | [optional] 
**ProviderKey** | **string** |  | [optional] 
**ProviderId** | **int** |  | [optional] 
**Order** | **int** | Position within an indexed room. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

