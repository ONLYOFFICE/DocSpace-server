# FilePayload

The file carried by a file.* webhook, and by form.filled.out and form.stopped.  A copy of FileDto. Not carried: thumbnailUrl, dimensions, viewAccessibility, formFillingStatus, hasDraft and the form-role fields. Each needs per-request work the REST helper does and a delivery must not; dimensions in particular opens the file stream to measure the image.

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
**version** | **int** | The file version. | [optional] 
**version_group** | **int** | The version group of the file. | [optional] 
**content_length** | **int** | The file size in bytes, as a number; the REST FileDto sends a formatted string instead. | [optional] 
**file_type** | **int** | The file type. Values of &#x60;FileType&#x60;: 0 &#x3D; Unknown, 1 &#x3D; Archive, 2 &#x3D; Video, 3 &#x3D; Audio, 4 &#x3D; Image, 5 &#x3D; Spreadsheet, 6 &#x3D; Presentation, 7 &#x3D; Document, 10 &#x3D; Pdf, 11 &#x3D; Diagram. | [optional] 
**file_exst** | **str** | The file extension, including the leading dot. | [optional] 
**comment** | **str** | The comment on the current file version. | [optional] 
**view_url** | **str** | The URL the file content can be downloaded from. Carries no share token. | [optional] 
**web_url** | **str** | The URL the file can be opened at in a browser. Carries no share token. | [optional] 
**encrypted** | **bool** | Specifies whether the file is encrypted or not. | [optional] 
**locked** | **bool** | Specifies whether the file is locked or not. | [optional] 
**locked_by** | **str** | The user name of whoever locked the file. | [optional] 
**is_form** | **bool** | True for every PDF file, with or without form fields, since the editor can open any PDF as a form; absent for other files. It does not tell a fillable form from an ordinary PDF document. | [optional] 
**custom_filter_enabled** | **bool** | Specifies whether a custom filter is enabled for the file or not. | [optional] 
**custom_filter_enabled_by** | **str** | The user name of whoever enabled the custom filter. | [optional] 
**last_opened** | **datetime** | The UTC date when the file was last opened. | [optional] 
**vectorization_status** | **int** | The AI vectorization status of the file. Values of &#x60;VectorizationStatus&#x60;: 0 &#x3D; InProgress, 1 &#x3D; Completed, 2 &#x3D; Failed. | [optional] 

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


