# DocspaceWebhooksSdk::FileDelivery

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **event** | [**WebhookEventInfo**](WebhookEventInfo.md) |  | [optional] |
| **payload** | [**FilePayload**](FilePayload.md) |  | [optional] |
| **webhook** | [**WebhookConfigInfo**](WebhookConfigInfo.md) |  | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::FileDelivery.new(
  event: null,
  payload: null,
  webhook: null
)
```

