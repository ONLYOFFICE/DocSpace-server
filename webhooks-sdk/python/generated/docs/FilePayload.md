# FilePayload

ASC.Files/Core/ApiModels/WebhookDto/FileWebhookDto.cs. A copy of the REST FileDto. Sent by every file.* trigger and by form.filled.out and form.stopped.  Not carried: thumbnailUrl, dimensions, viewAccessibility, formFillingStatus, hasDraft and the form-role fields. Each needs per-request work the REST helper does and a delivery must not - dimensions in particular opens the file stream to measure the image. 

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
**version** | **int** |  | [optional] 
**version_group** | **int** |  | [optional] 
**content_length** | **int** | Bytes, as a number. The REST DTO sends a formatted string. | [optional] 
**file_type** | **int** | enum FileType | [optional] 
**file_exst** | **str** | Includes the leading dot, e.g. \&quot;.docx\&quot;. | [optional] 
**comment** | **str** |  | [optional] 
**view_url** | **str** | Download URL. Carries no share token. | [optional] 
**web_url** | **str** | Browser URL. Carries no share token. | [optional] 
**encrypted** | **bool** |  | [optional] 
**locked** | **bool** |  | [optional] 
**locked_by** | **str** |  | [optional] 
**is_form** | **bool** |  | [optional] 
**custom_filter_enabled** | **bool** |  | [optional] 
**custom_filter_enabled_by** | **str** |  | [optional] 
**last_opened** | **datetime** |  | [optional] 
**vectorization_status** | **int** | enum VectorizationStatus | [optional] 

## Example

```python
from docspace_webhooks_sdk.models.file_payload import FilePayload

# TODO update the JSON string below
json = "{}"
# create an instance of FilePayload from a JSON string
file_payload_instance = FilePayload.from_json(json)
# print the JSON string representation of the object
print(FilePayload.to_json())

# convert the object into a dict
file_payload_dict = file_payload_instance.to_dict()
# create an instance of FilePayload from a dict
file_payload_from_dict = FilePayload.from_dict(file_payload_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


