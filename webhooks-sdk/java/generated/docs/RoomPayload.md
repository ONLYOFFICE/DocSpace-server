

# RoomPayload

The room carried by a room.* webhook, and by every agent.* webhook.  A copy of the room-shaped half of FolderDto. Agent rooms use this payload too: an agent is an AI room and has the same shape today, so agent.* sends it as well. Not carried: logo, watermark, lifetime, tags and chatSettings. Each needs per-request URL building or an extra query that a delivery should not pay for; receivers can read them from GET api/2.0/files/rooms/{id}.

## Properties

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
|**id** | [**EntryId**](EntryId.md) |  |  [optional] |
|**parentId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**rootFolderId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**title** | **String** | The file entry title. |  [optional] |
|**fileEntryType** | **Integer** | The file entry type: 1 for a folder, 2 for a file. Present on every entry payload. Flags of &#x60;FileEntryType&#x60;: 1 &#x3D; Folder, 2 &#x3D; File. |  [optional] |
|**created** | **OffsetDateTime** | The UTC date when the file entry was created. |  [optional] |
|**createdBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
|**updated** | **OffsetDateTime** | The UTC date when the file entry was last updated. |  [optional] |
|**updatedBy** | [**UserSummaryPayload**](UserSummaryPayload.md) |  |  [optional] |
|**rootFolderType** | **Integer** | The root folder type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. |  [optional] |
|**parentRoomType** | **Integer** | The parent room type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. |  [optional] |
|**originId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**originRoomId** | [**EntryId**](EntryId.md) |  |  [optional] |
|**originTitle** | **String** | The origin title of the file entry. |  [optional] |
|**originRoomTitle** | **String** | The origin room title of the file entry. |  [optional] |
|**providerItem** | **Boolean** | Specifies whether the file entry is backed by a third-party provider or not. |  [optional] |
|**providerKey** | **String** | The third-party provider key of the file entry. |  [optional] |
|**providerId** | **Integer** | The third-party provider ID of the file entry. |  [optional] |
|**order** | **Integer** | The position of the file entry within an indexed room. |  [optional] |
|**roomType** | **Integer** | The room type. Values of &#x60;RoomType&#x60;: 1 &#x3D; FillingFormsRoom, 2 &#x3D; EditingRoom, 5 &#x3D; CustomRoom, 6 &#x3D; PublicRoom, 8 &#x3D; VirtualDataRoom, 9 &#x3D; AiRoom. |  [optional] |
|**type** | **Integer** | The underlying folder type of the room. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. |  [optional] |
|**filesCount** | **Integer** | The number of files the room directly contains. |  [optional] |
|**foldersCount** | **Integer** | The number of folders the room directly contains. |  [optional] |
|**_private** | **Boolean** | Specifies whether the room is private or not. |  [optional] |
|**indexing** | **Boolean** | Specifies whether the room is indexed or not. |  [optional] |
|**denyDownload** | **Boolean** | Specifies whether downloading from the room is denied or not. |  [optional] |
|**pinned** | **Boolean** | Specifies whether the room is pinned or not. |  [optional] |
|**quotaLimit** | **Long** | The room&#39;s own quota in bytes, -1 for an explicit \&quot;no limit\&quot;. Absent when the room has no quota of its own, so the portal&#39;s default room quota applies, if room quotas are on. Unlike the REST API, which shows the effective limit only to members who may edit the room, the value goes to every subscriber that gets the event. |  [optional] |
|**usedSpace** | **Long** | The space used by the room in bytes. |  [optional] |
|**color** | **String** | The room cover colour. |  [optional] |
|**cover** | **String** | The room cover name. |  [optional] |



