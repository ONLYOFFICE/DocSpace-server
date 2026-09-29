# DocspaceWebhooksSdk::FileEntryPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | [**EntryId**](EntryId.md) |  | [optional] |
| **parent_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **root_folder_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **title** | **String** |  | [optional] |
| **file_entry_type** | **Integer** | 1 folder, 2 file. Present on every entry payload. | [optional] |
| **created** | **Time** |  | [optional] |
| **created_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] |
| **updated** | **Time** |  | [optional] |
| **updated_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] |
| **root_folder_type** | **Integer** | enum FolderType | [optional] |
| **parent_room_type** | **Integer** | enum FolderType | [optional] |
| **origin_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **origin_room_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **origin_title** | **String** |  | [optional] |
| **origin_room_title** | **String** |  | [optional] |
| **provider_item** | **Boolean** |  | [optional] |
| **provider_key** | **String** |  | [optional] |
| **provider_id** | **Integer** |  | [optional] |
| **order** | **Integer** | Position within an indexed room. | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::FileEntryPayload.new(
  id: null,
  parent_id: null,
  root_folder_id: null,
  title: null,
  file_entry_type: null,
  created: null,
  created_by: null,
  updated: null,
  updated_by: null,
  root_folder_type: null,
  parent_room_type: null,
  origin_id: null,
  origin_room_id: null,
  origin_title: null,
  origin_room_title: null,
  provider_item: null,
  provider_key: null,
  provider_id: null,
  order: null
)
```

