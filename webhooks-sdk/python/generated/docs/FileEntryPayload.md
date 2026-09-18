# FileEntryPayload

The payload for EVERY file, folder, room, agent and form trigger.  There is deliberately no File- or Folder-specific schema. WebhookManager calls PublishAsync<T1,T2> with a static parameter type of FileEntry<T>, so T1 binds to the abstract base and System.Text.Json serializes by DECLARED type. File<T> and Folder<T> members -- pureTitle, version, contentLength, folderType, filesCount, isRoom -- therefore never reach the wire, however the entry was published.  One consequence worth internalising: `title` IS present, even for files. File<T> hides Title behind [JsonIgnore] and exposes pureTitle instead, but that override is invisible here because the base declaration is what gets serialized.  Verified against a captured production delivery: all 14 keys of a real file.created payload are members of this schema and nothing else. (Files/Core/Core/Entries/FileEntry.cs; [JsonIgnore] members excluded.) 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**EntryId**](EntryId.md) |  | [optional] 
**parent_id** | [**EntryId**](EntryId.md) |  | [optional] 
**root_id** | [**EntryId**](EntryId.md) |  | [optional] 
**origin_id** | [**EntryId**](EntryId.md) |  | [optional] 
**origin_room_id** | [**EntryId**](EntryId.md) |  | [optional] 
**folder_id_display** | [**EntryId**](EntryId.md) |  | [optional] 
**mutable_id** | **bool** |  | [optional] 
**title** | **str** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized.  | [optional] 
**is_new** | **bool** | Declared abstract on FileEntry; omitted when false. | [optional] 
**create_by** | **UUID** |  | [optional] 
**create_on** | **datetime** |  | [optional] 
**modified_by** | **UUID** |  | [optional] 
**modified_on** | **datetime** |  | [optional] 
**shared_by** | **UUID** |  | [optional] 
**root_create_by** | **UUID** |  | [optional] 
**parent_room_created_by** | **UUID** |  | [optional] 
**root_folder_type** | **int** | enum FolderType | [optional] 
**parent_room_type** | **int** | enum FolderType | [optional] 
**file_entry_type** | **int** | enum FileEntryType -- 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent.  | [optional] 
**access** | **int** | enum FileShare | [optional] 
**shared** | **bool** |  | [optional] 
**shared_for_user** | **bool** |  | [optional] 
**shared_external** | **bool** |  | [optional] 
**parent_shared** | **bool** |  | [optional] 
**provider_id** | **int** |  | [optional] 
**provider_key** | **str** |  | [optional] 
**origin_title** | **str** |  | [optional] 
**origin_room_title** | **str** |  | [optional] 
**order** | **int** |  | [optional] 
**error** | **str** |  | [optional] 
**tags** | **List[Dict[str, object]]** | TODO: expand Tag. | [optional] 
**share_record** | **Dict[str, object]** | TODO: expand FileShareRecord&lt;T&gt;. | [optional] 
**security** | **Dict[str, bool]** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW.  | [optional] 
**security_by_users** | **Dict[str, Dict[str, bool]]** | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW.  | [optional] 

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


