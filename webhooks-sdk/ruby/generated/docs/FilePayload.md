# DocspaceWebhooksSdk::FilePayload

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
| **version** | **Integer** |  | [optional] |
| **version_group** | **Integer** |  | [optional] |
| **content_length** | **Integer** | Bytes, as a number. The REST DTO sends a formatted string. | [optional] |
| **file_type** | **Integer** | enum FileType | [optional] |
| **file_exst** | **String** | Includes the leading dot, e.g. \&quot;.docx\&quot;. | [optional] |
| **comment** | **String** |  | [optional] |
| **view_url** | **String** | Download URL. Carries no share token. | [optional] |
| **web_url** | **String** | Browser URL. Carries no share token. | [optional] |
| **encrypted** | **Boolean** |  | [optional] |
| **locked** | **Boolean** |  | [optional] |
| **locked_by** | **String** |  | [optional] |
| **is_form** | **Boolean** |  | [optional] |
| **custom_filter_enabled** | **Boolean** |  | [optional] |
| **custom_filter_enabled_by** | **String** |  | [optional] |
| **last_opened** | **Time** |  | [optional] |
| **vectorization_status** | **Integer** | enum VectorizationStatus | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::FilePayload.new(
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
  order: null,
  version: null,
  version_group: null,
  content_length: null,
  file_type: null,
  file_exst: null,
  comment: null,
  view_url: null,
  web_url: null,
  encrypted: null,
  locked: null,
  locked_by: null,
  is_form: null,
  custom_filter_enabled: null,
  custom_filter_enabled_by: null,
  last_opened: null,
  vectorization_status: null
)
```

