# DocspaceWebhooksSdk::GroupPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** |  | [optional] |
| **name** | **String** |  | [optional] |
| **parent** | **String** |  | [optional] |
| **category** | **String** |  | [optional] |
| **is_ldap** | **Boolean** |  | [optional] |
| **is_system** | **Boolean** |  | [optional] |
| **manager** | [**UserSummaryPayload**](UserSummaryPayload.md) |  | [optional] |
| **members_count** | **Integer** |  | [optional] |

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

