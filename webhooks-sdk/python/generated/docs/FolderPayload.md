# FolderPayload

ASC.Files/Core/ApiModels/WebhookDto/FolderWebhookDto.cs. The folder.* triggers. Rooms are RoomPayload, never this, even though both come from Folder<T>.  Not carried: inRoom, mute, new - all per-viewer state. 

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
**files_count** | **int** |  | [optional] 
**folders_count** | **int** |  | [optional] 
**type** | **int** | enum FolderType | [optional] 
**is_shareable** | **bool** |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.folder_payload import FolderPayload

# TODO update the JSON string below
json = "{}"
# create an instance of FolderPayload from a JSON string
folder_payload_instance = FolderPayload.from_json(json)
# print the JSON string representation of the object
print(FolderPayload.to_json())

# convert the object into a dict
folder_payload_dict = folder_payload_instance.to_dict()
# create an instance of FolderPayload from a dict
folder_payload_from_dict = FolderPayload.from_dict(folder_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


