
# RoomPayload

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **parentId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **rootFolderId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **title** | **kotlin.String** | The file entry title. |  [optional] |
| **fileEntryType** | **kotlin.Int** | The file entry type: 1 for a folder, 2 for a file. Present on every entry payload. Flags of &#x60;FileEntryType&#x60;: 1 &#x3D; Folder, 2 &#x3D; File. |  [optional] |
| **created** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) | The UTC date when the file entry was created. |  [optional] |
| **createdBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
| **updated** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) | The UTC date when the file entry was last updated. |  [optional] |
| **updatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
| **rootFolderType** | **kotlin.Int** | The root folder type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. |  [optional] |
| **parentRoomType** | **kotlin.Int** | The parent room type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. |  [optional] |
| **originId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **originRoomId** | [**onlyoffice.docspace.webhooks.sdk.EntryId**](onlyoffice.docspace.webhooks.sdk.EntryId.md) |  |  [optional] |
| **originTitle** | **kotlin.String** | The origin title of the file entry. |  [optional] |
| **originRoomTitle** | **kotlin.String** | The origin room title of the file entry. |  [optional] |
| **providerItem** | **kotlin.Boolean** | Specifies whether the file entry is backed by a third-party provider or not. |  [optional] |
| **providerKey** | **kotlin.String** | The third-party provider key of the file entry. |  [optional] |
| **providerId** | **kotlin.Int** | The third-party provider ID of the file entry. |  [optional] |
| **order** | **kotlin.Int** | The position of the file entry within an indexed room. |  [optional] |
| **roomType** | **kotlin.Int** | The room type. Values of &#x60;RoomType&#x60;: 1 &#x3D; FillingFormsRoom, 2 &#x3D; EditingRoom, 5 &#x3D; CustomRoom, 6 &#x3D; PublicRoom, 8 &#x3D; VirtualDataRoom, 9 &#x3D; AiRoom. |  [optional] |
| **type** | **kotlin.Int** | The underlying folder type of the room. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. |  [optional] |
| **filesCount** | **kotlin.Int** | The number of files the room directly contains. |  [optional] |
| **foldersCount** | **kotlin.Int** | The number of folders the room directly contains. |  [optional] |
| **&#x60;private&#x60;** | **kotlin.Boolean** | Specifies whether the room is private or not. |  [optional] |
| **indexing** | **kotlin.Boolean** | Specifies whether the room is indexed or not. |  [optional] |
| **denyDownload** | **kotlin.Boolean** | Specifies whether downloading from the room is denied or not. |  [optional] |
| **pinned** | **kotlin.Boolean** | Specifies whether the room is pinned or not. |  [optional] |
| **quotaLimit** | **kotlin.Long** | The room&#39;s own quota in bytes, -1 for an explicit \&quot;no limit\&quot;. Absent when the room has no quota of its own, so the portal&#39;s default room quota applies, if room quotas are on, and absent as well when the subscription&#39;s creator may not edit the room: as in the REST API, only those who may edit a room see its quota. |  [optional] |
| **usedSpace** | **kotlin.Long** | The space used by the room in bytes. |  [optional] |
| **color** | **kotlin.String** | The room cover colour. |  [optional] |
| **cover** | **kotlin.String** | The room cover name. |  [optional] |



