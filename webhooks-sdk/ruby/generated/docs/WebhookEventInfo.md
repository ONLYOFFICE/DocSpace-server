# DocspaceWebhooksSdk::WebhookEventInfo

## Properties

| Name | Type | Description | Notes |
| ---- | ---- | ----------- | ----- |
| **id** | **Integer** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat.  | [optional] |
| **create_on** | **Time** | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body.  | [optional] |
| **create_by** | **String** | Acting user. Omitted when the empty guid (background jobs). | [optional] |
| **trigger** | **String** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. | [optional] |
| **trigger_id** | **Integer** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). | [optional] |

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

