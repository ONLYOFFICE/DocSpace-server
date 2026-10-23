# FileEntryPayload

What every file, folder and room webhook payload has in common.  A copy of FileEntryDto, deliberately not derived from it, and populated straight from the domain entity rather than through FileEntryDtoHelper. Three groups of the REST fields are not carried:  - caller-relative state - access, security, securityByUsers, availableShareRights, shareSettings, canShare, shared, sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. A delivery has no caller; who may receive it is already decided by WebhookFileEntryAccessChecker against the subscription owner, and putting one user's permission matrix on the wire would be both meaningless and a disclosure.  - anything the REST helper pays for per request - thumbnails, image dimensions (it opens the file stream), form-filling roles, view accessibility, external share links.  - subtype fields belong on the subtype, unlike the previous payload where File and Folder members were erased by the declared parameter type and never reached the wire at all.  Timestamps are UTC, matching the delivery envelope, rather than the tenant-local ApiDateTime.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**EntryId**](EntryId.md) |  | [optional] 
**parent_id** | [**EntryId**](EntryId.md) |  | [optional] 
**root_folder_id** | [**EntryId**](EntryId.md) |  | [optional] 
**title** | **str** | The file entry title. | [optional] 
**file_entry_type** | **int** | The file entry type: 1 for a folder, 2 for a file. Present on every entry payload. Flags of &#x60;FileEntryType&#x60;: 1 &#x3D; Folder, 2 &#x3D; File. | [optional] 
**created** | **datetime** | The UTC date when the file entry was created. | [optional] 
**created_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**updated** | **datetime** | The UTC date when the file entry was last updated. | [optional] 
**updated_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**root_folder_type** | **int** | The root folder type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**parent_room_type** | **int** | The parent room type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] 
**origin_id** | [**EntryId**](EntryId.md) |  | [optional] 
**origin_room_id** | [**EntryId**](EntryId.md) |  | [optional] 
**origin_title** | **str** | The origin title of the file entry. | [optional] 
**origin_room_title** | **str** | The origin room title of the file entry. | [optional] 
**provider_item** | **bool** | Specifies whether the file entry is backed by a third-party provider or not. | [optional] 
**provider_key** | **str** | The third-party provider key of the file entry. | [optional] 
**provider_id** | **int** | The third-party provider ID of the file entry. | [optional] 
**order** | **int** | The position of the file entry within an indexed room. | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.file_entry_payload import FileEntryPayload

# TODO update the JSON string below
json = "{}"
# create an instance of FileEntryPayload from a JSON string
file_entry_payload_instance = FileEntryPayload.from_json(json)
# print the JSON string representation of the object
print(FileEntryPayload.to_json())

# convert the object into a dict
file_entry_payload_dict = file_entry_payload_instance.to_dict()
# create an instance of FileEntryPayload from a dict
file_entry_payload_from_dict = FileEntryPayload.from_dict(file_entry_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


