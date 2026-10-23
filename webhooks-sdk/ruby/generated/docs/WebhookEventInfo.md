# DocspaceWebhooksSdk::WebhookEventInfo

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **Integer** | The ID of the first delivery-log record of the event, and the idempotency key. It is stable across every resend of one event, the server&#39;s automatic retries and a manual retry from the admin UI alike: a manual retry is a delivery-log record of its own, but it still sends the ID of the first one. The same ID therefore always means the same logical event. It is also echoed in the x-docspace-event-id header. | [optional] |
| **create_on** | **Time** | When the event happened, in UTC and whole seconds. Echoed in the x-docspace-event-timestamp header, but that header is not signed, so replay defence must read this value from the verified body. | [optional] |
| **create_by** | **String** | The ID of the user whose action caused the event. Omitted when it is the empty GUID, as for background jobs. | [optional] |
| **trigger** | **String** | The trigger name, e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional] |
| **trigger_id** | **Integer** | The bit value of the trigger. Omitted when 0, the \&quot;*\&quot; fallback. | [optional] |

## Example

```ruby
require 'docspace-webhooks-sdk'

instance = DocspaceWebhooksSdk::WebhookEventInfo.new(
  id: null,
  create_on: null,
  create_by: null,
  trigger: null,
  trigger_id: null
)
```

