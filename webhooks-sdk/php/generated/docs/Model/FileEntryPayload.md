# FileEntryPayload

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

[[Back to Model list]](../../README.md#models) [[Back to API list]](../../README.md#endpoints) [[Back to README]](../../README.md)
