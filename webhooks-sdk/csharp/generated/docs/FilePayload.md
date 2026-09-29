# DocSpace.Webhooks.SDK.Model.FilePayload
ASC.Files/Core/ApiModels/WebhookDto/FileWebhookDto.cs. A copy of the REST FileDto. Sent by every file.* trigger and by form.filled.out and form.stopped.  Not carried: thumbnailUrl, dimensions, viewAccessibility, formFillingStatus, hasDraft and the form-role fields. Each needs per-request work the REST helper does and a delivery must not - dimensions in particular opens the file stream to measure the image. 

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
**VarVersion** | **int** |  | [optional] 
**VersionGroup** | **int** |  | [optional] 
**ContentLength** | **long** | Bytes, as a number. The REST DTO sends a formatted string. | [optional] 
**FileType** | **int** | enum FileType | [optional] 
**FileExst** | **string** | Includes the leading dot, e.g. \&quot;.docx\&quot;. | [optional] 
**Comment** | **string** |  | [optional] 
**ViewUrl** | **string** | Download URL. Carries no share token. | [optional] 
**WebUrl** | **string** | Browser URL. Carries no share token. | [optional] 
**Encrypted** | **bool** |  | [optional] 
**Locked** | **bool** |  | [optional] 
**LockedBy** | **string** |  | [optional] 
**IsForm** | **bool** |  | [optional] 
**CustomFilterEnabled** | **bool** |  | [optional] 
**CustomFilterEnabledBy** | **string** |  | [optional] 
**LastOpened** | **DateTime** |  | [optional] 
**VectorizationStatus** | **int** | enum VectorizationStatus | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

