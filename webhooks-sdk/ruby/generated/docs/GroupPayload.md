# DocspaceWebhooksSdk::GroupPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** | The group ID. | [optional] |
| **name** | **String** | The group name. | [optional] |
| **parent** | **String** | The parent group ID; absent for a top-level group. | [optional] |
| **category** | **String** | The ID of the group category: the empty GUID for the groups members create, and one fixed category ID for the built-in system groups. | [optional] |
| **is_ldap** | **Boolean** | Specifies whether the group comes from LDAP or not. | [optional] |
| **is_system** | **Boolean** | Specifies whether the group is a system group or not. | [optional] |
| **manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] |
| **members_count** | **Integer** | The number of group members. | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::GroupPayload.new(
  id: null,
  name: null,
  parent: null,
  category: null,
  is_ldap: null,
  is_system: null,
  manager: null,
  members_count: null
)
```

