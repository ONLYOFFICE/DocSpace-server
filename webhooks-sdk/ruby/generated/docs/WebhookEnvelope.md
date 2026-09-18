# DocspaceWebhooksSdk::WebhookEnvelope

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] |
| **payload** | **Object** | Trigger-specific body. Left untyped so that codegen stays clean in all 9 target languages; the runtime narrows it.  | [optional] |
| **webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::WebhookEnvelope.new(
  event: null,
  payload: null,
  webhook: null
)
```

