# FileEntryPayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**parent_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**root_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**origin_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**origin_room_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**folder_id_display** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**mutable_id** | **bool** |  | [optional]
**title** | **string** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized. | [optional]
**is_new** | **bool** | Declared abstract on FileEntry; omitted when false. | [optional]
**create_by** | **string** |  | [optional]
**create_on** | **\DateTime** |  | [optional]
**modified_by** | **string** |  | [optional]
**modified_on** | **\DateTime** |  | [optional]
**shared_by** | **string** |  | [optional]
**root_create_by** | **string** |  | [optional]
**parent_room_created_by** | **string** |  | [optional]
**root_folder_type** | **int** | enum FolderType | [optional]
**parent_room_type** | **int** | enum FolderType | [optional]
**file_entry_type** | **int** | enum FileEntryType -- 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent. | [optional]
**access** | **int** | enum FileShare | [optional]
**shared** | **bool** |  | [optional]
**shared_for_user** | **bool** |  | [optional]
**shared_external** | **bool** |  | [optional]
**parent_shared** | **bool** |  | [optional]
**provider_id** | **int** |  | [optional]
**provider_key** | **string** |  | [optional]
**origin_title** | **string** |  | [optional]
**origin_room_title** | **string** |  | [optional]
**order** | **int** |  | [optional]
**error** | **string** |  | [optional]
**tags** | **array<string,mixed>[]** | TODO: expand Tag. | [optional]
**share_record** | **array<string,mixed>** | TODO: expand FileShareRecord&lt;T&gt;. | [optional]
**security** | **array<string,bool>** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW. | [optional]
**security_by_users** | **array<string,array<string,bool>>** | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW. | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
