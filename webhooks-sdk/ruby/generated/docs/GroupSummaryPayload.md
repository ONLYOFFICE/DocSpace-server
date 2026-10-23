# DocspaceWebhooksSdk::GroupSummaryPayload

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **String** | The group ID. | [optional] |
| **name** | **String** | The group name. | [optional] |
| **manager** | **String** | The user name of the group manager, not an ID. | [optional] |
| **is_system** | **Boolean** | Specifies whether the group is a system group or not. | [optional] |

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

