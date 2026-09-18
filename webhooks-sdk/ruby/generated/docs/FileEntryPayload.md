# DocspaceWebhooksSdk::FileEntryPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | [**EntryId**](EntryId.md) |  | [optional] |
| **parent_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **root_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **origin_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **origin_room_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **folder_id_display** | [**EntryId**](EntryId.md) |  | [optional] |
| **mutable_id** | **Boolean** |  | [optional] |
| **title** | **String** | The entry name, e.g. \&quot;321.xlsx\&quot;. Present for files as well as folders: File&lt;T&gt; overrides Title with [JsonIgnore] and exposes pureTitle instead, but that override is never reached because the base declaration is what gets serialized.  | [optional] |
| **is_new** | **Boolean** | Declared abstract on FileEntry; omitted when false. | [optional] |
| **create_by** | **String** |  | [optional] |
| **create_on** | **Time** |  | [optional] |
| **modified_by** | **String** |  | [optional] |
| **modified_on** | **Time** |  | [optional] |
| **shared_by** | **String** |  | [optional] |
| **root_create_by** | **String** |  | [optional] |
| **parent_room_created_by** | **String** |  | [optional] |
| **root_folder_type** | **Integer** | enum FolderType | [optional] |
| **parent_room_type** | **Integer** | enum FolderType | [optional] |
| **file_entry_type** | **Integer** | enum FileEntryType -- 1 folder, 2 file. The ONLY way to tell a folder from a file: no subtype-specific fields are ever sent.  | [optional] |
| **access** | **Integer** | enum FileShare | [optional] |
| **shared** | **Boolean** |  | [optional] |
| **shared_for_user** | **Boolean** |  | [optional] |
| **shared_external** | **Boolean** |  | [optional] |
| **parent_shared** | **Boolean** |  | [optional] |
| **provider_id** | **Integer** |  | [optional] |
| **provider_key** | **String** |  | [optional] |
| **origin_title** | **String** |  | [optional] |
| **origin_room_title** | **String** |  | [optional] |
| **order** | **Integer** |  | [optional] |
| **error** | **String** |  | [optional] |
| **tags** | **Array&lt;Hash&lt;String, Object&gt;&gt;** | TODO: expand Tag. | [optional] |
| **share_record** | **Hash&lt;String, Object&gt;** | TODO: expand FileShareRecord&lt;T&gt;. | [optional] |
| **security** | **Hash&lt;String, Boolean&gt;** | Caller-relative permission map (enum FilesSecurityActions -&gt; bool). Internal ACL state on the wire. REVIEW.  | [optional] |
| **security_by_users** | **Hash&lt;String, Hash&lt;String, Boolean&gt;&gt;** | Per-user permission map. Initialised non-null, so it is emitted as {} rather than omitted. REVIEW.  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::FileEntryPayload.new(
  id: null,
  parent_id: null,
  root_id: null,
  origin_id: null,
  origin_room_id: null,
  folder_id_display: null,
  mutable_id: null,
  title: null,
  is_new: null,
  create_by: null,
  create_on: null,
  modified_by: null,
  modified_on: null,
  shared_by: null,
  root_create_by: null,
  parent_room_created_by: null,
  root_folder_type: null,
  parent_room_type: null,
  file_entry_type: null,
  access: null,
  shared: null,
  shared_for_user: null,
  shared_external: null,
  parent_shared: null,
  provider_id: null,
  provider_key: null,
  origin_title: null,
  origin_room_title: null,
  order: null,
  error: null,
  tags: null,
  share_record: null,
  security: null,
  security_by_users: null
)
```

