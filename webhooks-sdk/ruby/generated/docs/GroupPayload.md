# DocspaceWebhooksSdk::GroupPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** |  | [optional] |
| **name** | **String** |  | [optional] |
| **category_id** | **String** |  | [optional] |
| **parent** | [**GroupPayload**](GroupPayload.md) |  | [optional] |
| **sid** | **String** | LDAP identifier. REVIEW. | [optional] |
| **removed** | **Boolean** |  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::GroupPayload.new(
  id: null,
  name: null,
  category_id: null,
  parent: null,
  sid: null,
  removed: null
)
```

