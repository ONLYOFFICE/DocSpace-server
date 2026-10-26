# DocSpace.Webhooks.SDK.Model.FilePayload
The file carried by a file.* webhook, and by form.filled.out and form.stopped.  A copy of FileDto. Not carried: thumbnailUrl, dimensions, viewAccessibility, formFillingStatus, hasDraft and the form-role fields. Each needs per-request work the REST helper does and a delivery must not; dimensions in particular opens the file stream to measure the image.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | [**EntryId**](EntryId.md) |  | [optional] 
**ParentId** | [**EntryId**](EntryId.md) |  | [optional] 
**RootFolderId** | [**EntryId**](EntryId.md) |  | [optional] 
**Title** | **string** | The file entry title. | [optional] 
**FileEntryType** | **int** | The file entry type: 1 for a folder, 2 for a file. Present on every entry payload. Flags of &#x60;FileEntryType&#x60;: 1 &#x3D; Folder, 2 &#x3D; File. | [optional] 
**Created** | **DateTime** | The UTC date when the file entry was created. | [optional] 
**CreatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**Updated** | **DateTime** | The UTC date when the file entry was last updated. | [optional] 
**UpdatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**RootFolderType** | **int** | The root folder type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**ParentRoomType** | **int** | The parent room type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**OriginId** | [**EntryId**](EntryId.md) |  | [optional] 
**OriginRoomId** | [**EntryId**](EntryId.md) |  | [optional] 
**OriginTitle** | **string** | The origin title of the file entry. | [optional] 
**OriginRoomTitle** | **string** | The origin room title of the file entry. | [optional] 
**ProviderItem** | **bool** | Specifies whether the file entry is backed by a third-party provider or not. | [optional] 
**ProviderKey** | **string** | The third-party provider key of the file entry. | [optional] 
**ProviderId** | **int** | The third-party provider ID of the file entry. | [optional] 
**Order** | **int** | The position of the file entry within an indexed room. | [optional] 
**VarVersion** | **int** | The file version. | [optional] 
**VersionGroup** | **int** | The version group of the file. | [optional] 
**ContentLength** | **long** | The file size in bytes, as a number; the REST FileDto sends a formatted string instead. | [optional] 
**FileType** | **int** | The file type. Values of &#x60;FileType&#x60;: 0 &#x3D; Unknown, 1 &#x3D; Archive, 2 &#x3D; Video, 3 &#x3D; Audio, 4 &#x3D; Image, 5 &#x3D; Spreadsheet, 6 &#x3D; Presentation, 7 &#x3D; Document, 10 &#x3D; Pdf, 11 &#x3D; Diagram. | [optional] 
**FileExst** | **string** | The file extension, including the leading dot. | [optional] 
**Comment** | **string** | The comment on the current file version. | [optional] 
**ViewUrl** | **string** | The URL the file content can be downloaded from. Carries no share token. | [optional] 
**WebUrl** | **string** | The URL the file can be opened at in a browser. Carries no share token. | [optional] 
**Encrypted** | **bool** | Specifies whether the file is encrypted or not. | [optional] 
**Locked** | **bool** | Specifies whether the file is locked or not. | [optional] 
**LockedBy** | **string** | The user name of whoever locked the file. | [optional] 
**IsForm** | **bool** | True for every PDF file, with or without form fields, since the editor can open any PDF as a form; absent for other files. It does not tell a fillable form from an ordinary PDF document. | [optional] 
**CustomFilterEnabled** | **bool** | Specifies whether a custom filter is enabled for the file or not. | [optional] 
**CustomFilterEnabledBy** | **string** | The user name of whoever enabled the custom filter. | [optional] 
**LastOpened** | **DateTime** | The UTC date when the file was last opened. | [optional] 
**VectorizationStatus** | **int** | The AI vectorization status of the file. Values of &#x60;VectorizationStatus&#x60;: 0 &#x3D; InProgress, 1 &#x3D; Completed, 2 &#x3D; Failed. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

