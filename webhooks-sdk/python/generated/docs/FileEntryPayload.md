# FileEntryPayload

ASC.Files/Core/ApiModels/WebhookDto/FileEntryWebhookDto.cs. What FilePayload, FolderPayload and RoomPayload have in common.  This schema is never sent on its own -- unlike the previous contract, where every file, folder, room, agent and form trigger sent exactly this and nothing else. The old payload was typed as the abstract FileEntry<T> at the publish site, so System.Text.Json serialized by DECLARED type and every File<T> and Folder<T> member was silently dropped: version, contentLength, fileType, folderType, filesCount, roomType never reached a receiver. That is fixed; the subtypes below carry their own fields.  Not carried, deliberately: access, security, securityByUsers, availableShareRights, shareSettings, canShare, shared, sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. Those answer \"what may the caller see\", and a delivery has no caller - who receives it is decided by WebhookFileEntryAccessChecker against the subscription owner. Putting one user's permission matrix on the wire was both meaningless to the receiver and a disclosure. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**EntryId**](EntryId.md) |  | [optional] 
**parent_id** | [**EntryId**](EntryId.md) |  | [optional] 
**root_folder_id** | [**EntryId**](EntryId.md) |  | [optional] 
**title** | **str** |  | [optional] 
**file_entry_type** | **int** | 1 folder, 2 file. Present on every entry payload. | [optional] 
**created** | **datetime** |  | [optional] 
**created_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**updated** | **datetime** |  | [optional] 
**updated_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] 
**root_folder_type** | **int** | enum FolderType | [optional] 
**parent_room_type** | **int** | enum FolderType | [optional] 
**origin_id** | [**EntryId**](EntryId.md) |  | [optional] 
**origin_room_id** | [**EntryId**](EntryId.md) |  | [optional] 
**origin_title** | **str** |  | [optional] 
**origin_room_title** | **str** |  | [optional] 
**provider_item** | **bool** |  | [optional] 
**provider_key** | **str** |  | [optional] 
**provider_id** | **int** |  | [optional] 
**order** | **int** | Position within an indexed room. | [optional] 

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


