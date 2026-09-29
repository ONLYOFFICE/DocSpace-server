# DocspaceWebhooksSdk::GroupSummaryPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** |  | [optional] |
| **name** | **String** |  | [optional] |
| **manager** | **String** | The user name of the group manager, not an id. | [optional] |
| **is_system** | **Boolean** |  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::GroupSummaryPayload.new(
  id: null,
  name: null,
  manager: null,
  is_system: null
)
```

