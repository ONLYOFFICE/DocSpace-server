# FileEntryPayload

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**EntryId**](EntryId.md) |  | [optional] 
**parentId** | [**EntryId**](EntryId.md) |  | [optional] 
**rootFolderId** | [**EntryId**](EntryId.md) |  | [optional] 
**title** | **String** |  | [optional] 
**fileEntryType** | **Int** | 1 folder, 2 file. Present on every entry payload. | [optional] 
**created** | **Date** |  | [optional] 
**createdBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**updated** | **Date** |  | [optional] 
**updatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**rootFolderType** | **Int** | enum FolderType | [optional] 
**parentRoomType** | **Int** | enum FolderType | [optional] 
**originId** | [**EntryId**](EntryId.md) |  | [optional] 
**originRoomId** | [**EntryId**](EntryId.md) |  | [optional] 
**originTitle** | **String** |  | [optional] 
**originRoomTitle** | **String** |  | [optional] 
**providerItem** | **Bool** |  | [optional] 
**providerKey** | **String** |  | [optional] 
**providerId** | **Int** |  | [optional] 
**order** | **Int** | Position within an indexed room. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


