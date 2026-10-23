# DocspaceWebhooksSdk::FilePayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | [**EntryId**](EntryId.md) |  | [optional] |
| **parent_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **root_folder_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **title** | **String** | The file entry title. | [optional] |
| **file_entry_type** | **Integer** | The file entry type: 1 for a folder, 2 for a file. Present on every entry payload. Flags of &#x60;FileEntryType&#x60;: 1 &#x3D; Folder, 2 &#x3D; File. | [optional] |
| **created** | **Time** | The UTC date when the file entry was created. | [optional] |
| **created_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] |
| **updated** | **Time** | The UTC date when the file entry was last updated. | [optional] |
| **updated_by** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] |
| **root_folder_type** | **Integer** | The root folder type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] |
| **parent_room_type** | **Integer** | The parent room type of the file entry. Values of &#x60;FolderType&#x60;: 0 &#x3D; DEFAULT, 1 &#x3D; COMMON, 2 &#x3D; BUNCH, 3 &#x3D; TRASH, 5 &#x3D; USER, 6 &#x3D; SHARE, 8 &#x3D; Projects, 10 &#x3D; Favorites, 11 &#x3D; Recent, 12 &#x3D; Templates, 13 &#x3D; Privacy, 14 &#x3D; VirtualRooms, 15 &#x3D; FillingFormsRoom, 16 &#x3D; EditingRoom, 19 &#x3D; CustomRoom, 20 &#x3D; Archive, 21 &#x3D; ThirdpartyBackup, 22 &#x3D; PublicRoom, 25 &#x3D; ReadyFormFolder, 26 &#x3D; InProcessFormFolder, 27 &#x3D; FormFillingFolderDone, 28 &#x3D; FormFillingFolderInProgress, 29 &#x3D; VirtualDataRoom, 30 &#x3D; RoomTemplates, 31 &#x3D; AiRoom, 32 &#x3D; Knowledge, 33 &#x3D; ChatOutputs, 34 &#x3D; AiAgents, 35 &#x3D; DefaultTemplates, 36 &#x3D; Forms, 37 &#x3D; Ai. | [optional] |
| **origin_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **origin_room_id** | [**EntryId**](EntryId.md) |  | [optional] |
| **origin_title** | **String** | The origin title of the file entry. | [optional] |
| **origin_room_title** | **String** | The origin room title of the file entry. | [optional] |
| **provider_item** | **Boolean** | Specifies whether the file entry is backed by a third-party provider or not. | [optional] |
| **provider_key** | **String** | The third-party provider key of the file entry. | [optional] |
| **provider_id** | **Integer** | The third-party provider ID of the file entry. | [optional] |
| **order** | **Integer** | The position of the file entry within an indexed room. | [optional] |
| **version** | **Integer** | The file version. | [optional] |
| **version_group** | **Integer** | The version group of the file. | [optional] |
| **content_length** | **Integer** | The file size in bytes, as a number; the REST FileDto sends a formatted string instead. | [optional] |
| **file_type** | **Integer** | The file type. Values of &#x60;FileType&#x60;: 0 &#x3D; Unknown, 1 &#x3D; Archive, 2 &#x3D; Video, 3 &#x3D; Audio, 4 &#x3D; Image, 5 &#x3D; Spreadsheet, 6 &#x3D; Presentation, 7 &#x3D; Document, 10 &#x3D; Pdf, 11 &#x3D; Diagram. | [optional] |
| **file_exst** | **String** | The file extension, including the leading dot. | [optional] |
| **comment** | **String** | The comment on the current file version. | [optional] |
| **view_url** | **String** | The URL the file content can be downloaded from. Carries no share token. | [optional] |
| **web_url** | **String** | The URL the file can be opened at in a browser. Carries no share token. | [optional] |
| **encrypted** | **Boolean** | Specifies whether the file is encrypted or not. | [optional] |
| **locked** | **Boolean** | Specifies whether the file is locked or not. | [optional] |
| **locked_by** | **String** | The user name of whoever locked the file. | [optional] |
| **is_form** | **Boolean** | Specifies whether the file is a form or not. | [optional] |
| **custom_filter_enabled** | **Boolean** | Specifies whether a custom filter is enabled for the file or not. | [optional] |
| **custom_filter_enabled_by** | **String** | The user name of whoever enabled the custom filter. | [optional] |
| **last_opened** | **Time** | The UTC date when the file was last opened. | [optional] |
| **vectorization_status** | **Integer** | The AI vectorization status of the file. Values of &#x60;VectorizationStatus&#x60;: 0 &#x3D; InProgress, 1 &#x3D; Completed, 2 &#x3D; Failed. | [optional] |

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

