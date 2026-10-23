# DocspaceWebhooksSdk::WebhookTargetInfo

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | [**EntryId**](EntryId.md) |  | [optional] |
| **type** | **String** | The prefix of the trigger the delivery is for: user, group, file, folder, room, form or agent today. Deliberately not an enum: every new trigger family adds a value, and a receiver built against an older contract must keep parsing it. | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::WebhookTargetInfo.new(
  id: null,
  type: null
)
```

