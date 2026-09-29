# DocSpace.Webhooks.SDK.Model.FolderPayload
ASC.Files/Core/ApiModels/WebhookDto/FolderWebhookDto.cs. The folder.* triggers. Rooms are RoomPayload, never this, even though both come from Folder<T>.  Not carried: inRoom, mute, new - all per-viewer state. 

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
**FilesCount** | **int** |  | [optional] 
**FoldersCount** | **int** |  | [optional] 
**Type** | **int** | enum FolderType | [optional] 
**IsShareable** | **bool** |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

