
# WebhookEventInfo

## Properties
| Name | Type | Description | Notes |
| ------------ | ------------- | ------------- | ------------- |
| **id** | **kotlin.Int** | Delivery-log record id. Stable across the automatic retries of one delivery; a MANUAL retry mints a new record and therefore a new id. Use as the idempotency key, with that caveat.  |  [optional] |
| **createOn** | [**java.time.OffsetDateTime**](java.time.OffsetDateTime.md) | UTC, whole seconds. No header timestamp exists, so replay defence must read this from the verified body.  |  [optional] |
| **createBy** | [**java.util.UUID**](java.util.UUID.md) | Acting user. Omitted when the empty guid (background jobs). |  [optional] |
| **trigger** | **kotlin.String** | e.g. \&quot;file.created\&quot;. \&quot;*\&quot; only in the legacy-payload fallback. |  [optional] |
| **triggerId** | **kotlin.Long** | Bit value of the trigger. Omitted when 0 (the \&quot;*\&quot; fallback). |  [optional] |



