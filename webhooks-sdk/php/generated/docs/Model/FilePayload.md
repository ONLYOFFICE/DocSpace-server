# FilePayload

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**parent_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**root_folder_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**title** | **string** |  | [optional]
**file_entry_type** | **int** | 1 folder, 2 file. Present on every entry payload. | [optional]
**created** | **\DateTime** |  | [optional]
**created_by** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserSummaryPayload**](UserSummaryPayload.md) |  | [optional]
**updated** | **\DateTime** |  | [optional]
**updated_by** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\UserSummaryPayload**](UserSummaryPayload.md) |  | [optional]
**root_folder_type** | **int** | enum FolderType | [optional]
**parent_room_type** | **int** | enum FolderType | [optional]
**origin_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**origin_room_id** | [**\OnlyOffice\DocSpace\Webhooks\Sdk\Model\EntryId**](EntryId.md) |  | [optional]
**origin_title** | **string** |  | [optional]
**origin_room_title** | **string** |  | [optional]
**provider_item** | **bool** |  | [optional]
**provider_key** | **string** |  | [optional]
**provider_id** | **int** |  | [optional]
**order** | **int** | Position within an indexed room. | [optional]
**version** | **int** |  | [optional]
**version_group** | **int** |  | [optional]
**content_length** | **int** | Bytes, as a number. The REST DTO sends a formatted string. | [optional]
**file_type** | **int** | enum FileType | [optional]
**file_exst** | **string** | Includes the leading dot, e.g. \&quot;.docx\&quot;. | [optional]
**comment** | **string** |  | [optional]
**view_url** | **string** | Download URL. Carries no share token. | [optional]
**web_url** | **string** | Browser URL. Carries no share token. | [optional]
**encrypted** | **bool** |  | [optional]
**locked** | **bool** |  | [optional]
**locked_by** | **string** |  | [optional]
**is_form** | **bool** |  | [optional]
**custom_filter_enabled** | **bool** |  | [optional]
**custom_filter_enabled_by** | **string** |  | [optional]
**last_opened** | **\DateTime** |  | [optional]
**vectorization_status** | **int** | enum VectorizationStatus | [optional]

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
