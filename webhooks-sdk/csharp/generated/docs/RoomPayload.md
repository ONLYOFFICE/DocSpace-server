# DocSpace.Webhooks.SDK.Model.RoomPayload
ASC.Files/Core/ApiModels/WebhookDto/RoomWebhookDto.cs. The room.* triggers and every agent.* trigger - an agent is an AI room and has the same shape today.  Not carried: logo, watermark, lifetime, tags, chatSettings. Each needs per-request URL building or an extra query; read them from GET api/2.0/files/rooms/{id}. 

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
**RoomType** | **int** | enum RoomType | [optional] 
**Type** | **int** | enum FolderType | [optional] 
**FilesCount** | **int** |  | [optional] 
**FoldersCount** | **int** |  | [optional] 
**Private** | **bool** |  | [optional] 
**Indexing** | **bool** |  | [optional] 
**DenyDownload** | **bool** |  | [optional] 
**Pinned** | **bool** |  | [optional] 
**QuotaLimit** | **long** |  | [optional] 
**UsedSpace** | **long** |  | [optional] 
**Color** | **string** |  | [optional] 
**Cover** | **string** |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

