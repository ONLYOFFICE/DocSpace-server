# FilePayload

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
**version** | **Int** |  | [optional] 
**versionGroup** | **Int** |  | [optional] 
**contentLength** | **Int64** | Bytes, as a number. The REST DTO sends a formatted string. | [optional] 
**fileType** | **Int** | enum FileType | [optional] 
**fileExst** | **String** | Includes the leading dot, e.g. \&quot;.docx\&quot;. | [optional] 
**comment** | **String** |  | [optional] 
**viewUrl** | **String** | Download URL. Carries no share token. | [optional] 
**webUrl** | **String** | Browser URL. Carries no share token. | [optional] 
**encrypted** | **Bool** |  | [optional] 
**locked** | **Bool** |  | [optional] 
**lockedBy** | **String** |  | [optional] 
**isForm** | **Bool** |  | [optional] 
**customFilterEnabled** | **Bool** |  | [optional] 
**customFilterEnabledBy** | **String** |  | [optional] 
**lastOpened** | **Date** |  | [optional] 
**vectorizationStatus** | **Int** | enum VectorizationStatus | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


