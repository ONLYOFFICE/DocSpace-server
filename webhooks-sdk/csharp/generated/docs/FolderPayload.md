# DocSpace.Webhooks.SDK.Model.FolderPayload
The folder carried by a folder.* webhook.  A copy of FolderDto reduced to what a plain folder has. Rooms are a separate payload (RoomWebhookDto) even though both come from Folder<T>, because room.* and folder.* are separate triggers whose contracts should be free to move independently. Not carried: inRoom, mute and new, all of them per-viewer state.

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
**FilesCount** | **int** | The number of files the folder directly contains. | [optional] 
**FoldersCount** | **int** | The number of subfolders the folder directly contains. | [optional] 
**Type** | **int** | The folder type. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**IsShareable** | **bool** | Specifies whether the folder can be shared or not. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

