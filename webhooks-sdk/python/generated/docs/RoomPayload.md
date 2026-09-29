# RoomPayload

ASC.Files/Core/ApiModels/WebhookDto/RoomWebhookDto.cs. The room.* triggers and every agent.* trigger - an agent is an AI room and has the same shape today.  Not carried: logo, watermark, lifetime, tags, chatSettings. Each needs per-request URL building or an extra query; read them from GET api/2.0/files/rooms/{id}. 

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
**room_type** | **int** | enum RoomType | [optional] 
**type** | **int** | enum FolderType | [optional] 
**files_count** | **int** |  | [optional] 
**folders_count** | **int** |  | [optional] 
**private** | **bool** |  | [optional] 
**indexing** | **bool** |  | [optional] 
**deny_download** | **bool** |  | [optional] 
**pinned** | **bool** |  | [optional] 
**quota_limit** | **int** |  | [optional] 
**used_space** | **int** |  | [optional] 
**color** | **str** |  | [optional] 
**cover** | **str** |  | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.room_payload import RoomPayload

# TODO update the JSON string below
json = "{}"
# create an instance of RoomPayload from a JSON string
room_payload_instance = RoomPayload.from_json(json)
# print the JSON string representation of the object
print(RoomPayload.to_json())

# convert the object into a dict
room_payload_dict = room_payload_instance.to_dict()
# create an instance of RoomPayload from a dict
room_payload_from_dict = RoomPayload.from_dict(room_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


