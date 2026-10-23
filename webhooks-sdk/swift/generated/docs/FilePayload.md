# FilePayload

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**EntryId**](EntryId.md) |  | [optional] 
**parentId** | [**EntryId**](EntryId.md) |  | [optional] 
**rootFolderId** | [**EntryId**](EntryId.md) |  | [optional] 
**title** | **String** | The file entry title. | [optional] 
**fileEntryType** | **Int** | The file entry type: 1 for a folder, 2 for a file. Present on every entry payload. Flags of &#x60;FileEntryType&#x60;: 1 &#x3D; Folder, 2 &#x3D; File. | [optional] 
**created** | **Date** | The UTC date when the file entry was created. | [optional] 
**createdBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**updated** | **Date** | The UTC date when the file entry was last updated. | [optional] 
**updatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**rootFolderType** | **Int** | The root folder type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**parentRoomType** | **Int** | The parent room type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**originId** | [**EntryId**](EntryId.md) |  | [optional] 
**originRoomId** | [**EntryId**](EntryId.md) |  | [optional] 
**originTitle** | **String** | The origin title of the file entry. | [optional] 
**originRoomTitle** | **String** | The origin room title of the file entry. | [optional] 
**providerItem** | **Bool** | Specifies whether the file entry is backed by a third-party provider or not. | [optional] 
**providerKey** | **String** | The third-party provider key of the file entry. | [optional] 
**providerId** | **Int** | The third-party provider ID of the file entry. | [optional] 
**order** | **Int** | The position of the file entry within an indexed room. | [optional] 
**version** | **Int** | The file version. | [optional] 
**versionGroup** | **Int** | The version group of the file. | [optional] 
**contentLength** | **Int64** | The file size in bytes, as a number; the REST FileDto sends a formatted string instead. | [optional] 
**fileType** | **Int** | The file type. Values of &#x60;FileType&#x60;: 0 &#x3D; Unknown, 1 &#x3D; Archive, 2 &#x3D; Video, 3 &#x3D; Audio, 4 &#x3D; Image, 5 &#x3D; Spreadsheet, 6 &#x3D; Presentation, 7 &#x3D; Document, 10 &#x3D; Pdf, 11 &#x3D; Diagram. | [optional] 
**fileExst** | **String** | The file extension, including the leading dot. | [optional] 
**comment** | **String** | The comment on the current file version. | [optional] 
**viewUrl** | **String** | The URL the file content can be downloaded from. Carries no share token. | [optional] 
**webUrl** | **String** | The URL the file can be opened at in a browser. Carries no share token. | [optional] 
**encrypted** | **Bool** | Specifies whether the file is encrypted or not. | [optional] 
**locked** | **Bool** | Specifies whether the file is locked or not. | [optional] 
**lockedBy** | **String** | The user name of whoever locked the file. | [optional] 
**isForm** | **Bool** | Specifies whether the file is a form or not. | [optional] 
**customFilterEnabled** | **Bool** | Specifies whether a custom filter is enabled for the file or not. | [optional] 
**customFilterEnabledBy** | **String** | The user name of whoever enabled the custom filter. | [optional] 
**lastOpened** | **Date** | The UTC date when the file was last opened. | [optional] 
**vectorizationStatus** | **Int** | The AI vectorization status of the file. Values of &#x60;VectorizationStatus&#x60;: 0 &#x3D; InProgress, 1 &#x3D; Completed, 2 &#x3D; Failed. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


